import { CommonModule, Location } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, inject, OnDestroy, OnInit } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';

import { AuthService, PRIVILEGES } from '../../../services/auth/auth.service';
import {
  PaymentService, PaymentDto, PaymentSummaryDto,
  PaymentStatus, PaymentMethod
} from '../../../services/payments/payment.service';
import { ModalComponent } from '../../modal/modal';
import { HttpError } from '../../../interfaces/ErrorDto';

@Component({
  selector: 'app-view-payment',
  standalone: true,
  imports: [
    CommonModule, MatIconModule, MatButtonModule,
    MatTooltipModule, MatDialogModule, TranslatePipe, RouterLink,
  ],
  templateUrl: './view.html',
  styleUrl: './view.scss',
})
export class ViewPaymentComponent implements OnInit, OnDestroy {

  private readonly destroyRef = inject(DestroyRef);
  private readonly translate  = inject(TranslateService);
  private readonly cdr        = inject(ChangeDetectorRef);
  private readonly location   = inject(Location);

  readonly PRIVILEGES    = PRIVILEGES;
  readonly PaymentStatus = PaymentStatus;
  readonly PaymentMethod = PaymentMethod;

  selectedPayment: PaymentDto | null          = null;
  paymentSummary:  PaymentSummaryDto | null   = null;
  paymentIdFromRoute: string | null           = null;

  errors: string[]            = [];
  successMessage: string|null = null;

  constructor(
    public  authService:    AuthService,
    private paymentService: PaymentService,
    private route:          ActivatedRoute,
    private router:         Router,
    private dialog:         MatDialog,
  ) {}

  ngOnInit(): void {
    this.paymentIdFromRoute = this.route.snapshot.paramMap.get('id');
    if (!this.paymentIdFromRoute) { this.cancel(); return; }
    this.reload();
  }

  reload(): void {
    this.paymentService.getById(this.paymentIdFromRoute!).subscribe({
      next: payment => {
        this.selectedPayment = payment;
        // Load invoice payment summary to show progress
        this.paymentService.getPaymentSummary(payment.invoiceId).subscribe({
          next:  s => { this.paymentSummary = s; this.cdr.markForCheck(); },
          error: () => { this.cdr.markForCheck(); },
        });
        this.cdr.markForCheck();
      },
      error: () => { this.flash('error', this.translate.instant('PAYMENTS.ERRORS.LOAD_FAILED')); this.cancel(); },
    });
  }

  // ── Actions ──────────────────────────────────────────────────────────
  delete(payment: PaymentDto): void {
    const ref = this.dialog.open(ModalComponent, {
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
    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(confirmed => {
      if (!confirmed) return;
      this.paymentService.delete(payment.id).subscribe({
        next: () => {
          this.flash('success', this.translate.instant('PAYMENTS.SUCCESS.DELETED'));
          setTimeout(() => this.cancel(), 2000);
        },
        error: () => this.flash('error', this.translate.instant('PAYMENTS.ERRORS.DELETE_FAILED')),
      });
    });
  }

  restore(payment: PaymentDto): void {
    this.paymentService.restore(payment.id).subscribe({
      next:  () => { this.flash('success', this.translate.instant('PAYMENTS.SUCCESS.RESTORED')); this.reload(); },
      error: () => this.flash('error', this.translate.instant('PAYMENTS.ERRORS.RESTORE_FAILED')),
    });
  }

  // ── Helpers ──────────────────────────────────────────────────────────
  statusClass(status: PaymentStatus): Record<string, boolean> { return this.paymentService.statusBadgeClass(status); }
  methodLabel(method: PaymentMethod): string                  { return this.paymentService.methodLabel(method); }
  methodIcon(method: PaymentMethod):  string                  { return this.paymentService.methodIcon(method); }

  get progressPercent(): number {
    if (!this.paymentSummary) return 0;
    return this.paymentService.paymentProgress(
      this.paymentSummary.totalPaid,
      this.paymentSummary.totalTTC + this.paymentSummary.lateFeeAmount,
    );
  }

  get isOverdue(): boolean {
    if (!this.selectedPayment) return false;
    return this.paymentService.isOverdue(this.selectedPayment.paymentDate);
  }

  flash(type: 'success' | 'error', msg: string): void {
    if (type === 'success') {
      this.successMessage = msg;
      setTimeout(() => { this.successMessage = null; }, 3000);
    } else {
      this.errors = [msg];
      setTimeout(() => { this.errors = []; }, 4000);
    }
  }

  cancel(): void { this.location.back(); }
  ngOnDestroy(): void {}
}