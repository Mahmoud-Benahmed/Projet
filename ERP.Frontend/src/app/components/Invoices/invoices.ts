import { Component, OnInit, OnDestroy, signal, computed, inject, DestroyRef, ViewChild, ElementRef, ChangeDetectorRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { catchError, firstValueFrom, forkJoin, map, Observable, of } from 'rxjs';

import { AuthService, PRIVILEGES } from '../../services/auth/auth.service';
import { InvoiceService, InvoiceDto, CreateInvoiceDto, InvoiceStatsDto } from '../../services/invoice.service';
import { ClientsService, ClientResponseDto } from '../../services/clients/clients.service';
import { ArticleService, UnitEnum } from '../../services/articles/articles.service';
import { PaginationComponent } from '../pagination/pagination';
import { ModalComponent } from '../modal/modal';
import { HttpError } from '../../interfaces/ErrorDto';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { StockItem, StockService } from '../../services/stock.service';

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

type CreditLimitInfo= {
    hasSufficientCredit: boolean,
    currentUsage: number,
    remainingCredit: number
};


type ViewMode = 'list' | 'list-deleted' | 'create'|'edit' | 'view' | 'stats';

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
export class InvoicesComponent implements OnInit, OnDestroy{
  @ViewChild('monthlyChart') monthlyChartRef!: ElementRef<HTMLCanvasElement>;
  private chart: Chart | null = null;
  private themeObserver: MutationObserver | null = null;
  private readonly destroyRef = inject(DestroyRef);
  private translate = inject(TranslateService);
  private cdr = inject(ChangeDetectorRef);
  

  creditWarning: string | null = null;
  creditLimitInfo: CreditLimitInfo = {
    hasSufficientCredit: true,
    currentUsage: 0,
    remainingCredit: 0
  };

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
  readonly units = UnitEnum;

  private masterArticles: StockItem[] = [];
  articles: StockItem[] = [];

  // Instead of getter, use signal
  invoiceTotalTTC = computed(() => {
    if (this.discountInfo.applies && this.discountInfo.discountedTotal > 0) {
      return this.discountInfo.discountedTotal;
    }
    return this.pendingTotalTTC;
  });



  // ── View mode ──────────────────────────────────────────────────────────────
  viewMode = signal<ViewMode>('list');
  private previousMode: ViewMode = 'list';

  isMode = (mode: ViewMode) => computed(() => this.viewMode() === mode);

  isList = this.isMode('list');
  isDeletedList = this.isMode('list-deleted');
  isCreate = this.isMode('create');
  isEdit = this.isMode('edit');
  isView = this.isMode('view');
  isStats = this.isMode('stats');

  // ── Invoice list state ─────────────────────────────────────────────────────
  invoices: InvoiceDto[] = [];
  deletedInvoices: InvoiceDto[] = [];
  selectedInvoice: InvoiceDto | null = null;

  totalCount = 0;
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
  searchQuery = '';
  statusFilter = 'ALL';
  sortColumn = '';
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

  // ── Clients ───────────────────────────────────────────────────────────────
  clients: ClientResponseDto[] = [];
  filteredClients: ClientResponseDto[] = [];
  clientSearchQuery = '';

  // ── Pending line items (create view) ──────────────────────────────────────
  pendingItems: PendingItem[] = [];
  inlineItemOpen = false;
  inlineItemLocalId = '';

  // Cached selected article for template bindings to avoid repeated find() per CD cycle
  private _selectedArticle: StockItem | null = null;

  constructor(
    public authService: AuthService,
    private invoiceService: InvoiceService,
    private clientsService: ClientsService,
    private articleService: ArticleService,
    private fb: FormBuilder,
    private dialog: MatDialog,
    private stock: StockService
  ) {}

  ngAfterViewInit(): void {
    this.observeThemeChanges();
  }

  ngOnInit(): void {
    this.buildForms();
    this.reload();
  }

  ngOnDestroy(): void {
    this.themeObserver?.disconnect();
    if (this.chart) {
      this.chart.destroy();
      this.chart = null;
    }
  }

  // ── Page title ────────────────────────────────────────────────────────────
  get pageTitle(): string {
    if (this.isCreate()) return 'INVOICES.TITLE_NEW';
    if (this.isView()) return 'INVOICES.TITLE_DETAILS';
    if (this.isDeletedList()) return 'INVOICES.TITLE_DELETED';
    if (this.isStats()) return 'INVOICES.TITLE_ANALYTICS';
    return 'INVOICES.TITLE_LIST';
  }

  get duePaymentPeriodHint(): string {
    if (!this.selectedClientForValidation?.duePaymentPeriod) return '';
    return this.translate.instant('INVOICES.FORM.DUE_DATE_HINT', {
      days: this.selectedClientForValidation.duePaymentPeriod
    });
  }

  loadCreditLimitInfo(): void {
    if (!this.selectedClientForValidation?.id) return;

    this.invoiceService.getClientOutstandingBalance(this.selectedClientForValidation.id).subscribe({
      next: (currentOutstanding) => {
        const result = this.invoiceService.validateCreditLimit(
          this.selectedClientForValidation,
          this.invoiceTotalTTC(),  // Note the parentheses for signal
          currentOutstanding
        );
        
        this.creditLimitInfo = {
          hasSufficientCredit: result.hasSufficientCredit,
          currentUsage: result.currentUsage,
          remainingCredit: result.remainingCredit
        };
      },
      error: () => {
        this.creditLimitInfo = {
          hasSufficientCredit: false,
          currentUsage: 0,
          remainingCredit: 0
        };
      }
    });
  }

  // ── Forms ─────────────────────────────────────────────────────────────────
  private buildForms(): void {
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

    this.invoiceForm.get('invoiceDate')?.valueChanges.subscribe(() => {
      this.onInvoiceDateChange();
    });

    // Clear cached selected article when articleId changes
    this.itemForm.get('articleId')?.valueChanges.subscribe(id => {
      this._selectedArticle = id ? (this.articles.find(a => a.id === id) ?? null) : null;
    });
  }

  // ── Load data ─────────────────────────────────────────────────────────────
  load(): void {
    const req$ = this.statusFilter === 'ALL'
      ? this.invoiceService.getAll(this.currentPage, this.currentSize)
      : this.invoiceService.getByStatus(this.statusFilter, this.currentPage, this.currentSize);

    req$.subscribe({
      next: res => {
        this.invoices = res.items;
        this.totalCount = res.totalCount;
        if (this.statusFilter === 'ALL') this.refreshStats();
      },
      error: () => this.flash('error', this.translate.instant('INVOICES.ERRORS.LOAD_FAILED')),
    });
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

  private loadArticlesWithStock(): void {
    forkJoin({
      articles: this.articleService.getAll(1, 1000).pipe(catchError(() => of({ items: [] }))),
      stock: this.stock.getStockArticles().pipe(catchError(() => of({ inStock: [], outStock: [] })))
    }).subscribe({
      next: (results) => {
        const allArticles = results.articles.items || [];
        const stockData = results.stock || { inStock: [], outStock: [] };

        const stockMap = new Map<string, number>();
        stockData.inStock.forEach((s: any) => {
          stockMap.set(s.articleId || s.id, s.quantity);
        });

        // Store original articles (never modified)
        this.masterArticles = allArticles
          .filter((a: any) => stockMap.has(a.id) && stockMap.get(a.id)! > 0)
          .map((a: any) => ({ ...a, quantity: stockMap.get(a.id)! }));

        this.syncArticles();

        this.cdr.markForCheck();
      },
      error: () => {
        this.syncArticles();
        this.articles = [];
      }
    });
  }

  loadStats(): void {
    this.statsLoading = true;
    this.invoiceStats = null;
    this.invoiceService.getStats().subscribe({
      next: stats => {
        this.invoiceStats = stats;
        this.statsLoading = false;
        setTimeout(() => this.renderStatusPieChart(), 100);
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
    this.loadClients();
    this.loadArticlesWithStock();

    if (this.isDeletedList()) {
      this.loadDeletedInvoices();
    } else if (this.isStats()) {
      this.loadStats();
    } else {
      this.load();
    }
  }

  // ── Navigation / Mode Management ──────────────────────────────────────────

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

    const today = new Date();
    this.invoiceForm.patchValue({ invoiceDate: today.toISOString().split('T')[0] });
  }

  openEdit(invoice: InvoiceDto): void {
    if (this.isEdit()) return;
    
    this.previousMode = this.viewMode();
    this.setViewMode('edit');
    this.selectedInvoice = invoice;
    
    
    // Format dates
    const formattedInvoiceDate = invoice.invoiceDate ? new Date(invoice.invoiceDate).toISOString().split('T')[0] : '';
    const formattedDueDate = invoice.dueDate ? new Date(invoice.dueDate).toISOString().split('T')[0] : '';
    
    // Populate form
    this.invoiceForm.patchValue({
      invoiceDate: formattedInvoiceDate,
      dueDate: formattedDueDate,
      clientId: invoice.clientId,
      clientFullName: invoice.clientFullName,
      clientAddress: invoice.clientAddress,
      additionalNotes: invoice.additionalNotes,
    });
    
    // Populate pending items
    this.pendingItems = invoice.items.map(item => ({
      _localId: crypto.randomUUID(),
      articleId: item.articleId,
      articleName: item.articleName,
      articleBarCode: item.articleBarCode,
      quantity: item.quantity,
      uniPriceHT: item.uniPriceHT,
      taxRate: item.taxRate,
      totalHT: item.totalHT,
      totalTTC: item.totalTTC
    }));
    this.syncArticles();
    
    // Find and set the selected client
    const client = this.clients.find(c => c.id === invoice.clientId);
    if (client) {
      this.selectedClientForValidation = client;
    }
    
    this.cdr.markForCheck();
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

    if (target !== 'view') this.selectedInvoice = null;
    if (target !== 'create') this.resetCreateForm();

    this.reload();
  }

  private resolveCancel(): ViewMode {
    const current = this.viewMode();
    if (current === 'view' && (this.previousMode === 'list' || this.previousMode === 'list-deleted')) {
      return this.previousMode;
    }
    if (current === 'create') return this.previousMode ?? 'list';
    if (current === 'stats') return 'list';
    this.resetCreateForm();
    return 'list';
  }

  private resetCreateForm(): void {
    // Reset form values
    this.invoiceForm.reset({
      invoiceDate: '',
      dueDate: '',
      clientId: '',
      clientFullName: '',
      clientAddress: '',
      additionalNotes: null,
    });
    
    // Mark form as pristine and untouched
    this.invoiceForm.markAsPristine();
    this.invoiceForm.markAsUntouched();
    
    // Reset all form controls individually
    Object.keys(this.invoiceForm.controls).forEach(key => {
      const control = this.invoiceForm.get(key);
      control?.markAsPristine();
      control?.markAsUntouched();
      control?.setErrors(null);
    });
    
    // Clear pending items
    this.pendingItems = [];
    this.inlineItemOpen = false;
    this.inlineItemLocalId = '';
    this.clientSearchQuery = '';
    this.filteredClients = [];
    this.creditWarning = null;
    this.discountInfo = { applies: false, rate: 0, discountAmount: 0, originalTotal: 0, discountedTotal: 0 };
    this.selectedClientForValidation = null;
    this._selectedArticle = null;
    
    // Reset available articles to original stock
    this.syncArticles();
    
    
    this.cdr.markForCheck();
  }

  setViewMode(mode: ViewMode): void {
    this.viewMode.set(mode);
  }

  // ── Filters / sort ────────────────────────────────────────────────────────

  setStatusFilter(status: string): void {
    this.statusFilter = status;
    this.currentPage = 1;
    this.load();
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
    if (!query || query.length < 2) { this.filteredClients = []; return; }
    const q = query.toLowerCase();
    this.filteredClients = this.clients
      .filter(c => c.name?.toLowerCase().includes(q) || c.email?.toLowerCase().includes(q))
      .slice(0, 8);
  }

  selectClient(client: ClientResponseDto): void {
    this.selectedClientForValidation = client;

    let invoiceDate = this.invoiceForm.get('invoiceDate')?.value;
    if (!invoiceDate) {
      invoiceDate = new Date().toISOString().split('T')[0];
      this.invoiceForm.patchValue({ invoiceDate });
    }

    this.invoiceForm.patchValue({
      clientId: client.id,
      clientFullName: client.name,
      clientAddress: client.address ?? '',
      dueDate: this.calculateDueDate(invoiceDate, client.duePaymentPeriod),
    });

    // Mark form as dirty to trigger unsaved changes detection
    this.invoiceForm.markAsDirty();
    
    this.clientSearchQuery = client.name;
    this.filteredClients = [];
    this.checkClientLimitsAndDiscount();
  }

  onClientSelectChange(event: Event): void {
    const id = (event.target as HTMLSelectElement).value;
    const client = this.clients.find(c => c.id === id);
    if (client) this.selectClient(client);
    this.loadCreditLimitInfo();
  }

  // ── Article selection ─────────────────────────────────────────────────────

  onArticleSelectChange(event: Event): void {
    const id = (event.target as HTMLSelectElement).value;
    const article = this.articles.find(a => a.id === id);
    this.onArticleSelected(article);
  }

  onArticleSelected(article: StockItem | undefined): void {
    if (!article) return;
    this._selectedArticle = article;
    this.itemForm.patchValue({
      uniPriceHT: article.prix ?? 0,
      taxRate: article.tva ?? 19,
    });
    this.updateQuantityValidator(article.quantity ?? 0);
  }

  // ── Inline item form ──────────────────────────────────────────────────────

  openInlineItemAdd(): void {
    this.itemForm.reset({ articleId: '', quantity: 1, uniPriceHT: 0, taxRate: 19 });
    this._selectedArticle = null;
    this.inlineItemLocalId = '';
    this.inlineItemOpen = true;
  }

  openInlineItemEdit(item: PendingItem): void {
    this.inlineItemLocalId = item._localId;  // set BEFORE syncArticles
    this._selectedArticle = this.masterArticles.find(a => a.id === item.articleId) ?? null;
    this.syncArticles();                     // now the editing line is excluded from consumed
    this.itemForm.patchValue({
      articleId:  item.articleId,
      quantity:   item.quantity,
      uniPriceHT: item.uniPriceHT,
      taxRate:    item.taxRate,
    });
    if (this._selectedArticle) this.updateQuantityValidator(this._selectedArticle.quantity);
    this.inlineItemOpen = true;
  }

  closeInlineItem(): void {
    this.inlineItemOpen = false;
    this.inlineItemLocalId = '';
    this._selectedArticle = null;
    this.itemForm.reset();
    this.syncArticles();
  }

  submitInlineItem(): void {
    if (this.itemForm.invalid) {
      this.flash('error', this.translate.instant('VALIDATION.REQUIRED'));
      return;
    }

    const { articleId, quantity, uniPriceHT, taxRate } = this.itemForm.value;

    // Use masterArticles for the real stock ceiling
    const master = this.masterArticles.find(a => a.id === articleId);
    if (!master) {
      this.flash('error', this.translate.instant('ERRORS.ARTICLE_NOT_FOUND'));
      return;
    }

    // Total already consumed by OTHER pending lines (excluding the one being edited)
    const alreadyConsumed = this.pendingItems
      .filter(i => i.articleId === articleId && i._localId !== this.inlineItemLocalId)
      .reduce((sum, i) => sum + i.quantity, 0);

    const maxAllowed = master.quantity - alreadyConsumed;

    if (quantity > maxAllowed) {
      this.flash('error', this.translate.instant('STOCK.ERRORS.INSUFFICIENT_STOCK', {
        max: maxAllowed, requested: quantity
      }));
      return;
    }

    const totalHT  = quantity * uniPriceHT;
    const totalTTC = totalHT * (1 + (taxRate / 100));

    if (this.inlineItemLocalId) {
      // Editing existing line
      const idx = this.pendingItems.findIndex(i => i._localId === this.inlineItemLocalId);
      if (idx !== -1) {
        this.pendingItems[idx] = {
          ...this.pendingItems[idx],
          articleId, articleName: master.libelle ?? '',
          articleBarCode: master.barCode ?? '',
          quantity, uniPriceHT, taxRate, totalHT, totalTTC,
        };
      }
    } else {
      // Adding — merge if same article already exists
      const existingIndex = this.pendingItems.findIndex(i => i.articleId === articleId);
      if (existingIndex !== -1) {
        const existing   = this.pendingItems[existingIndex];
        const newQuantity = existing.quantity + quantity;

        if (newQuantity > maxAllowed) {
          this.flash('error', this.translate.instant('STOCK.ERRORS.MERGED_QUANTITY_EXCEEDS_STOCK', {
            article: master.libelle, total: newQuantity, max: maxAllowed
          }));
          return;
        }

        this.pendingItems[existingIndex] = {
          ...existing,
          quantity:  newQuantity,
          totalHT:   newQuantity * existing.uniPriceHT,
          totalTTC:  newQuantity * existing.uniPriceHT * (1 + (existing.taxRate / 100)),
        };
      } else {
        this.pendingItems.push({
          _localId: crypto.randomUUID(),
          articleId, articleName: master.libelle ?? '',
          articleBarCode: master.barCode ?? '',
          quantity, uniPriceHT, taxRate, totalHT, totalTTC,
        });
      }
    }

    this.pendingItems = [...this.pendingItems];
    this.invoiceForm.markAsDirty();
    this.closeInlineItem();
    this.syncArticles();
    this.checkClientLimitsAndDiscount();
  }

  removePendingItem(localId: string): void {
    this.pendingItems = this.pendingItems.filter(i => i._localId !== localId);
    this.syncArticles();
    this.checkClientLimitsAndDiscount();
  }

  get canSubmit(): boolean {
    if (this.invoiceForm.invalid) return false;
    if (this.pendingItems.length === 0) return false;
    if (this.creditWarning) return false;
    if (this.isValidating) return false;
    if(!this.creditLimitInfo.hasSufficientCredit && this.selectedClientForValidation?.creditLimit) return false;

    return true;
  }

  getSubmitButtonTooltip(): string {
    if(!this.creditLimitInfo.hasSufficientCredit && this.selectedClientForValidation?.creditLimit) return this.translate.instant('INVOICES.ERRORS.INSUFFICIENT_CREDIT', {
        creditLimit: this.selectedClientForValidation.creditLimit.toFixed(2),
        currentOutstanding: this.creditLimitInfo.currentUsage.toFixed(2),
        invoiceTotal: this.invoiceTotalTTC()
      });
    if (this.isValidating) return this.translate.instant('COMMON.PROCESSING');
    if (this.invoiceForm.invalid) return this.translate.instant('VALIDATION.REQUIRED');
    if (this.pendingItems.length === 0) return this.translate.instant('INVOICES.FORM.NO_ITEMS_YET');
    if (this.creditWarning) return this.translate.instant('INVOICES.ERRORS.CREDIT_LIMIT_EXCEEDED');
    return '';
  }

  async validateAllItemsStock(): Promise<boolean> {
    const stockChecks = this.pendingItems.map(async (item) => {
      try {
        const response = await firstValueFrom(this.stock.getArticleCurrentStock(item.articleId));
        const currentStock = response?.currentStock ?? 0;
        if (currentStock < item.quantity) {
          return { valid: false, articleName: item.articleName, requested: item.quantity, available: currentStock };
        }
        return { valid: true };
      } catch {
        return { valid: true };
      }
    });

    const results = await Promise.all(stockChecks);
    const failures = results.filter(r => !r.valid);

    if (failures.length > 0) {
      const msgs = failures.map(f =>
        this.translate.instant('STOCK.ERRORS.INSUFFICIENT_STOCK_DETAIL', {
          article: (f as any).articleName,
          requested: (f as any).requested,
          available: (f as any).available,
        })
      );
      this.flash('error', msgs.join('; '));
      return false;
    }
    return true;
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

    const stockValid = await this.validateAllItemsStock();
    if (!stockValid) return;

    const formValue = this.invoiceForm.value;
    const selectedClient = this.clients.find(c => c.id === formValue.clientId);
    if (!selectedClient) {
      this.flash('error', this.translate.instant('INVOICES.ERRORS.CLIENT_NOT_FOUND'));
      return;
    }

    const items = this.pendingItems.map(({ _localId, totalHT, totalTTC, ...rest }) => ({
      articleId: rest.articleId,  // Ensure this is string
      quantity: Number(rest.quantity),  // Force to number
      uniPriceHT: Number(rest.uniPriceHT),  // Force to number
      taxRate: Number(rest.taxRate),  // Force to number
    }));

    this.isValidating = true;

    try {
      const validation = await this.invoiceService.validateInvoiceBeforeSubmission(selectedClient, items);

      if (!validation.isValid) {
        this.flash('error', validation.errors.join(', '));
        this.isValidating = false;
        return;
      }

      validation.warnings.forEach(w => this.flash('success', w));

      let finalItems = items;
      if (validation.discountRate && validation.discountRate > 0) {
        finalItems = this.invoiceService.applyDiscountToItems(items, validation.discountRate);
      }

      const dto: CreateInvoiceDto = {
        invoiceDate: formValue.invoiceDate,
        dueDate: formValue.dueDate,
        clientId: formValue.clientId,
        additionalNotes: formValue.additionalNotes,
        items: finalItems,
      };

      this.invoiceService.create(dto).subscribe({
        next: () => {
          this.flash('success', this.translate.instant('INVOICES.SUCCESS.CREATED'));
          this.cancel();
        },
        error: (err) => {
          const errorMsg = (err.error as HttpError)?.message || this.translate.instant('INVOICES.ERRORS.CREATE_FAILED');
          this.flash('error', errorMsg);
          this.isValidating = false;
        },
      });
    } catch {
      this.flash('error', this.translate.instant('INVOICES.ERRORS.VALIDATION_FAILED'));
      this.isValidating = false;
    }
  }

  finalize(invoice: InvoiceDto): void {
    this.invoiceService.finalize(invoice.id).subscribe({
      next: (updated) => {
        if (this.isEdit() && this.selectedInvoice?.id === updated.id) this.selectedInvoice = updated;
        this.flash('success', this.translate.instant('INVOICES.SUCCESS.FINALIZED'));
        this.reload();
      },
      error: () => this.flash('error', this.translate.instant('INVOICES.ERRORS.FINALIZE_FAILED')),
    });
  }

  markAsPaid(invoice: InvoiceDto): void {
    this.invoiceService.markAsPaid(invoice.id).subscribe({
      next: (updated) => {
        if (this.isView() && this.selectedInvoice?.id === updated.id) this.selectedInvoice = updated;
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
        icon: 'cancel', iconColor: 'warn',
        title: this.translate.instant('INVOICES.DIALOG.CANCEL_INVOICE_TITLE'),
        message: this.translate.instant('INVOICES.DIALOG.CANCEL_INVOICE_MESSAGE', { number: invoice.invoiceNumber }),
        confirmText: this.translate.instant('INVOICES.DIALOG.CANCEL_CONFIRM'),
        cancelText: this.translate.instant('INVOICES.DIALOG.GO_BACK'),
        showCancel: true,
      },
    });
    dialogRef.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(confirmed => {
      if (!confirmed) return;
      this.invoiceService.cancel(invoice.id).subscribe({
        next: (updated) => {
          if (this.isView() && this.selectedInvoice?.id === updated.id) this.selectedInvoice = updated;
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
        icon: 'delete', iconColor: 'warn',
        title: this.translate.instant('INVOICES.DIALOG.DELETE_INVOICE_TITLE'),
        message: this.translate.instant('INVOICES.DIALOG.DELETE_INVOICE_MESSAGE', { number: invoice.invoiceNumber }),
        confirmText: this.translate.instant('INVOICES.DIALOG.DELETE_CONFIRM'),
        cancelText: this.translate.instant('COMMON.CANCEL'),
        showCancel: true,
      },
    });
    dialogRef.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(confirmed => {
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
        if (this.isDeletedList()) this.loadDeletedInvoices();
        else this.reload();
      },
      error: () => this.flash('error', this.translate.instant('INVOICES.ERRORS.RESTORE_FAILED')),
    });
  }

  updateInvoice(): void {
    if (this.invoiceForm.invalid) {
      this.flash('error', this.translate.instant('VALIDATION.REQUIRED'));
      return;
    }
    if (this.pendingItems.length === 0) {
      this.flash('error', this.translate.instant('INVOICES.FORM.NO_ITEMS_YET'));
      return;
    }

    this.isValidating = true;
    const formValue = this.invoiceForm.value;

    const items = this.pendingItems.map(item => ({
      articleId: item.articleId,
      quantity: item.quantity,
      uniPriceHT: item.uniPriceHT,
      taxRate: item.taxRate, // This should already be in percentage (0-100)
    }));

    const updateDto = {
      invoiceDate: formValue.invoiceDate,
      dueDate: formValue.dueDate,
      clientId: formValue.clientId,
      clientAddress: formValue.clientAddress,
      additionalNotes: formValue.additionalNotes,
      items: items
    };

    this.invoiceService.update(this.selectedInvoice!.id, updateDto).subscribe({
      next: () => {
        this.flash('success', this.translate.instant('INVOICES.SUCCESS.UPDATED'));
        this.isValidating = false; // ✅ add this
        this.cancel();
        this.reload();
      },
      error: (err) => {
        const errorMsg = (err.error as HttpError)?.message || this.translate.instant('INVOICES.ERRORS.UPDATE_FAILED');
        this.flash('error', errorMsg);
        this.isValidating = false;
      },
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

  dismissError(): void { this.errors = []; }

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

  get pendingTotalHT(): number { return this.pendingItems.reduce((s, i) => s + i.totalHT, 0); }
  get pendingTotalTTC(): number { return this.pendingItems.reduce((s, i) => s + i.totalTTC, 0); }
  get pendingTotalTVA(): number { return this.pendingTotalTTC - this.pendingTotalHT; }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('fr-TN', { style: 'currency', currency: 'TND' }).format(amount);
  }

  trackById(_: number, item: { id: string }) { return item.id; }
  trackByLocalId(_: number, item: PendingItem) { return item._localId; }

  private getCSSVariable(variableName: string, fallback = '#ffffff'): string {
    return getComputedStyle(document.documentElement).getPropertyValue(variableName).trim() || fallback;
  }

  private renderStatusPieChart(): void {
    if (!this.monthlyChartRef || !this.invoiceStats) return;
    if (this.chart) { this.chart.destroy(); }

    const textHi = this.getCSSVariable('--text-hi', '#ffffff');
    const statusLabels = [
      this.translate.instant('INVOICES.STATUS.DRAFT'),
      this.translate.instant('INVOICES.STATUS.UNPAID'),
      this.translate.instant('INVOICES.STATUS.PAID'),
      this.translate.instant('INVOICES.STATUS.CANCELLED'),
    ];
    const statusCounts = [
      this.invoiceStats.draftCount,
      this.invoiceStats.unpaidCount,
      this.invoiceStats.paidCount,
      this.invoiceStats.cancelledCount,
    ];

    const config: ChartConfiguration = {
      type: 'doughnut',
      data: {
        labels: statusLabels,
        datasets: [{
          data: statusCounts,
          backgroundColor: ['#f5a623', '#e05252', '#3ecf8e', '#8b92a8'],
          borderColor: '#fff',
          borderWidth: 2,
        }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: true,
        plugins: {
          legend: {
            position: 'right',
            labels: { color: textHi, font: { family: 'Outfit, sans-serif', size: 12 }, usePointStyle: true, boxWidth: 10, padding: 15 },
          },
          tooltip: {
            callbacks: {
              label: (ctx) => {
                const total = statusCounts.reduce((a, b) => a + b, 0);
                const pct = total > 0 ? ((ctx.raw as number / total) * 100).toFixed(1) : 0;
                return `${ctx.label}: ${ctx.raw} (${pct}%)`;
              },
            },
          },
        },
      },
    };

    this.chart = new Chart(this.monthlyChartRef.nativeElement, config);
  }

  private observeThemeChanges(): void {
    this.themeObserver = new MutationObserver(() => {
      if (this.isStats() && this.invoiceStats) this.renderStatusPieChart();
    });
    this.themeObserver.observe(document.documentElement, {
      attributes: true, attributeFilter: ['class', 'data-theme'],
    });
  }

  async checkClientLimitsAndDiscount(): Promise<void> {
    if (!this.selectedClientForValidation || this.pendingItems.length === 0) {
      this.creditWarning = null;
      this.discountInfo = { applies: false, rate: 0, discountAmount: 0, originalTotal: 0, discountedTotal: 0 };
      return;
    }

    const items = this.pendingItems.map(({ _localId, totalHT, totalTTC, ...rest }) => ({
      articleId: rest.articleId, quantity: rest.quantity,
      uniPriceHT: rest.uniPriceHT, taxRate: rest.taxRate,
    }));

    const { discountRate, applies } = this.invoiceService.calculateBulkDiscount(this.selectedClientForValidation);
    const { originalTotalTTC, discountedTotalTTC, discountAmount } =
      this.invoiceService.calculateDiscountedTotals(items, discountRate);

    this.discountInfo = { applies, rate: discountRate, discountAmount, originalTotal: originalTotalTTC, discountedTotal: discountedTotalTTC };

    try {
      const outstanding = await firstValueFrom(this.invoiceService.getClientOutstandingBalance(this.selectedClientForValidation.id));
      const creditCheck = this.invoiceService.validateCreditLimit(this.selectedClientForValidation, discountedTotalTTC, outstanding || 0);
      this.creditWarning = creditCheck.hasSufficientCredit ? null : creditCheck.message;
    } catch {
      // silently ignore credit check failures
    }
  }

  calculateDueDate(invoiceDate: string | Date | null | undefined, paymentPeriod: number | null | undefined): string {
    const daysToAdd = paymentPeriod || 30;
    const date = invoiceDate ? new Date(invoiceDate) : new Date();
    const due = new Date(date);
    due.setDate(date.getDate() + daysToAdd);
    return due.toISOString().split('T')[0];
  }

  onInvoiceDateChange(): void {
    const invoiceDate = this.invoiceForm.get('invoiceDate')?.value;
    if (this.selectedClientForValidation && invoiceDate) {
      this.invoiceForm.patchValue({
        dueDate: this.calculateDueDate(invoiceDate, this.selectedClientForValidation.duePaymentPeriod),
      });
    }
  }

  getStockStatusClass(availableStock: number): string {
    if (availableStock === 0) return 'stock-out';
    if (availableStock <= 5) return 'stock-critical';
    if (availableStock <= 10) return 'stock-low';
    return 'stock-normal';
  }

  isLowStock(stock: number): boolean { return stock > 0 && stock <= 10; }
  isCriticalStock(stock: number): boolean { return stock > 0 && stock <= 5; }
  isOutOfStock(stock: number): boolean { return stock === 0; }

  getAddButtonTooltip(): string {
    return this.articles.length === 0 ? this.translate.instant('STOCK.ERRORS.ARTICLES_NOT_FOUND') : '';
  }

  /** Returns the currently selected article. Uses a cached reference set on articleId change. */
  getSelectedArticle(): StockItem | null {
    return this._selectedArticle;
  }

  getAvailableStock(articleId: string): number {
    return this.articles.find(a => a.id === articleId)?.quantity || 0;
  }

  private updateQuantityValidator(maxStock: number): void {
    const qtyControl = this.itemForm.get('quantity');
    if (!qtyControl) return;
    const validators = [Validators.required, Validators.min(1)];
    if (maxStock > 0) validators.push(Validators.max(maxStock));
    qtyControl.setValidators(validators);
    qtyControl.updateValueAndValidity();
  }

  // Used for checkArticleStock calls from openInlineItemEdit
  checkArticleStock(articleId: string, _requestedQuantity: number): void {
    const article = this.articles.find(a => a.id === articleId);
    this.updateQuantityValidator(article?.quantity ?? 0);
  }

  getQuantityStep(unit?: string): number {
    if (!unit) return 1;
    const integerUnits = [UnitEnum.Piece, UnitEnum.Hour, UnitEnum.Day];
    return integerUnits.includes(unit as UnitEnum) ? 1 : 0.1;
  }

  getQuantityMin(unit?: string): number {
    if (!unit) return 1;
    const integerUnits = [UnitEnum.Piece, UnitEnum.Hour, UnitEnum.Day];
    return integerUnits.includes(unit as UnitEnum) ? 1 : 0.001;
  }

  getQuantityMax(): number {
    return this._selectedArticle?.quantity ?? Infinity;
  }

  getUnitTranslation(): string {
    const unit = this._selectedArticle?.unit;
    if (!unit) return '';
    return this.translate.instant(`ARTICLES.UNIT.${unit.toUpperCase()}`);
  }


  private syncArticles(): void {
    const consumed = new Map<string, number>();
    for (const item of this.pendingItems) {
      if (item._localId === this.inlineItemLocalId) continue;
      consumed.set(item.articleId, (consumed.get(item.articleId) ?? 0) + item.quantity);
    }

    const editingId = this._selectedArticle?.id ?? null;

    this.articles = this.masterArticles
      .map(master => ({
        ...master,
        quantity: master.quantity - (consumed.get(master.id) ?? 0),
      }))
      .filter(a => a.quantity > 0 || a.id === editingId);

    this.cdr.markForCheck();
    this.loadCreditLimitInfo();
  }

  getCreditLimitMessage(): string {
    // No credit limit set
    if (!this.selectedClientForValidation?.creditLimit) {
      return this.translate.instant('INVOICES.FORM.NO_LIMIT');
    }
    
    // Has sufficient credit
    if (this.creditLimitInfo.hasSufficientCredit) {
      return this.translate.instant('INVOICES.ERRORS.HAS_SUFFICIENT_CREDIT', {
        remainingCredit: this.creditLimitInfo.remainingCredit.toFixed(2)
      });
    }
    
    // Insufficient credit
    return this.translate.instant('INVOICES.ERRORS.INSUFFICIENT_CREDIT', {
      creditLimit: this.selectedClientForValidation.creditLimit.toFixed(2),
      currentOutstanding: this.creditLimitInfo.currentUsage.toFixed(2),
      invoiceTotal: this.invoiceTotalTTC().toFixed(2)
    });
  }

  getCreditLimitClass(): string {
    if (!this.selectedClientForValidation?.creditLimit) return 'text-muted';
    if (this.creditLimitInfo.hasSufficientCredit) return 'text-success';
    return 'text-danger';
  }
}