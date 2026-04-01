import { Component, OnInit, ChangeDetectorRef, ViewEncapsulation, DestroyRef, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PRIVILEGES, AuthService } from '../../../services/auth/auth.service';
import {
  StockService,
  FournisseurResponse,
  CreateFournisseurRequest,
  UpdateFournisseurRequest,
  FournisseurStatsDto,
} from './../../../services/stock.service';
import { MatTableDataSource } from '@angular/material/table';
import { HttpError } from '../../../interfaces/ErrorDto';
import { MatDialog } from '@angular/material/dialog';
import { ModalComponent } from '../../modal/modal';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PaginationComponent } from "../../pagination/pagination";
import { ActivatedRoute } from '@angular/router';

type ViewMode = 'list' | 'list-deleted' | 'list-blocked' | 'create' | 'edit' | 'view';


@Component({
  selector: 'app-fournisseur',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    MatIconModule, MatButtonModule, MatTooltipModule, MatProgressSpinnerModule,
    PaginationComponent
],
  templateUrl: './fournisseur.html',
  styleUrl: './fournisseur.scss',
  encapsulation: ViewEncapsulation.None,
})
export class FournisseurComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  dataSource = new MatTableDataSource<FournisseurResponse>([]);

  pageNumber = signal(1);
  pageSize = signal(10);
  pageSizeOptions = [5, 10, 25, 50];
  totalCount = 0;

  stats: FournisseurStatsDto | null = null;

  // ── State ──────────────────────────────────────────────────────────────────
  viewMode = signal<ViewMode>('list');
  isMode = (mode: ViewMode) => computed(() => this.viewMode() === mode);
  private previousMode: ViewMode = 'list';

  isList        = this.isMode('list');
  isDeletedList = this.isMode('list-deleted');
  isBlockedList = this.isMode('list-blocked');
  isCreate      = this.isMode('create');
  isEdit        = this.isMode('edit');
  isView        = this.isMode('view');


  errors: string[] = [];
  successMessage: string | null = null;
  searchQuery = '';

  readonly PRIVILEGES = PRIVILEGES
  fournisseurForm: FormGroup;

  sortColumn: string = '';
  sortDirection: 'asc' | 'desc' = 'asc';

  selectedFournisseur: FournisseurResponse | null = null;


  constructor(
    private stock: StockService,
    public authService: AuthService,
    private fb: FormBuilder,
    private cdr: ChangeDetectorRef,
    private dialog: MatDialog,
    private route: ActivatedRoute
  ) {
    this.fournisseurForm = this.fb.group({
      name:       ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
      address:    ['', [Validators.required, Validators.minLength(5)]],
      phone:      ['', [Validators.required, Validators.pattern(/^\+?[0-9\s\-()]{8,50}$/)]],
      taxNumber:  ['', [Validators.required]],
      rib:        ['', [Validators.required,Validators.pattern(/^[A-Za-z0-9]{10,34}$/)]],
      email:     ['', [Validators.email]],
    });
  }


  ngOnInit(): void {
    this.dataSource.filterPredicate = (data, filter) =>
      this.flattenObject(data).includes(filter);

    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        const id = params.get('id');

        if (id) {
          this.openFournisseurFromRoute(id);
        } else {
          this.reload();
        }
      });
  }

  private openFournisseurFromRoute(id: string): void {

    this.stock.getFournisseurById(id).subscribe({
      next: (fournisseur) => {
        this.selectedFournisseur = fournisseur;
        this.setViewMode('view');
        this.loadStats();
        this.cdr.markForCheck();
      },
      error: () => {
        this.flash('error', 'Fournisseur not found.');
        this.setViewMode('list');
      }
    });
  }

  sortBy(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
  }

  get sortedData(): FournisseurResponse[] {
    const data = [...this.dataSource.filteredData];
    if (!this.sortColumn) return data;

    return data.sort((a, b) => {
      let valA = this.getNestedValue(a, this.sortColumn);
      let valB = this.getNestedValue(b, this.sortColumn);

      if (valA == null) return 1;
      if (valB == null) return -1;
      if (typeof valA === 'string') valA = valA.toLowerCase();
      if (typeof valB === 'string') valB = valB.toLowerCase();

      return (valA < valB ? -1 : valA > valB ? 1 : 0) *
        (this.sortDirection === 'asc' ? 1 : -1);
    });
  }

  applyFilter(): void {
    this.dataSource.filter = this.searchQuery.trim().toLowerCase();
  }

  onActiveCardClick(): void {
    if (this.isList()) return;
    this.setViewMode('list');
    this.load();
  }

  onBlockedCardClick(): void {
    if (this.isBlockedList() || this.blockedFournisseurs < 1) return;
    this.setViewMode('list-blocked');
    this.loadBlocked();
  }

  onDeletedCardClick(): void {
    if (this.isDeletedList() || this.deletedFournisseurs < 1) return;
    this.setViewMode('list-deleted');
    this.loadDeleted();
  }

  // ── Data loading ───────────────────────────────────────────────────────────
  load(): void {
    this.stock.getFournisseurs().subscribe({
      next: (res) => {
        this.dataSource.data = res.items.filter(f => !f.isDeleted && !f.isBlocked);
        this.totalCount = res.totalCount;
        this.cdr.markForCheck();
      },
      error: (err) => {
        const error= err.error as HttpError;
        this.flash('error', error.message ?? 'Failed to load fournisseurs.');
      }
    });
  }

  loadDeleted(): void {
    this.stock.getDeletedFournisseurs().subscribe({
      next: (res) => {
        this.dataSource.data = res.items;
        this.totalCount = res.totalCount;
        this.cdr.markForCheck();

      },
      error:(err)=> {
        const error= err.error as HttpError;
        this.flash('error', error.message ?? 'Failed to load deleted fournisseurs.');
      }
    });
  }

  loadBlocked(): void {
    this.stock.getBlockedFournisseurs().subscribe({
      next: (res) => {
        this.dataSource.data = res.items;
        this.totalCount = res.totalCount;
        this.cdr.markForCheck();
      },
      error:(err)=> {
        const error= err.error as HttpError;
        this.flash('error', error.message ?? 'Failed to load blocked fournisseurs.');
      }
    });
  }

  loadStats(): void {
    this.stock.getFournisseurStats().subscribe({
      next:(res)=>{
        this.stats= res;
        this.cdr.markForCheck();
      },
      error:(err)=>{
        const error= err.error as HttpError;
        this.flash('error', error.message ?? 'Failed to load stats.');
      }

    });
  }

  reload(): void {
    if (this.isDeletedList()) {
      this.loadDeleted();
    } else if (this.isBlockedList()) {
      this.loadBlocked();
    } else {
      this.load();
    }
    this.loadStats();
    this.cdr.markForCheck();
  }


  openCreate(): void {
    if (this.isCreate()) return;
    this.previousMode = this.viewMode();
    this.setViewMode('create');
    this.selectedFournisseur = null;
    this.fournisseurForm.reset({
      name: '', address: '', phone: '',
      taxNumber: '', rib: '', email: ''
    });
  }

  openView(fournisseur: FournisseurResponse): void {
    if (this.isView()) return;
    this.previousMode = this.viewMode();
    this.setViewMode('view');
    this.selectedFournisseur = fournisseur;
    this.cdr.markForCheck();
  }

  openEdit(fournisseur: FournisseurResponse): void {
    if (this.isEdit()) return;
    this.previousMode = this.viewMode();
    this.selectedFournisseur = fournisseur;
    this.setViewMode('edit');
    this.fournisseurForm.patchValue({
      name:        fournisseur.name,
      email:       fournisseur.email,
      address:     fournisseur.address,
      phone:       fournisseur.phone ?? '',
      taxNumber:   fournisseur.taxNumber ?? '',
      rib:         fournisseur.rib ?? '',
    });
    this.cdr.markForCheck();
  }

  cancel(): void {
    const target = this.resolveCancel();
    const needsClient: ViewMode[] = ['view', 'edit'];

    this.setViewMode(target);

    if (!needsClient.includes(target)) {
      this.selectedFournisseur = null;
    }

    if (target !== 'edit') {
      this.fournisseurForm.reset();
    }
  }

  private resolveCancel(): ViewMode {
    const current = this.viewMode();

    if (current === 'edit' && this.previousMode === 'view' && this.selectedFournisseur) {
      return 'view';
    }

    if (current === 'view' && (
      this.previousMode === 'list' ||
      this.previousMode === 'list-deleted' ||
      this.previousMode === 'list-blocked'
    )) {
      // ✅ data may never have been loaded if we arrived via route navigation
      this.reloadForMode(this.previousMode);
      return this.previousMode;
    }

    if (current === 'create') {
      this.reloadForMode(this.previousMode ?? 'list');
      return this.previousMode ?? 'list';
    }

    this.load();
    return 'list';
  }

  // ✅ new helper — loads the right dataset for any list mode
  private reloadForMode(mode: ViewMode): void {
    if (mode === 'list-deleted') {
      this.loadDeleted();
    } else if (mode === 'list-blocked') {
      this.loadBlocked();
    } else {
      this.load();
    }
    this.loadStats();
  }


  get totalFournisseurs():   number { return this.stats?.totalFournisseurs   ?? 0; }
  get activeFournisseurs():  number { return this.stats?.activeFournisseurs  ?? 0; }
  get blockedFournisseurs(): number { return this.stats?.blockedFournisseurs ?? 0; }
  get deletedFournisseurs(): number { return this.stats?.deletedFournisseurs ?? 0; }

  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize()); }
  onPageSizeChange(): void { this.pageNumber.set(1); this.load(); }

  submit(): void {
    if (this.fournisseurForm.invalid) return;
    const val = this.fournisseurForm.value;
    if (this.isCreate()) {
      const dto: CreateFournisseurRequest = {
        name:        val.name,
        address:     val.address,
        phone:       val.phone,
        taxNumber:   val.taxNumber,
        rib:         val.rib ,
        email:       val.email || undefined,
      };

      this.stock.createFournisseur(dto).subscribe({
        next: () => {
          this.cancel();
          this.reload();
          this.flash('success', `Fournisseur "${val.name}" created successfully.`);
        },
        error: (err) =>{
          const error= err.error as HttpError;
          this.flash('error',  error.message ?? 'Failed to create fournisseur.')
        }
      });
    } else if (this.isEdit() && this.selectedFournisseur) {
      const dto: UpdateFournisseurRequest = {
        name:        val.name,
        address:     val.address,
        phone:       val.phone,
        taxNumber:   val.taxNumber,
        rib:         val.rib ,
        email:       val.email || undefined,
      };

      this.stock.updateFournisseur(this.selectedFournisseur.id, dto).subscribe({
        next: () => {
          this.cancel();
          this.reload();
          this.flash('success', `Fournisseur "${val.name}" updated successfully.`); },
        error: (err) =>{
          const error= err.error as HttpError;
          this.flash('error', error.message ?? 'Failed to update client.');
        }
      });
    }
  }

  // ── Delete / Restore ───────────────────────────────────────────────────────
  delete(fournisseur: FournisseurResponse): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '400px',
      data: {
        title:       'Delete Fournisseur',
        message:     `Fournisseur "${fournisseur.name}" will be soft-deleted. Do you want to proceed?`,
        confirmText: 'Delete',
        showCancel:  true,
        icon:        'auto_delete',
        iconColor:   'danger',
      },
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result) return;
        this.stock.deleteFournisseur(fournisseur.id).subscribe({
          next: () => {
            if (this.isView()) this.cancel();
            this.reload();
            this.flash('success', `Fournisseur "${fournisseur.name}" deleted successfully.`);
          },
          error: (err) => {
            const error= err.error as HttpError;
            this.flash('error', error.message ?? `Failed to delete fournisseur "${fournisseur.name}".`);
          }
        });
      });
  }

  restore(id: string): void {
    this.stock.restoreFournisseur(id).subscribe({
      next: () => { this.flash('success', 'Fournisseur restored.'); this.load(); },
      error: (err) => { this.flash('error', err.error?.message ?? 'Restore failed.'); },
    });
  }

  // ── Block / Unblock ────────────────────────────────────────────────────────
  toggleBlock(fournisseur: FournisseurResponse): void {
    const action = fournisseur.isBlocked ? 'Unblock' : 'Block';
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '400px',
      data: {
        title:       `${action} Fournisseur`,
        message:     `Are you sure you want to ${action.toLowerCase()} "${fournisseur.name}"?`,
        confirmText: action,
        showCancel:  true,
        icon:        fournisseur.isBlocked ? 'lock_open' : 'block',
        iconColor:   fournisseur.isBlocked ? 'success' : 'warning',
      },
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result) return;
        this.stock.toggleBlock(fournisseur).subscribe({
          next: (updated) => {
            this.flash('success', `Fournisseur "${fournisseur.name}" ${action.toLowerCase()}ed successfully.`);
            if (this.selectedFournisseur?.id === fournisseur.id) this.selectedFournisseur = updated;
            this.reload();
          },
          error: () => this.flash('error', `Failed to ${action.toLowerCase()} fournisseur.`),
        });
      });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  flash(type: 'success' | 'error', msg: string): void {
    if (type === 'success') {
      this.successMessage = msg;
      setTimeout(() => { this.successMessage = null; this.cdr.markForCheck(); }, 3000);
    } else {
      this.errors = [msg];
      setTimeout(() => { this.errors = []; this.cdr.markForCheck(); }, 4000);
    }
    this.cdr.markForCheck();
  }

  dismissError(): void { this.errors = []; }

  trackById(_: number, f: FournisseurResponse): string { return f.id; }

  private flattenObject(obj: any): string {
    return Object.keys(obj)
      .map(key => {
        const value = obj[key];
        if (value && typeof value === 'object') return this.flattenObject(value);
        return value;
      })
      .join(' ')
      .toLowerCase();
  }

  private getNestedValue(obj: any, path: string): any {
    return path.split('.').reduce((acc, key) => acc?.[key], obj);
  }

  setViewMode(mode: ViewMode): void {
    this.viewMode.set(mode);
    this.cdr.markForCheck();
  }
}
