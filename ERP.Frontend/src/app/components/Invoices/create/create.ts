import { CommonModule, Location, ViewportScroller } from '@angular/common';
import { ChangeDetectorRef, Component, computed, DestroyRef, inject, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ClientResponseDto, ClientsService } from '../../../services/clients/clients.service';
import { StockItem, StockService } from '../../../services/stock.service';
import { catchError, firstValueFrom, forkJoin, map, Observable, of } from 'rxjs';
import { AuthService } from '../../../services/auth/auth.service';
import { CreateInvoiceDto, InvoiceService, TaxCalculationMode } from '../../../services/invoice.service';
import { ArticleService, UnitEnum } from '../../../services/articles/articles.service';
import { HttpError } from '../../../interfaces/ErrorDto';
import { Router } from '@angular/router';

interface PendingItem {
  _localId: string;
  articleId: string;
  articleName: string;
  articleBarCode: string;
  quantity: number;
  uniPriceHT: number;       // original price before discount
  effectivePriceHT: number; // price after discount applied
  taxRate: number;
  totalHT: number;
  totalTTC: number;
  taxAmount: number;        // isolated tax amount for perInvoice mode
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


@Component({
  selector: 'app-invoices-create',
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatDialogModule,
    TranslatePipe
  ],
  templateUrl: './create.html',
  styleUrl: './create.scss',
})
export class CreateInvoiceComponent implements OnInit, OnDestroy{
  private themeObserver: MutationObserver | null = null;
  private readonly destroyRef = inject(DestroyRef);
  private translate = inject(TranslateService);
  private cdr = inject(ChangeDetectorRef);
  private location= inject(Location);

  // Forms
  invoiceForm!: FormGroup;
  itemForm!: FormGroup;
  selectedClientForValidation: ClientResponseDto | null = null;  
  private _selectedArticle: StockItem | null = null;
  private masterArticles: StockItem[] = [];
  articles: StockItem[] = [];
  clients: ClientResponseDto[] = [];
  filteredClients: ClientResponseDto[] = [];
  clientSearchQuery = '';
  pendingItems: PendingItem[] = [];
  
  isValidating= false;
  taxCalculationMode: TaxCalculationMode= TaxCalculationMode.LINE;
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
    discountAmountHT: 0,
    originalTotal: 0,
    discountedTotal: 0
  };
  
  inlineItemLocalId = '';
  inlineItemOpen = false;

  // ── Alerts ────────────────────────────────────────────────────────────────
  errors: string[] = [];
  successMessage: string | null = null

  readonly TAXMODES= TaxCalculationMode;

  constructor(
    public authService: AuthService,
    private invoiceService: InvoiceService,
    private clientsService: ClientsService,
    private articleService: ArticleService,
    private fb: FormBuilder,
    private dialog: MatDialog,
    private stock: StockService,
    private router: Router,
    private viewportScroller: ViewportScroller
  ) {}
  
  ngOnInit(): void {
    this.buildForms();
    this.reload();
  }

  private buildForms(): void {
    this.invoiceForm = this.fb.group({
      invoiceDate:     ['', Validators.required],
      dueDate:         ['', Validators.required],
      clientId:        ['', Validators.required],
      clientFullName:  ['', Validators.required],
      clientAddress:   ['', Validators.required],
      additionalNotes: [null],
      taxModeInvoice:  [false], // false = perLine, true = perInvoice
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

  // service load calls
  loadClients(): Observable<ClientResponseDto[]> {
    return this.clientsService.getAll(1, 1000).pipe(
      map(res => {
        this.clients = res.items;
        return this.clients;
      }),
      catchError(() => {
        this.clients = [];
        return of([]);
      })
    );
  }

  loadArticlesWithStock(): Observable<StockItem[]> {
    return forkJoin({
      articles: this.articleService.getAll(1, 1000).pipe(catchError(() => of({ items: [] }))),
      stock: this.stock.getStockArticles().pipe(catchError(() => of({ inStock: [], outStock: [] })))
    }).pipe(
      map((results) => {
        const allArticles = results.articles.items || [];
        const stockData = results.stock || { inStock: [], outStock: [] };

        const stockMap = new Map<string, number>();
        stockData.inStock.forEach((s: any) => {
          stockMap.set(s.articleId || s.id, s.quantity);
        });

        this.masterArticles = allArticles
          .filter((a: any) => stockMap.has(a.id) && stockMap.get(a.id)! > 0)
          .map((a: any) => ({ ...a, quantity: stockMap.get(a.id)! }));

        this.syncArticles(); // updates this.articles and calls markForCheck
        return this.articles;
      }),
      catchError(() => {
        this.syncArticles();
        this.articles = [];
        return of([]);
      })
    );
  }

  reload(): void {
    forkJoin({
      clients: this.loadClients(),
      articles: this.loadArticlesWithStock()
    }).subscribe({
      next: () => {
      },
      error: () => {
        this.cdr.markForCheck();
      }
    });
  }


  invoiceTotalTTC = computed(() => {
    if (this.discountInfo.applies && this.discountInfo.discountedTotal > 0) {
      return this.discountInfo.discountedTotal;
    }
    return this.pendingTotalTTC;
  });

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

  // Form helpers
  getSelectedArticle(): StockItem | null {
    return this._selectedArticle;
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
  
  getAvailableStock(articleId: string): number {
    return this.articles.find(a => a.id === articleId)?.quantity || 0;
  }
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
    this.isValidating = false; // ← add this
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

  const discountRate = this.discountInfo.applies ? this.discountInfo.rate : 0;

  const { effectivePriceHT, totalHT, taxAmount, totalTTC } =
    this.calcLineAmounts(quantity, uniPriceHT, taxRate, discountRate);

  if (this.inlineItemLocalId) {
    const idx = this.pendingItems.findIndex(i => i._localId === this.inlineItemLocalId);
    if (idx !== -1) {
      this.pendingItems[idx] = {
        ...this.pendingItems[idx],
        articleId, articleName: master.libelle ?? '',
        articleBarCode: master.barCode ?? '',
        quantity, uniPriceHT, effectivePriceHT,
        taxRate, totalHT, taxAmount, totalTTC,
      };
    }
  } else {
    const existingIndex = this.pendingItems.findIndex(i => i.articleId === articleId);
    if (existingIndex !== -1) {
      const existing = this.pendingItems[existingIndex];
      const newQuantity = existing.quantity + quantity;
      if (newQuantity > maxAllowed) { /* existing error flash */ return; }

      const merged = this.calcLineAmounts(newQuantity, existing.uniPriceHT, existing.taxRate, discountRate);
      this.pendingItems[existingIndex] = {
        ...existing, quantity: newQuantity,
        effectivePriceHT: merged.effectivePriceHT,
        totalHT: merged.totalHT, taxAmount: merged.taxAmount, totalTTC: merged.totalTTC,
      };
    } else {
      this.pendingItems.push({
        _localId: crypto.randomUUID(),
        articleId, articleName: master.libelle ?? '',
        articleBarCode: master.barCode ?? '',
        quantity, uniPriceHT, effectivePriceHT,
        taxRate, totalHT, taxAmount, totalTTC,
      });
    }
  }

    this.pendingItems = [...this.pendingItems];
    this.invoiceForm.markAsDirty();
    this.closeInlineItem();
    this.syncArticles();
    this.checkClientLimitsAndDiscount();
  }

  private calcLineAmounts(
    qty: number,
    uniPriceHT: number,
    taxRate: number,          // as percentage e.g. 19
    discountRate = 0          // as percentage e.g. 10
  ): { effectivePriceHT: number; totalHT: number; taxAmount: number; totalTTC: number } {
    const effectivePriceHT = uniPriceHT * (1 - discountRate / 100);
    const totalHT  = qty * effectivePriceHT;
    const taxAmount = totalHT * (taxRate / 100);
    const totalTTC  = totalHT + taxAmount;
    return { effectivePriceHT, totalHT, taxAmount, totalTTC };
  }

  // In the component — replace checkClientLimitsAndDiscount():
  async checkClientLimitsAndDiscount(): Promise<void> {
    if (!this.selectedClientForValidation || this.pendingItems.length === 0) {
      this.creditWarning = null;
      this.discountInfo = { 
        applies: false, rate: 0, discountAmount: 0, 
        discountAmountHT: 0, originalTotal: 0, discountedTotal: 0 
      };
      return;
    }

    const { discountRate, applies } = this.invoiceService.calculateBulkDiscount(
      this.selectedClientForValidation
    );

    // ── Derive everything from pendingItems (original prices) ──
    const originalTotalHT  = this.pendingItems.reduce((s, i) => s + i.quantity * i.uniPriceHT, 0);
    const originalTotalTTC = this.pendingItems.reduce(
      (s, i) => s + i.quantity * i.uniPriceHT * (1 + i.taxRate / 100), 0
    );

    const discountMultiplier = 1 - discountRate / 100;
    const discountedTotalHT  = originalTotalHT * discountMultiplier;
    const discountedTotalTTC = originalTotalTTC * discountMultiplier;

    this.discountInfo = {
      applies,
      rate: discountRate,
      discountAmountHT: originalTotalHT - discountedTotalHT,
      discountAmount:   originalTotalTTC - discountedTotalTTC,  // TTC savings
      originalTotal:    originalTotalTTC,
      discountedTotal:  discountedTotalTTC,
    };

    // ── Recalc pending items with new discount rate ──
    this.recalcAllItemPrices();

    try {
      const outstanding = await firstValueFrom(
        this.invoiceService.getClientOutstandingBalance(this.selectedClientForValidation.id)
      );
      const creditCheck = this.invoiceService.validateCreditLimit(
        this.selectedClientForValidation,
        this.discountInfo.applies ? discountedTotalTTC : originalTotalTTC,
        outstanding || 0
      );
      this.creditWarning = creditCheck.hasSufficientCredit ? null : creditCheck.message;
    } catch {
      // silently ignore
    }
  }

  get duePaymentPeriodHint(): string {
    if (!this.selectedClientForValidation?.duePaymentPeriod) return '';
    return this.translate.instant('INVOICES.FORM.DUE_DATE_HINT', {
      days: this.selectedClientForValidation.duePaymentPeriod
    });
  }

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

  private updateQuantityValidator(maxStock: number): void {
    const qtyControl = this.itemForm.get('quantity');
    if (!qtyControl) return;
    const validators = [Validators.required, Validators.min(1)];
    if (maxStock > 0) validators.push(Validators.max(maxStock));
    qtyControl.setValidators(validators);
    qtyControl.updateValueAndValidity();
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

    const formValue = this.invoiceForm.value;
    const selectedClient = this.clients.find(c => c.id === formValue.clientId);
    if (!selectedClient) {
      this.flash('error', this.translate.instant('INVOICES.ERRORS.CLIENT_NOT_FOUND'));
      return;
    }

    // ── UI-level guards only ──────────────────────────────────────────────
    if (selectedClient.isBlocked) {
      this.flash('error', this.translate.instant('INVOICES.ERRORS.CLIENT_BLOCKED'));
      return;
    }
    if (selectedClient.isDeleted) {
      this.flash('error', this.translate.instant('INVOICES.ERRORS.CLIENT_DELETED'));
      return;
    }

    const stockValid = await this.validateAllItemsStock();
    if (!stockValid) return;

    this.isValidating = true;

    const dto: CreateInvoiceDto = {
      invoiceDate: formValue.invoiceDate,
      dueDate: formValue.dueDate,
      clientId: formValue.clientId,
      additionalNotes: formValue.additionalNotes,
      taxMode: this.taxCalculationMode,
      items: this.pendingItems.map(item => ({
        articleId:  item.articleId,
        quantity:   Number(item.quantity),
        uniPriceHT: Number(item.uniPriceHT),  // ← original price, backend applies discount
        taxRate:    Number(item.taxRate / 100),
      })),
    };
    this.invoiceService.create(dto).subscribe({
      next: () => {
        this.flash('success', this.translate.instant('INVOICES.SUCCESS.CREATED'));
        setTimeout(() => {
          document.getElementById('top')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }, 0);
        setTimeout(() => {
          this.cancel();
          this.isValidating = false;
        }, 2000);
      },
      error: (err) => {
        const errorMsg = (err.error as HttpError)?.message 
          || this.translate.instant('INVOICES.ERRORS.CREATE_FAILED');
        this.flash('error', errorMsg);
        this.isValidating = false;
      },
    });
  }
    // Toggle handler (add to component):
  setTaxCalculationMode(mode: TaxCalculationMode): void {
    this.taxCalculationMode = mode;
    // No need to recalc items — pendingTotalTTC getter reacts instantly
    this.checkClientLimitsAndDiscount();
  }

  private recalcAllItemPrices(): void {
    const discountRate = this.discountInfo.applies ? this.discountInfo.rate : 0;
    this.pendingItems = this.pendingItems.map(item => {
      const { effectivePriceHT, totalHT, taxAmount, totalTTC } = this.calcLineAmounts(
        item.quantity, item.uniPriceHT, item.taxRate, discountRate
      );
      return { ...item, effectivePriceHT, totalHT, taxAmount, totalTTC };
    });
    this.syncArticles();
  }

  get pendingTotalHT(): number {
    // always sum effectivePriceHT (post-discount) × qty
    return this.pendingItems.reduce((s, i) => s + i.totalHT, 0);
  }

  get pendingTotalTVA(): number {
    if (this.taxCalculationMode === TaxCalculationMode.LINE) {
      // sum each line's tax individually (may differ due to rounding)
      return this.pendingItems.reduce((s, i) => s + i.taxAmount, 0);
    }
    // INVOICE mode: weighted average rate on total HT
    const totalHT = this.pendingTotalHT;
    if (totalHT === 0) return 0;
    const weightedRate = this.pendingItems.reduce(
      (s, i) => s + i.totalHT * (i.taxRate / 100), 0
    ) / totalHT;
    return Math.round(totalHT * weightedRate * 100) / 100;
  }

  get pendingTotalTTC(): number {
    return this.pendingTotalHT + this.pendingTotalTVA;
  }

  trackById(_: number, item: { id: string }) { return item.id; }
  trackByLocalId(_: number, item: PendingItem) { return item._localId; }

  // Template
  get pageTitle():string{
    return 'INVOICES.TITLE_NEW';
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
  dismissError(): void { this.errors = []; }
  cancel(){
    this.location.back();
  }
  ngOnDestroy(): void {
    this.themeObserver?.disconnect();
  }
}
