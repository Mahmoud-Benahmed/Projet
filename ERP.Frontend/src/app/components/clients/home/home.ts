import { ChangeDetectorRef, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatIcon } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatTableDataSource } from '@angular/material/table';
import {
  ClientsService, ClientResponseDto, ClientStatsDto,
  CreateClientRequestDto, UpdateClientRequestDto,
  AddCategoryRequestDto, AssignedCategoryDto
} from '../../../services/clients/clients.service';
import { ModalComponent } from '../../modal/modal';
import { PaginationComponent } from '../../pagination/pagination';
import { HttpError } from '../../../interfaces/ErrorDto';
import { AuthService, PRIVILEGES } from '../../../services/auth/auth.service';
import { CategoriesService, ClientCategoryResponseDto } from '../../../services/clients/categories.service';
import { CurrencyConfigService } from '../../../services/currency-config.service';

type ViewMode = 'list' | 'list-deleted' | 'list-blocked' | 'create' | 'edit' | 'view';

@Component({
  selector: 'app-clients',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, MatIcon, PaginationComponent],
  templateUrl: './home.html',
  styleUrls: ['./home.scss'],
})
export class ClientsComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  dataSource = new MatTableDataSource<ClientResponseDto>([]);

  categories: ClientCategoryResponseDto[] = [];
  stats: ClientStatsDto | null = null;

  pageNumber = signal(1);
  pageSize = signal(10);
  pageSizeOptions = [5, 10, 25, 50];
  totalCount = 0;

  // ── Signals ───────────────────────────────────────────────────────────────

  viewMode = signal<ViewMode>('list');
  isMode = (mode: ViewMode) => computed(() => this.viewMode() === mode);

  isList        = this.isMode('list');
  isDeletedList = this.isMode('list-deleted');
  isBlockedList = this.isMode('list-blocked');
  isCreate      = this.isMode('create');
  isEdit        = this.isMode('edit');
  isView        = this.isMode('view');

  private previousMode: ViewMode = 'list';

  selectedClient: ClientResponseDto | null = null;
  loading = false;
  errors: string[] = [];
  successMessage: string | null = null;
  searchQuery = '';

  readonly PRIVILEGES = PRIVILEGES;
  clientForm: FormGroup;

  sortColumn: string = '';
  sortDirection: 'asc' | 'desc' = 'asc';

  selectedCategoryId = '';

  constructor(
    public authService: AuthService,
    private clientsService: ClientsService,
    private categoriesService: CategoriesService,
    private fb: FormBuilder,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef,
    private currencyConfig: CurrencyConfigService
  ) {
    this.clientForm = this.fb.group({
      name:        ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
      email:       ['', [Validators.required, Validators.email]],
      address:     ['', [Validators.required, Validators.minLength(5)]],
      phone:       [''],
      taxNumber:   [''],
      creditLimit: [null, [Validators.min(0)]],
      delaiRetour: [null, [Validators.min(0)]],
    });
  }

  ngOnInit(): void {
    this.dataSource.filterPredicate = (data, filter) =>
      this.flattenObject(data).includes(filter);
    this.reload();
  }

  // ── Page title ────────────────────────────────────────────────────────────

  get pageTitle(): string {
    if (this.isCreate())      return 'Add Client';
    if (this.isEdit())        return 'Edit Client';
    if (this.isView())        return 'Client Details';
    if (this.isDeletedList()) return 'Deleted Clients';
    if (this.isBlockedList()) return 'Blocked Clients';
    return 'List Clients';
  }

  // ── Stats ─────────────────────────────────────────────────────────────────

  get totalClients():   number { return this.stats?.totalClients   ?? 0; }
  get activeClients():  number { return this.stats?.activeClients  ?? 0; }
  get blockedClients(): number { return this.stats?.blockedClients ?? 0; }
  get deletedClients(): number { return this.stats?.deletedClients ?? 0; }

  get currencyCode():   string { return this.currencyConfig.code;   }
  get currencyLocale(): string { return this.currencyConfig.locale; }

  // ── Sorting ───────────────────────────────────────────────────────────────

  sortBy(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
  }

  get sortedData(): ClientResponseDto[] {
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

  // ── Pagination ────────────────────────────────────────────────────────────

  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize()); }
  onPageSizeChange(): void { this.pageNumber.set(1); this.load(); }

  // ── Load (pure fetchers — no mode switching) ──────────────────────────────

  load(): void {
    this.loading = true;
    this.errors = [];
    this.clientsService.getAll(this.pageNumber(), this.pageSize()).subscribe({
      next: (res) => {
        this.dataSource.data = res.items.filter(c => !c.isBlocked);
        this.totalCount = res.totalCount;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.flash('error', 'Failed to load clients.');
        this.loading = false;
      },
    });
  }

  loadDeleted(): void {
    this.clientsService.getDeleted(this.pageNumber(), this.pageSize()).subscribe({
      next: (res) => {
        this.dataSource.data = res.items;
        this.totalCount = res.totalCount;
        this.cdr.markForCheck();
      },
      error: () => this.flash('error', 'Failed to load deleted clients.'),
    });
  }

  loadBlocked(): void {
    this.clientsService.getAll(this.pageNumber(), this.pageSize()).subscribe({
      next: (res) => {
        this.dataSource.data = res.items.filter(c => c.isBlocked);
        this.totalCount = res.totalCount;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => this.flash('error', 'Failed to load blocked clients.'),
    });
  }

  loadCategories(): void {
    this.categoriesService.getAll().subscribe({
      next: (cats) => { this.categories = cats; this.cdr.markForCheck(); },
      error: () => this.flash('error', 'Failed to load categories.'),
    });
  }

  loadStats(): void {
    this.clientsService.getStats().subscribe({
      next: (res) => {
        this.stats = res;
        // auto-switch back to list when no deleted clients remain
        if (this.isDeletedList() && res.deletedClients === 0) {
          this.setViewMode('list');
          this.load();
        }
        // auto-switch back to list when no blocked clients remain
        if (this.isBlockedList() && res.blockedClients === 0) {
          this.setViewMode('list');
          this.load();
        }
        this.cdr.markForCheck();
      },
      error: () => this.flash('error', 'Failed to load stats.'),
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
    this.loadCategories();
    this.loadStats();
    this.cdr.markForCheck();
  }

  // ── Stat card clicks ──────────────────────────────────────────────────────

  onActiveCardClick(): void {
    if (this.isList()) return;
    this.setViewMode('list');
    this.load();
  }

  onBlockedCardClick(): void {
    if (this.isBlockedList() || this.blockedClients < 1) return;
    this.setViewMode('list-blocked');
    this.loadBlocked();
  }

  onDeletedCardClick(): void {
    if (this.isDeletedList() || this.deletedClients < 1) return;
    this.setViewMode('list-deleted');
    this.loadDeleted();
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────

  openCreate(): void {
    if (this.isCreate()) return;
    this.previousMode = this.viewMode();
    this.setViewMode('create');
    this.selectedClient = null;
    this.clientForm.reset({
      name: '', email: '', address: '',
      phone: '', taxNumber: '', creditLimit: null, delaiRetour: null
    });
  }

  openEdit(client: ClientResponseDto): void {
    if (this.isEdit()) return;
    this.previousMode = this.viewMode();
    this.setViewMode('edit');
    this.selectedClient = client;
    this.clientForm.patchValue({
      name:        client.name,
      email:       client.email,
      address:     client.address,
      phone:       client.phone ?? '',
      taxNumber:   client.taxNumber ?? '',
      creditLimit: client.creditLimit ?? null,
      delaiRetour: client.delaiRetour ?? null,
    });
    this.cdr.markForCheck();
  }

  openView(client: ClientResponseDto): void {
    if (this.isView()) return;
    this.previousMode = this.viewMode();
    this.setViewMode('view');
    this.selectedClient = client;
    this.selectedCategoryId = '';
    this.cdr.markForCheck();
  }

  cancel(): void {
    this.setViewMode(this.previousMode);
    this.selectedClient = null;
    this.clientForm.reset();
    this.selectedCategoryId = '';
  }

  restore(client: ClientResponseDto): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '400px',
      data: {
        title:       'Restore Client',
        message:     `Restore client "${client.name}"? It will reappear in the active clients list.`,
        confirmText: 'Restore',
        showCancel:  true,
        icon:        'settings_backup_restore',
        iconColor:   'success',
      },
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        if (!result) return;
        this.clientsService.restore(client.id).subscribe({
          next: () => {
            if (this.isView()) this.cancel();
            this.flash('success', `Client "${client.name}" has been restored.`);
            this.reload();
          },
          error: () => this.flash('error', 'Failed to restore client.'),
        });
      });
  }

  submit(): void {
    if (this.clientForm.invalid) return;
    const val = this.clientForm.value;

    if (this.isCreate()) {
      const dto: CreateClientRequestDto = {
        name:        val.name,
        email:       val.email,
        address:     val.address,
        phone:       val.phone || undefined,
        taxNumber:   val.taxNumber || undefined,
        creditLimit: val.creditLimit ?? undefined,
        delaiRetour: val.delaiRetour ?? undefined,
      };
      this.clientsService.create(dto).subscribe({
        next: () => { this.cancel(); this.reload(); this.flash('success', `Client "${val.name}" created successfully.`); },
        error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Failed to create client.'),
      });
    } else if (this.isEdit() && this.selectedClient) {
      const dto: UpdateClientRequestDto = {
        name:        val.name,
        email:       val.email,
        address:     val.address,
        phone:       val.phone || undefined,
        taxNumber:   val.taxNumber || undefined,
        creditLimit: val.creditLimit ?? undefined,
        delaiRetour: val.delaiRetour ?? undefined,
      };
      this.clientsService.update(this.selectedClient.id, dto).subscribe({
        next: () => { this.cancel(); this.reload(); this.flash('success', `Client "${val.name}" updated successfully.`); },
        error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Failed to update client.'),
      });
    }
  }

  delete(client: ClientResponseDto): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '400px',
      data: {
        title:       'Delete Client',
        message:     `Client "${client.name}" will be soft-deleted. Do you want to proceed?`,
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
        this.clientsService.delete(client.id).subscribe({
          next: () => {
            if (this.isView()) this.cancel();
            this.flash('success', `Client "${client.name}" deleted successfully.`);
            this.reload();
          },
          error: () => this.flash('error', `Failed to delete client "${client.name}".`),
        });
      });
  }

  toggleBlock(client: ClientResponseDto): void {
    const action = client.isBlocked ? 'Unblock' : 'Block';
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '400px',
      data: {
        title:       `${action} Client`,
        message:     `Are you sure you want to ${action.toLowerCase()} "${client.name}"?`,
        confirmText: action,
        showCancel:  true,
        icon:        client.isBlocked ? 'lock_open' : 'block',
        iconColor:   client.isBlocked ? 'success' : 'warning',
      },
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result) return;
        this.clientsService.toggleBlock(client).subscribe({
          next: (updated) => {
            this.flash('success', `Client "${client.name}" ${action.toLowerCase()}ed successfully.`);
            if (this.selectedClient?.id === client.id) this.selectedClient = updated;
            this.reload();
          },
          error: () => this.flash('error', `Failed to ${action.toLowerCase()} client.`),
        });
      });
  }

  // ── Category Management ───────────────────────────────────────────────────

  addCategory(clientId: string): void {
    if (!this.selectedCategoryId) return;
    const dto: AddCategoryRequestDto = { categoryId: this.selectedCategoryId };

    this.clientsService.addCategory(clientId, dto).subscribe({
      next: (result) => {
        this.selectedCategoryId = '';
        this.flash('success', 'Category assigned successfully.');
        this.selectedClient = result;
      },
      error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Failed to assign category.'),
    });
  }

  removeCategory(clientId: string, categoryId: string, categoryName: string): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '400px',
      data: {
        title:       'Remove Category',
        message:     `Remove category "${categoryName}" from this client?`,
        confirmText: 'Remove',
        showCancel:  true,
        icon:        'label_off',
        iconColor:   'danger',
      },
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result) return;
        this.clientsService.removeCategory(clientId, categoryId).subscribe({
          next: (client) => {
            this.flash('success', `Category "${categoryName}" removed.`);
            this.selectedClient = client;
          },
          error: () => this.flash('error', 'Failed to remove category.'),
        });
      });
  }

  get availableCategories(): ClientCategoryResponseDto[] {
    if (!this.selectedClient) return this.categories;
    return this.categories.filter(
      cat => !this.clientsService.hasCategory(this.selectedClient!, cat.id)
    );
  }

  // ── Feedback ──────────────────────────────────────────────────────────────

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

  // ── Helpers ───────────────────────────────────────────────────────────────

  trackById(_: number, c: ClientResponseDto): string { return c.id; }
  trackByCategoryId(_: number, cat: AssignedCategoryDto): string { return cat.id; }

  getCategoryNames(client: ClientResponseDto): string {
    return this.clientsService.getCategoryNames(client);
  }

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
