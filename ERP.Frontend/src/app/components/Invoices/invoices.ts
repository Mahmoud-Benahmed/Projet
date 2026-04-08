import { Component, OnInit, signal, computed, inject, DestroyRef, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { AuthService, PRIVILEGES } from '../../services/auth/auth.service';
import { InvoiceService, InvoiceDto, CreateInvoiceDto, InvoiceStatsDto } from '../../services/invoices/invoice.service';
import { ClientsService, ClientResponseDto } from '../../services/clients/clients.service';
import { ArticleService, ArticleResponseDto } from '../../services/articles/articles.service';
import { PaginationComponent } from '../pagination/pagination';
import { ModalComponent } from '../modal/modal';
import { HttpError } from '../../interfaces/ErrorDto';
import { Chart, ChartConfiguration, registerables } from 'chart.js';

interface PendingItem {
  _localId: string;
  articleId: string;
  articleName: string;
  articleBarCode: string;
  quantity: number;
  uniPriceHT: number;
  taxRate: number;
  totalHT: number;
  totalTTC: number;
}

export interface InvoiceValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
  discountedTotal?: number;
  originalTotal?: number;
  discountApplied?: number;
  discountRate?: number;
}

type ViewMode = 'list' | 'list-deleted' | 'create' | 'view' | 'stats';

Chart.register(...registerables);
@Component({
  selector: 'app-invoices',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatDialogModule,
    PaginationComponent,
    TranslatePipe
  ],
  templateUrl: './invoices.html',
  styleUrl: './invoices.scss',
})
export class InvoicesComponent implements OnInit {
  @ViewChild('monthlyChart') monthlyChartRef!: ElementRef<HTMLCanvasElement>;
  private chart: Chart | null = null;
  private readonly destroyRef = inject(DestroyRef);
  private translate = inject(TranslateService);

  creditWarning: string | null = null;
  discountInfo = {
    applies: false,
    rate: 0,
    discountAmount: 0,
    originalTotal: 0,
    discountedTotal: 0
  };
  isValidating = false;
  selectedClientForValidation: ClientResponseDto | null = null;

  readonly PRIVILEGES = PRIVILEGES;

  // ── View mode ──────────────────────────────────────────────────────────────
  viewMode = signal<ViewMode>('list');
  private previousMode: ViewMode = 'list';

  // Computed properties for mode checking
  isMode = (mode: ViewMode) => computed(() => this.viewMode() === mode);

  isList = this.isMode('list');
  isDeletedList = this.isMode('list-deleted');
  isCreate = this.isMode('create');
  isView = this.isMode('view');
  isStats = this.isMode('stats');

  // ── Invoice list state ─────────────────────────────────────────────────────
  invoices: InvoiceDto[]             = [];
  deletedInvoices: InvoiceDto[]      = [];
  selectedInvoice: InvoiceDto | null = null;

  totalCount  = 0;
  currentPage = 1;
  currentSize = 10;
  readonly pageSizeOptions = [5, 10, 25, 50];
  get totalPages(): number { return Math.ceil(this.totalCount / this.currentSize) || 1; }

  // ── Quick stats (tabs/cards) ───────────────────────────────────────────────
  stats = { total: 0, draft: 0, unpaid: 0, paid: 0, cancelled: 0, deleted: 0 };

  // ── Full stats from API ────────────────────────────────────────────────────
  invoiceStats: InvoiceStatsDto | null = null;
  statsLoading = false;

  // ── Filters / sort ────────────────────────────────────────────────────────
  searchQuery   = '';
  statusFilter  = 'ALL';
  sortColumn    = '';
  sortDirection: 'asc' | 'desc' = 'asc';

  get sortedData(): InvoiceDto[] {
    let data = this.isDeletedList() ? [...this.deletedInvoices] : [...this.invoices];
    if (this.searchQuery) {
      const q = this.searchQuery.toLowerCase();
      data = data.filter(i =>
        i.invoiceNumber.toLowerCase().includes(q) ||
        i.clientFullName.toLowerCase().includes(q)
      );
    }
    if (this.sortColumn) {
      data.sort((a, b) => {
        const av = (a as any)[this.sortColumn];
        const bv = (b as any)[this.sortColumn];
        const cmp = av < bv ? -1 : av > bv ? 1 : 0;
        return this.sortDirection === 'asc' ? cmp : -cmp;
      });
    }
    return data;
  }

  // ── Alerts ────────────────────────────────────────────────────────────────
  errors: string[] = [];
  successMessage: string | null = null;

  // ── Forms ────────────────────────────────────────────────────────────────
  invoiceForm!: FormGroup;
  itemForm!: FormGroup;

  // ── Clients / articles ────────────────────────────────────────────────────
  clients: ClientResponseDto[]         = [];
  filteredClients: ClientResponseDto[] = [];
  clientSearchQuery                    = '';
  articles: ArticleResponseDto[]       = [];

  // ── Pending line items (create view) ──────────────────────────────────────
  pendingItems: PendingItem[] = [];
  inlineItemOpen    = false;
  inlineItemLocalId = '';

  constructor(
    public  authService:    AuthService,
    private invoiceService: InvoiceService,
    private clientsService: ClientsService,
    private articleService: ArticleService,
    private fb:             FormBuilder,
    private dialog:         MatDialog,
  ) {}

  ngAfterViewInit(): void {
    if (this.invoiceStats?.monthlyBreakdown) {
      this.renderStatusPieChart();
    }
    this.observeThemeChanges();
  }

  ngOnInit(): void {
    this.buildForms();
    this.reload();
    
  }

  // ── Page title ────────────────────────────────────────────────────────────
  get pageTitle(): string {
    if (this.isCreate())      return 'INVOICES.TITLE_NEW';
    if (this.isView())        return 'INVOICES.TITLE_DETAILS';
    if (this.isDeletedList()) return 'INVOICES.TITLE_DELETED';
    if (this.isStats())       return 'INVOICES.TITLE_ANALYTICS';
    return 'INVOICES.TITLE_LIST';
  }

  get duePaymentPeriodHint(): string {
    if (!this.selectedClientForValidation?.duePaymentPeriod) {
      return '';
    }
    return `Due date calculated: ${this.selectedClientForValidation.duePaymentPeriod} days after invoice date`;
  }

  // ── Forms ─────────────────────────────────────────────────────────────────
  private buildForms(): void {
    // Remove invoiceNumber - backend will generate it
    this.invoiceForm = this.fb.group({
      invoiceDate:     ['', Validators.required],
      dueDate:         ['', Validators.required],
      clientId:        ['', Validators.required],
      clientFullName:  ['', Validators.required],
      clientAddress:   ['', Validators.required],
      additionalNotes: [null],
    });
    
    this.itemForm = this.fb.group({
      articleId:  ['', Validators.required],
      quantity:   [1,  [Validators.required, Validators.min(1)]],
      uniPriceHT: [0,  [Validators.required, Validators.min(0)]],
      taxRate:    [19, [Validators.required, Validators.min(0), Validators.max(100)]],
    });
    
    // Listen to invoice date changes to auto-update due date
    this.invoiceForm.get('invoiceDate')?.valueChanges.subscribe(() => {
      this.onInvoiceDateChange();
    });
  }

  checkFormValidity(): void {
    console.log('Form valid:', this.invoiceForm.valid);
    console.log('Form errors:', this.invoiceForm.errors);
    Object.keys(this.invoiceForm.controls).forEach(key => {
      const control = this.invoiceForm.get(key);
      console.log(`${key}: valid=${control?.valid}, errors=`, control?.errors);
    });
  }

  // ── Load data ─────────────────────────────────────────────────────────────
  load(): void {
    if (this.statusFilter === 'ALL') {
      this.invoiceService.getAll(this.currentPage, this.currentSize).subscribe({
        next: res => {
          this.invoices = res.items;
          this.totalCount = res.totalCount;
          this.refreshStats();
        },
        error: () => this.flash('error', this.translate.instant('INVOICES.ERRORS.LOAD_FAILED')),
      });
    } else {
      this.invoiceService.getByStatus(this.statusFilter, this.currentPage, this.currentSize).subscribe({
        next: res => {
          this.invoices = res.items;
          this.totalCount = res.totalCount;
        },
        error: () => this.flash('error', this.translate.instant('INVOICES.ERRORS.LOAD_FAILED')),
      });
    }
  }

  loadDeletedInvoices(): void {
    this.invoiceService.getDeleted(this.currentPage, this.currentSize).subscribe({
      next: res => {
        this.deletedInvoices = res.items;
        this.totalCount = res.totalCount;
      },
      error: () => this.flash('error', this.translate.instant('INVOICES.ERRORS.LOAD_DELETED_FAILED')),
    });
  }

  loadClients(): void {
    this.clientsService.getAll(1, 1000).subscribe({
      next: res => { this.clients = res.items; },
      error: () => {},
    });
  }

  loadArticles(): void {
    this.articleService.getAll(1, 1000).subscribe({
      next: res => { this.articles = res.items; },
      error: () => {},
    });
  }

  loadStats(): void {
    this.statsLoading = true;
    this.invoiceStats = null;
    this.invoiceService.getStats().subscribe({
      next: stats => {
        this.invoiceStats = stats;
        this.statsLoading = false;
        setTimeout(() => this.renderStatusPieChart(), 100); // Render after view update
      },
      error: () => {
        this.flash('error', this.translate.instant('INVOICES.ERRORS.LOAD_STATS_FAILED'));
        this.statsLoading = false;
      },
    });
  }

  private refreshStats(): void {
    this.stats.total = this.totalCount;
    (['DRAFT', 'UNPAID', 'PAID', 'CANCELLED'] as const).forEach(status => {
      this.invoiceService.getByStatus(status, 1, 1).subscribe({
        next: res => {
          if (status === 'DRAFT')     this.stats.draft     = res.totalCount;
          if (status === 'UNPAID')    this.stats.unpaid    = res.totalCount;
          if (status === 'PAID')      this.stats.paid      = res.totalCount;
          if (status === 'CANCELLED') this.stats.cancelled = res.totalCount;
        },
      });
    });
    this.invoiceService.getDeleted(1, 1).subscribe({
      next: res => this.stats.deleted = res.totalCount,
    });
  }

  reload(): void {
    if (this.isDeletedList()) {
      this.loadDeletedInvoices();
    } else if (this.isStats()) {
      this.loadStats();
    } else {
      this.load();
    }
    this.loadClients();
    this.loadArticles();
  }

  // ── Navigation / Mode Management ──────────────────────────────────────────

  onActiveCardClick(): void {
    if (this.isList()) return;
    this.setViewMode('list');
    this.load();
  }

  onDeletedCardClick(): void {
    if (this.isDeletedList() || this.stats.deleted < 1) return;
    this.setViewMode('list-deleted');
    this.loadDeletedInvoices();
  }

  openStats(): void {
    if (this.isStats()) return;
    this.previousMode = this.viewMode();
    this.setViewMode('stats');
    this.loadStats();
  }

  openCreate(): void {
    if (this.isCreate()) return;
    this.previousMode = this.viewMode();
    this.setViewMode('create');
    this.resetCreateForm();
    
    // Set default dates
    const today = new Date();
    const defaultInvoiceDate = today.toISOString().split('T')[0];
    
    this.invoiceForm.patchValue({
      invoiceDate: defaultInvoiceDate,
    });
    
    // Mark fields as touched to show validation errors if needed
    // But don't mark as touched immediately to avoid showing errors
  }

  openView(invoice: InvoiceDto): void {
    if (this.isView()) return;
    this.previousMode = this.viewMode();
    this.setViewMode('view');
    this.selectedInvoice = invoice;
  }

  cancel(): void {
    const target = this.resolveCancel();
    this.setViewMode(target);

    if (target !== 'view') {
      this.selectedInvoice = null;
    }

    if (target !== 'create') {
      this.resetCreateForm();
    }

    this.reload();
  }

private resolveCancel(): ViewMode {
  const current = this.viewMode();

  // view → list / list-deleted (go back to where we came from)
  if (current === 'view' && (this.previousMode === 'list' || this.previousMode === 'list-deleted')) {
    return this.previousMode;
  }

  // create → previous mode (list or list-deleted)
  if (current === 'create') {
    return this.previousMode ?? 'list';
  }

  // stats → list
  if (current === 'stats') {
    return 'list';
  }

  // fallback
  return 'list';
}

  private resetCreateForm(): void {
    // Reset form with empty values
    this.invoiceForm.reset({
      invoiceDate: '',
      dueDate: '',
      clientId: '',
      clientFullName: '',
      clientAddress: '',
      additionalNotes: null,
    });
    
    this.pendingItems = [];
    this.inlineItemOpen = false;
    this.inlineItemLocalId = '';
    this.clientSearchQuery = '';
    this.filteredClients = [];
    this.creditWarning = null;
    this.discountInfo = { applies: false, rate: 0, discountAmount: 0, originalTotal: 0, discountedTotal: 0 };
    this.selectedClientForValidation = null;
  }

  setViewMode(mode: ViewMode): void {
    this.viewMode.set(mode);
  }

  // ── Filters / sort ────────────────────────────────────────────────────────

  setStatusFilter(status: string): void {
    this.statusFilter = status;
    this.currentPage  = 1;
    this.load();
  }

  applyFilter(): void {
    // Search is already handled by sortedData getter
  }

  sortBy(col: string): void {
    if (this.sortColumn === col) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = col;
      this.sortDirection = 'asc';
    }
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.isDeletedList() ? this.loadDeletedInvoices() : this.load();
  }

  onPageSizeChange(size: number): void {
    this.currentSize = size;
    this.currentPage = 1;
    this.isDeletedList() ? this.loadDeletedInvoices() : this.load();
  }

  // ── Client autocomplete ───────────────────────────────────────────────────

  filterClients(query: string): void {
    if (!query || query.length < 2) {
      this.filteredClients = [];
      return;
    }
    const q = query.toLowerCase();
    this.filteredClients = this.clients
      .filter(c => c.name?.toLowerCase().includes(q) || c.email?.toLowerCase().includes(q))
      .slice(0, 8);
  }

  selectClient(client: ClientResponseDto): void {
    this.selectedClientForValidation = client;
    
    // Get current invoice date or use today
    let invoiceDate = this.invoiceForm.get('invoiceDate')?.value;
    if (!invoiceDate) {
      const today = new Date();
      invoiceDate = today.toISOString().split('T')[0];
      this.invoiceForm.patchValue({ invoiceDate: invoiceDate });
    }
    
    // Calculate due date based on client's duePaymentPeriod
    const dueDate = this.calculateDueDate(invoiceDate, client.duePaymentPeriod);
    
    this.invoiceForm.patchValue({
      clientId: client.id,
      clientFullName: client.name,
      clientAddress: client.address ?? '',
      dueDate: dueDate,
    });
    
    this.clientSearchQuery = client.name;
    this.filteredClients = [];
    
    // Check limits and discount immediately
    this.checkClientLimitsAndDiscount();
    
    // Log form validity for debugging
    console.log('Form valid after client selection:', this.invoiceForm.valid);
  }

  // ── Article selection ─────────────────────────────────────────────────────

  onArticleSelectChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const id = select.value;
    const article = this.articles.find(a => a.id === id);
    this.onArticleSelected(article);
  }

  onArticleSelected(article: ArticleResponseDto | undefined): void {
    if (!article) return;
    this.itemForm.patchValue({
      uniPriceHT: article.prix ?? 0,
      taxRate: article.tva ?? 19
    });
  }

  onClientSelectChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const clientId = select.value;
    const selectedClient = this.clients.find(c => c.id === clientId);
    if (selectedClient) {
      this.selectClient(selectedClient);
    }
  }

  // ── Inline item form ──────────────────────────────────────────────────────

  openInlineItemAdd(): void {
    this.itemForm.reset({ quantity: 1, uniPriceHT: 0, taxRate: 19 });
    this.inlineItemLocalId = '';
    this.inlineItemOpen    = true;
  }

  openInlineItemEdit(item: PendingItem): void {
    this.itemForm.patchValue({
      articleId: item.articleId,
      quantity: item.quantity,
      uniPriceHT: item.uniPriceHT,
      taxRate: item.taxRate,
    });
    this.inlineItemLocalId = item._localId;
    this.inlineItemOpen    = true;
  }

  closeInlineItem(): void {
    this.inlineItemOpen    = false;
    this.inlineItemLocalId = '';
    this.itemForm.reset();
  }

  submitInlineItem(): void {
    if (this.itemForm.invalid) {
      this.flash('error', this.translate.instant('VALIDATION.REQUIRED'));
      return;
    }

    const { articleId, quantity, uniPriceHT, taxRate } = this.itemForm.value;
    const article = this.articles.find(a => a.id === articleId);

    if (!article) {
      this.flash('error', this.translate.instant('ERRORS.ARTICLE_NOT_FOUND'));
      return;
    }

    const taxRateDecimal = taxRate / 100;
    const totalHT = quantity * uniPriceHT;
    const totalTTC = totalHT * (1 + taxRateDecimal);

    if (this.inlineItemLocalId) {
      const idx = this.pendingItems.findIndex(i => i._localId === this.inlineItemLocalId);
      if (idx !== -1) {
        this.pendingItems[idx] = {
          ...this.pendingItems[idx],
          articleId,
          articleName: article.libelle ?? '',
          articleBarCode: article.barCode ?? '',
          quantity,
          uniPriceHT,
          taxRate,
          totalHT,
          totalTTC,
        };
      }
    } else {
      this.pendingItems.push({
        _localId: crypto.randomUUID(),
        articleId,
        articleName: article.libelle ?? '',
        articleBarCode: article.barCode ?? '',
        quantity,
        uniPriceHT,
        taxRate,
        totalHT,
        totalTTC,
      });
    }
    this.closeInlineItem();
    this.checkClientLimitsAndDiscount();
  }

  removePendingItem(localId: string): void {
    this.pendingItems = this.pendingItems.filter(i => i._localId !== localId);
    this.checkClientLimitsAndDiscount();
  }

  get canSubmit(): boolean {
    // Check basic form validity
    if (this.invoiceForm.invalid) return false;
    
    // Check if there are items
    if (this.pendingItems.length === 0) return false;
    
    // Check if credit limit is exceeded (has credit warning)
    if (this.creditWarning) return false;
    
    // Check if validation is in progress
    if (this.isValidating) return false;
    
    return true;
  }

  getSubmitButtonTooltip(): string {
    if (this.isValidating) return 'Processing...';
    if (this.invoiceForm.invalid) return 'Please fill all required fields';
    if (this.pendingItems.length === 0) return 'Please add at least one item';
    if (this.creditWarning) return 'Credit limit exceeded. Cannot create invoice';
    return '';
  }

  // ── CRUD actions ──────────────────────────────────────────────────────────
  async submit(): Promise<void> {
    if (this.invoiceForm.invalid) {
      this.flash('error', this.translate.instant('VALIDATION.REQUIRED'));
      return;
    }

    if (this.pendingItems.length === 0) {
      this.flash('error', this.translate.instant('INVOICES.FORM.NO_ITEMS_YET'));
      return;
    }

    const formValue = this.invoiceForm.value;
    const clientId = formValue.clientId;
    
    // Find the selected client
    const selectedClient = this.clients.find(c => c.id === clientId);
    if (!selectedClient) {
      this.flash('error', 'Client not found');
      return;
    }

    // Prepare items for validation
    const items = this.pendingItems.map(({ _localId, totalHT, totalTTC, ...rest }) => ({
      articleId: rest.articleId,
      quantity: rest.quantity,
      uniPriceHT: rest.uniPriceHT,
      taxRate: rest.taxRate,
    }));

    // Show loading state
    this.isValidating = true;

    try {
      // Validate before submission
      const validation = await this.invoiceService.validateInvoiceBeforeSubmission(
        selectedClient,
        items
      );

      if (!validation.isValid) {
        this.flash('error', validation.errors.join(', '));
        this.isValidating = false;
        return;
      }

      // Show warnings
      if (validation.warnings.length > 0) {
        validation.warnings.forEach(warning => {
          this.flash('success', warning);
        });
      }

      // Apply discount to items if applicable
      let finalItems = items;
      if (validation.discountRate && validation.discountRate > 0) {
        finalItems = this.invoiceService.applyDiscountToItems(items, validation.discountRate);
      }

      // Create the DTO
      const dto: CreateInvoiceDto = {
        invoiceDate: formValue.invoiceDate,
        dueDate: formValue.dueDate,
        clientId: clientId,
        additionalNotes: formValue.additionalNotes,
        items: finalItems,
      };

      // Submit to backend
      this.invoiceService.create(dto).subscribe({
        next: () => {
          this.flash('success', this.translate.instant('INVOICES.SUCCESS.CREATED'));
          this.isValidating = false;
          this.cancel();
        },
        error: (err) => {
          const errorMsg = (err.error as HttpError)?.message || this.translate.instant('INVOICES.ERRORS.CREATE_FAILED');
          this.flash('error', errorMsg);
          this.isValidating = false;
        },
      });
    } catch (error) {
      console.error('Validation error:', error);
      this.flash('error', 'Validation failed. Please try again.');
      this.isValidating = false;
    }
  }

  finalize(invoice: InvoiceDto): void {
    this.invoiceService.finalize(invoice.id).subscribe({
      next: (updated) => {
        if (this.isView() && this.selectedInvoice?.id === updated.id) {
          this.selectedInvoice = updated;
        }
        this.flash('success', this.translate.instant('INVOICES.SUCCESS.FINALIZED'));
        this.reload();
      },
      error: () => this.flash('error', this.translate.instant('INVOICES.ERRORS.FINALIZE_FAILED')),
    });
  }

  markAsPaid(invoice: InvoiceDto): void {
    this.invoiceService.markAsPaid(invoice.id).subscribe({
      next: (updated) => {
        if (this.isView() && this.selectedInvoice?.id === updated.id) {
          this.selectedInvoice = updated;
        }
        this.flash('success', this.translate.instant('INVOICES.SUCCESS.MARKED_PAID'));
        this.reload();
      },
      error: () => this.flash('error', this.translate.instant('INVOICES.ERRORS.MARK_PAID_FAILED')),
    });
  }

  cancelInvoice(invoice: InvoiceDto): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '420px',
      data: {
        icon: 'cancel',
        iconColor: 'warn',
        title: this.translate.instant('INVOICES.DIALOG.CANCEL_INVOICE_TITLE'),
        message: this.translate.instant('INVOICES.DIALOG.CANCEL_INVOICE_MESSAGE', { number: invoice.invoiceNumber }),
        confirmText: this.translate.instant('INVOICES.DIALOG.CANCEL_CONFIRM'),
        cancelText: this.translate.instant('INVOICES.DIALOG.GO_BACK'),
        showCancel: true,
      },
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.invoiceService.cancel(invoice.id).subscribe({
          next: (updated) => {
            if (this.isView() && this.selectedInvoice?.id === updated.id) {
              this.selectedInvoice = updated;
            }
            this.flash('success', this.translate.instant('INVOICES.SUCCESS.CANCELLED'));
            this.reload();
          },
          error: () => this.flash('error', this.translate.instant('INVOICES.ERRORS.CANCEL_FAILED')),
        });
      });
  }

  delete(invoice: InvoiceDto): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '420px',
      data: {
        icon: 'delete',
        iconColor: 'warn',
        title: this.translate.instant('INVOICES.DIALOG.DELETE_INVOICE_TITLE'),
        message: this.translate.instant('INVOICES.DIALOG.DELETE_INVOICE_MESSAGE', { number: invoice.invoiceNumber }),
        confirmText: this.translate.instant('INVOICES.DIALOG.DELETE_CONFIRM'),
        cancelText: this.translate.instant('COMMON.CANCEL'),
        showCancel: true,
      },
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.invoiceService.delete(invoice.id).subscribe({
          next: () => {
            this.flash('success', this.translate.instant('INVOICES.SUCCESS.DELETED'));
            if (this.isView()) this.cancel();
            this.reload();
          },
          error: () => this.flash('error', this.translate.instant('INVOICES.ERRORS.DELETE_FAILED')),
        });
      });
  }

  restore(invoice: InvoiceDto): void {
    this.invoiceService.restore(invoice.id).subscribe({
      next: () => {
        this.flash('success', this.translate.instant('INVOICES.SUCCESS.RESTORED'));
        if (this.isView()) this.cancel();
        if (this.isDeletedList()) {
          this.loadDeletedInvoices();
        } else {
          this.reload();
        }
      },
      error: () => this.flash('error', this.translate.instant('INVOICES.ERRORS.RESTORE_FAILED')),
    });
  }

  // ── Feedback ──────────────────────────────────────────────────────────────

  flash(type: 'success' | 'error', msg: string): void {
    if (type === 'success') {
      this.successMessage = msg;
      setTimeout(() => { this.successMessage = null; }, 3000);
    } else {
      this.errors = [msg];
      setTimeout(() => { this.errors = []; }, 4000);
    }
  }

  dismissError(): void {
    this.errors = [];
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  isDateOverdue(date: string | Date): boolean {
    if (!date) return false;
    const today = new Date();
    const compareDate = new Date(date);
    today.setHours(0, 0, 0, 0);
    compareDate.setHours(0, 0, 0, 0);
    return compareDate < today;
  }

  statusClass(status: string): Record<string, boolean> {
    return {
      'badge--amber': status === 'DRAFT',
      'badge--red':   status === 'UNPAID',
      'badge--green': status === 'PAID',
      'badge--grey':  status === 'CANCELLED',
    };
  }

  get pendingTotalHT(): number {
    return this.pendingItems.reduce((s, i) => s + i.totalHT, 0);
  }

  get pendingTotalTTC(): number {
    return this.pendingItems.reduce((s, i) => s + i.totalTTC, 0);
  }

  get pendingTotalTVA(): number {
    return this.pendingTotalTTC - this.pendingTotalHT;
  }

  monthName(month: number): string {
    return new Date(2000, month - 1).toLocaleString('default', { month: 'short' });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('fr-TN', { style: 'currency', currency: 'TND' }).format(amount);
  }

  trackById(_: number, item: { id: string }) {
    return item.id;
  }

  trackByLocalId(_: number, item: PendingItem) {
    return item._localId;
  }

  trackByMonth(_: number, item: { month: number }) {
    return item.month;
  }

  private getCSSVariable(variableName: string, fallback: string = '#ffffff'): string {
    const styles = getComputedStyle(document.documentElement);
    const value = styles.getPropertyValue(variableName).trim();
    return value || fallback;
  }

  private renderStatusPieChart(): void {
  if (!this.monthlyChartRef || !this.invoiceStats) return;

  if (this.chart) {
    this.chart.destroy();
  }

  const textHi = this.getCSSVariable('--text-hi', '#ffffff');
  const textMid = this.getCSSVariable('--text-mid', '#8b92a8');

  // Status data
  const statusLabels = [
    this.translate.instant('INVOICES.STATUS.DRAFT'),
    this.translate.instant('INVOICES.STATUS.UNPAID'),
    this.translate.instant('INVOICES.STATUS.PAID'),
    this.translate.instant('INVOICES.STATUS.CANCELLED')
  ];

  const statusCounts = [
    this.invoiceStats.draftCount,
    this.invoiceStats.unpaidCount,
    this.invoiceStats.paidCount,
    this.invoiceStats.cancelledCount
  ];

  const statusColors = [
    '#f5a623', // amber - DRAFT
    '#e05252', // red - UNPAID
    '#3ecf8e', // green - PAID
    '#8b92a8'  // grey - CANCELLED
  ];

  const config: ChartConfiguration = {
    type: 'doughnut',
    data: {
      labels: statusLabels,
      datasets: [
        {
          data: statusCounts,
          backgroundColor: statusColors,
          borderColor: '#fff',
          borderWidth: 2,
        }
      ]
    },
    options: {
      responsive: true,
      maintainAspectRatio: true,
      plugins: {
        legend: {
          position: 'right',
          labels: {
            color: textHi,
            font: { family: 'Outfit, sans-serif', size: 12 },
            usePointStyle: true,
            boxWidth: 10,
            padding: 15,
          }
        },
        tooltip: {
          callbacks: {
            label: (context) => {
              const label = context.label || '';
              const value = context.raw as number;
              const total = statusCounts.reduce((a, b) => a + b, 0);
              const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
              return `${label}: ${value} (${percentage}%)`;
            }
          }
        }
      }
    }
  };

  this.chart = new Chart(this.monthlyChartRef.nativeElement, config);
}

  private observeThemeChanges(): void {
    const observer = new MutationObserver(() => {
      this.renderStatusPieChart(); // Re-render chart when theme changes
    });

    observer.observe(document.documentElement, {
      attributes: true,
      attributeFilter: ['class', 'data-theme']
    });
  }


  // Add this method to check credit limit and discount in real-time
  async checkClientLimitsAndDiscount(): Promise<void> {
      if (!this.selectedClientForValidation || this.pendingItems.length === 0) {
        this.creditWarning = null;
        this.discountInfo = { applies: false, rate: 0, discountAmount: 0, originalTotal: 0, discountedTotal: 0 };
        return;
      }

      const items = this.pendingItems.map(({ _localId, totalHT, totalTTC, ...rest }) => ({
        articleId: rest.articleId,
        quantity: rest.quantity,
        uniPriceHT: rest.uniPriceHT,
        taxRate: rest.taxRate,
      }));

      // Calculate discount
      const { discountRate, applies } = this.invoiceService.calculateBulkDiscount(this.selectedClientForValidation);
      const { originalTotalTTC, discountedTotalTTC, discountAmount } = this.invoiceService.calculateDiscountedTotals(items, discountRate);
      
      this.discountInfo = {
        applies,
        rate: discountRate,
        discountAmount,
        originalTotal: originalTotalTTC,
        discountedTotal: discountedTotalTTC
      };

      // Check credit limit
      try {
        const currentOutstanding = await this.invoiceService.getClientOutstandingBalance(this.selectedClientForValidation.id).toPromise();
        const creditCheck = this.invoiceService.validateCreditLimit(
          this.selectedClientForValidation, 
          discountedTotalTTC, 
          currentOutstanding || 0
        );
        
        if (!creditCheck.hasSufficientCredit) {
          this.creditWarning = creditCheck.message;
        } else {
          this.creditWarning = null;
        }
      } catch (error) {
        console.error('Credit check failed:', error);
      }
  }


  // Calculate due date based on invoice date and payment period
  calculateDueDate(invoiceDate: string | Date | null | undefined, paymentPeriod: number | null | undefined): string {
    // Default to 30 days if no payment period
    const daysToAdd = paymentPeriod || 30;
    
    let date: Date;
    if (!invoiceDate) {
      date = new Date();
    } else {
      date = new Date(invoiceDate);
    }
    
    const dueDate = new Date(date);
    dueDate.setDate(date.getDate() + daysToAdd);
    return dueDate.toISOString().split('T')[0];
  }
  shouldShowError(controlName: string): boolean {
    const control = this.invoiceForm.get(controlName);
    return control ? (control.invalid && (control.dirty || control.touched)) : false;
  }

  onInvoiceDateChange(): void {
    const invoiceDate = this.invoiceForm.get('invoiceDate')?.value;
    if (this.selectedClientForValidation && invoiceDate) {
      const dueDate = this.calculateDueDate(invoiceDate, this.selectedClientForValidation.duePaymentPeriod);
      this.invoiceForm.patchValue({ dueDate: dueDate });
    }
  }




}
