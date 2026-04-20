import {
  Component, OnInit, OnDestroy, signal, computed,
  inject, DestroyRef, ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AuthService, PRIVILEGES } from '../../services/auth/auth.service';
import {
  PaymentService, PaymentDto, PaymentStatsDto,
  PaymentStatus, PaymentMethod
} from '../../services/payments/payment.service';
import { PaginationComponent } from '../pagination/pagination';
import { ModalComponent } from '../modal/modal';

type ViewMode = 'list' | 'stats';

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatTooltipModule, MatDialogModule,
    PaginationComponent, TranslatePipe, RouterLink,
  ],
  templateUrl: './payments.html',
  styleUrl: './payments.scss',
})
export class PaymentsComponent implements OnInit, OnDestroy {

  private readonly destroyRef    = inject(DestroyRef);
  private readonly translate     = inject(TranslateService);
  private readonly cdr           = inject(ChangeDetectorRef);

  readonly PRIVILEGES    = PRIVILEGES;
  readonly PaymentStatus = PaymentStatus;
  readonly PaymentMethod = PaymentMethod;

  // ── View mode ────────────────────────────────────────────────────────
  viewMode = signal<ViewMode>('list');
  isList   = computed(() => this.viewMode() === 'list');
  isStats  = computed(() => this.viewMode() === 'stats');

  // ── Data ─────────────────────────────────────────────────────────────
  payments: PaymentDto[]               = [];
  paymentStats: PaymentStatsDto | null = null;
  statsLoading                         = false;

  // ── Pagination ───────────────────────────────────────────────────────
  currentPage              = 1;
  currentSize              = 10;
  readonly pageSizeOptions = [5, 10, 25, 50];
  get totalPages(): number { return Math.ceil(this.filteredData.length / this.currentSize) || 1; }

  get pagedPayments(): PaymentDto[] {
    const start = (this.currentPage - 1) * this.currentSize;
    return this.sortedData.slice(start, start + this.currentSize);
  }

  // ── Quick stats bar ──────────────────────────────────────────────────
  stats = { total: 0, completed: 0, pending: 0, failed: 0, revenue: 0 };

  // ── Filters / sort ───────────────────────────────────────────────────
  searchQuery   = '';
  statusFilter  = 'ALL';
  sortColumn    = 'paymentDate';
  sortDirection: 'asc' | 'desc' = 'desc';

  get filteredData(): PaymentDto[] {
    let data = this.payments.filter(p => !p.isDeleted);

    if (this.statusFilter !== 'ALL') {
      data = data.filter(p => p.status === this.statusFilter);
    }
    if (this.searchQuery) {
      const q = this.searchQuery.toLowerCase();
      data = data.filter(p =>
        p.id.toLowerCase().includes(q) ||
        p.invoiceId.toLowerCase().includes(q) ||
        p.method.toLowerCase().includes(q),
      );
    }
    return data;
  }

  get sortedData(): PaymentDto[] {
    const data = [...this.filteredData];
    if (!this.sortColumn) return data;
    return data.sort((a, b) => {
      const av = (a as any)[this.sortColumn];
      const bv = (b as any)[this.sortColumn];
      const cmp = av < bv ? -1 : av > bv ? 1 : 0;
      return this.sortDirection === 'asc' ? cmp : -cmp;
    });
  }

  // ── Alerts ───────────────────────────────────────────────────────────
  errors: string[]            = [];
  successMessage: string|null = null;

  constructor(
    public  authService:    AuthService,
    private paymentService: PaymentService,
    private dialog:         MatDialog,
  ) {}

  ngOnInit(): void  { this.load(); }
  ngOnDestroy(): void {}

  // ── Load ─────────────────────────────────────────────────────────────
  load(): void {
    this.paymentService.getAll().subscribe({
      next: payments => {
        this.payments = payments;
        this.refreshStats();
        this.cdr.markForCheck();
      },
      error: () => this.flash('error', this.translate.instant('PAYMENTS.ERRORS.LOAD_FAILED')),
    });
  }

  private refreshStats(): void {
    this.stats.total     = this.payments.filter(p => !p.isDeleted).length;
    this.stats.completed = this.payments.filter(p => p.status === PaymentStatus.Completed && !p.isDeleted).length;
    this.stats.pending   = this.payments.filter(p => p.status === PaymentStatus.Pending   && !p.isDeleted).length;
    this.stats.failed    = this.payments.filter(p => p.status === PaymentStatus.Failed    && !p.isDeleted).length;
    this.stats.revenue   = this.payments
      .filter(p => p.status === PaymentStatus.Completed && !p.isDeleted)
      .reduce((s, p) => s + p.amount, 0);
  }

  loadStats(): void {
    this.statsLoading = true;
    this.paymentService.getStats().subscribe({
      next: stats => {
        this.paymentStats = stats;
        this.statsLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.flash('error', this.translate.instant('PAYMENTS.ERRORS.LOAD_STATS_FAILED'));
        this.statsLoading = false;
      },
    });
  }

  // ── Navigation ───────────────────────────────────────────────────────
  openStats(): void {
    if (this.isStats()) return;
    this.viewMode.set('stats');
    this.loadStats();
  }

  backToList(): void {
    this.viewMode.set('list');
    this.load();
  }

  // ── Filters ──────────────────────────────────────────────────────────
  setStatusFilter(status: string): void {
    this.statusFilter = status;
    this.currentPage  = 1;
  }

  sortBy(col: string): void {
    this.sortDirection = this.sortColumn === col && this.sortDirection === 'asc' ? 'desc' : 'asc';
    this.sortColumn    = col;
    this.currentPage   = 1;
  }

  onPageChange(page: number):     void { this.currentPage = page; }
  onPageSizeChange(size: number): void { this.currentSize = size; this.currentPage = 1; }

  // ── CRUD ─────────────────────────────────────────────────────────────
  delete(payment: PaymentDto): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '420px',
      data: {
        icon: 'delete', iconColor: 'warn',
        title:       this.translate.instant('PAYMENTS.DIALOG.DELETE_TITLE'),
        message:     this.translate.instant('PAYMENTS.DIALOG.DELETE_MESSAGE', { id: payment.id.slice(0, 8) }),
        confirmText: this.translate.instant('PAYMENTS.DIALOG.DELETE_CONFIRM'),
        cancelText:  this.translate.instant('COMMON.CANCEL'),
        showCancel:  true,
      },
    });
    dialogRef.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(confirmed => {
      if (!confirmed) return;
      this.paymentService.delete(payment.id).subscribe({
        next:  () => { this.flash('success', this.translate.instant('PAYMENTS.SUCCESS.DELETED')); this.load(); },
        error: () => this.flash('error', this.translate.instant('PAYMENTS.ERRORS.DELETE_FAILED')),
      });
    });
  }

  restore(payment: PaymentDto): void {
    this.paymentService.restore(payment.id).subscribe({
      next:  () => { this.flash('success', this.translate.instant('PAYMENTS.SUCCESS.RESTORED')); this.load(); },
      error: () => this.flash('error', this.translate.instant('PAYMENTS.ERRORS.RESTORE_FAILED')),
    });
  }

  // ── Helpers ──────────────────────────────────────────────────────────
  statusClass(status: PaymentStatus):  Record<string, boolean> { return this.paymentService.statusBadgeClass(status); }
  methodLabel(method: PaymentMethod):  string                  { return this.paymentService.methodLabel(method); }
  methodIcon(method: PaymentMethod):   string                  { return this.paymentService.methodIcon(method); }

  trackById(_: number, item: { id: string }) { return item.id; }

  flash(type: 'success' | 'error', msg: string): void {
    if (type === 'success') {
      this.successMessage = msg;
      setTimeout(() => { this.successMessage = null; }, 3000);
    } else {
      this.errors = [msg];
      setTimeout(() => { this.errors = []; }, 4000);
    }
  }

  dismissError(): void { this.errors = []; }
}