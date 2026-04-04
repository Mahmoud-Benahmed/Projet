import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AuthService, PRIVILEGES } from '../../services/auth/auth.service';
import { InvoiceService, InvoiceDto, CreateInvoiceDto } from '../../services/invoices/invoice.service';
import { ClientsService, ClientResponseDto } from '../../services/clients/clients.service';
import { ArticleService, ArticleResponseDto } from '../../services/articles/articles.service';
import { PaginationComponent } from '../pagination/pagination';
import { ModalComponent } from '../modal/modal';

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
  ],
  templateUrl: './invoices.html',
  styleUrl: './invoices.scss',
})
export class InvoicesComponent implements OnInit {

  readonly PRIVILEGES = PRIVILEGES;

  viewMode = signal<'list' | 'list-deleted' | 'create' | 'view'>('list');
  isList()        { return this.viewMode() === 'list'; }
  isDeletedList() { return this.viewMode() === 'list-deleted'; }
  isCreate()      { return this.viewMode() === 'create'; }
  isView()        { return this.viewMode() === 'view'; }

  invoices: InvoiceDto[]        = [];
  deletedInvoices: InvoiceDto[] = [];
  selectedInvoice: InvoiceDto | null = null;

  totalCount  = 0;
  currentPage = 1;
  currentSize = 10;
  readonly pageSizeOptions = [5, 10, 25, 50];
  get totalPages(): number { return Math.ceil(this.totalCount / this.currentSize) || 1; }

  stats = { total: 0, draft: 0, unpaid: 0, paid: 0, cancelled: 0, deleted: 0 };

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

  errors: string[] = [];
  successMessage   = '';

  invoiceForm!: FormGroup;
  itemForm!: FormGroup;

  clients: ClientResponseDto[]         = [];
  filteredClients: ClientResponseDto[] = [];
  clientSearchQuery                    = '';

  articles: ArticleResponseDto[] = [];

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

  ngOnInit(): void {
    this.buildForms();
    this.loadInvoices();
    this.loadClients();
    this.loadArticles();
  }

  private buildForms(): void {
    this.invoiceForm = this.fb.group({
      invoiceNumber:   ['', Validators.required],
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
      taxRate:    [0.19],
    });
  }

  loadInvoices(): void {
    if (this.statusFilter === 'ALL') {
      this.invoiceService.getAll(this.currentPage, this.currentSize).subscribe({
        next: (res: { items: InvoiceDto[]; totalCount: number }) => {
          this.invoices   = res.items;
          this.totalCount = res.totalCount;
          this.refreshStats();
        },
        error: () => this.addError('Failed to load invoices'),
      });
    } else {
      this.invoiceService.getByStatus(this.statusFilter, this.currentPage, this.currentSize).subscribe({
        next: (res: { items: InvoiceDto[]; totalCount: number }) => {
          this.invoices   = res.items;
          this.totalCount = res.totalCount;
        },
        error: () => this.addError('Failed to load invoices'),
      });
    }
  }

  loadDeletedInvoices(): void {
    this.invoiceService.getDeleted(this.currentPage, this.currentSize).subscribe({
      next: (res: { items: InvoiceDto[]; totalCount: number }) => {
        this.deletedInvoices = res.items;
        this.totalCount      = res.totalCount;
      },
      error: () => this.addError('Failed to load deleted invoices'),
    });
  }

  loadClients(): void {
    this.clientsService.getAll(1, 1000).subscribe({
      next: (res: { items: ClientResponseDto[]; totalCount: number }) => { this.clients = res.items; },
      error: () => {},
    });
  }

  loadArticles(): void {
    this.articleService.getAll(1, 1000).subscribe({
      next: (res: { items: ArticleResponseDto[]; totalCount: number }) => { this.articles = res.items; },
      error: () => {},
    });
  }

  private refreshStats(): void {
    this.stats.total = this.totalCount;
    (['DRAFT', 'UNPAID', 'PAID', 'CANCELLED'] as const).forEach(status => {
      this.invoiceService.getByStatus(status, 1, 1).subscribe({
        next: (res: { totalCount: number }) => {
          if (status === 'DRAFT')     this.stats.draft     = res.totalCount;
          if (status === 'UNPAID')    this.stats.unpaid    = res.totalCount;
          if (status === 'PAID')      this.stats.paid      = res.totalCount;
          if (status === 'CANCELLED') this.stats.cancelled = res.totalCount;
        },
      });
    });
    this.invoiceService.getDeleted(1, 1).subscribe({
      next: (res: { totalCount: number }) => this.stats.deleted = res.totalCount,
    });
  }

  openCreate(): void {
    this.invoiceForm.reset();
    this.pendingItems      = [];
    this.inlineItemOpen    = false;
    this.inlineItemLocalId = '';
    this.clientSearchQuery = '';
    this.filteredClients   = [];
    this.viewMode.set('create');
  }

  openView(invoice: InvoiceDto): void {
    this.invoiceService.getById(invoice.id).subscribe({
      next: (full: InvoiceDto) => { this.selectedInvoice = full; this.viewMode.set('view'); },
      error: () => this.addError('Failed to load invoice details'),
    });
  }

  cancel(): void {
    this.viewMode.set('list');
    this.selectedInvoice = null;
  }

  setStatusFilter(status: string): void {
    this.statusFilter = status;
    this.currentPage  = 1;
    this.loadInvoices();
  }

  applyFilter(): void {}

  sortBy(col: string): void {
    this.sortDirection = this.sortColumn === col && this.sortDirection === 'asc' ? 'desc' : 'asc';
    this.sortColumn = col;
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.isDeletedList() ? this.loadDeletedInvoices() : this.loadInvoices();
  }

  onPageSizeChange(size: number): void {
    this.currentSize = size;
    this.currentPage = 1;
    this.isDeletedList() ? this.loadDeletedInvoices() : this.loadInvoices();
  }

  filterClients(query: string): void {
    if (!query) { this.filteredClients = []; return; }
    const q = query.toLowerCase();
    this.filteredClients = this.clients.filter(c =>
      c.name?.toLowerCase().includes(q) || c.email?.toLowerCase().includes(q)
    ).slice(0, 8);
  }

  selectClient(client: ClientResponseDto): void {
    this.invoiceForm.patchValue({
      clientId:       client.id,
      clientFullName: client.name,
      clientAddress:  client.address ?? '',
    });
    this.clientSearchQuery = client.name;
    this.filteredClients   = [];
  }

  onArticleSelectChange(event: Event): void {
    const id = (event.target as HTMLSelectElement).value;
    const article = this.articles.find(a => a.id === id);
    this.onArticleSelected(article);
  }

  onArticleSelected(article: ArticleResponseDto | undefined): void {
    if (!article) return;
    this.itemForm.patchValue({ uniPriceHT: article.prix ?? 0, taxRate: article.tva ?? 0.19 });
  }

  openInlineItemAdd(): void {
    this.itemForm.reset({ quantity: 1, uniPriceHT: 0, taxRate: 0.19 });
    this.inlineItemLocalId = '';
    this.inlineItemOpen    = true;
  }

  openInlineItemEdit(item: PendingItem): void {
    this.itemForm.patchValue({
      articleId: item.articleId, quantity: item.quantity,
      uniPriceHT: item.uniPriceHT, taxRate: item.taxRate,
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
    if (this.itemForm.invalid) return;
    const { articleId, quantity, uniPriceHT, taxRate } = this.itemForm.value;
    const article  = this.articles.find(a => a.id === articleId);
    const totalHT  = quantity * uniPriceHT;
    const totalTTC = totalHT * (1 + taxRate);

    if (this.inlineItemLocalId) {
      const idx = this.pendingItems.findIndex(i => i._localId === this.inlineItemLocalId);
      if (idx !== -1) {
        this.pendingItems[idx] = {
          ...this.pendingItems[idx], articleId,
          articleName: article?.libelle ?? '', articleBarCode: article?.barCode ?? '',
          quantity, uniPriceHT, taxRate, totalHT, totalTTC,
        };
      }
    } else {
      this.pendingItems.push({
        _localId: crypto.randomUUID(), articleId,
        articleName: article?.libelle ?? '', articleBarCode: article?.barCode ?? '',
        quantity, uniPriceHT, taxRate, totalHT, totalTTC,
      });
    }
    this.closeInlineItem();
  }

  removePendingItem(localId: string): void {
    this.pendingItems = this.pendingItems.filter(i => i._localId !== localId);
  }

  submit(): void {
    if (this.invoiceForm.invalid || this.pendingItems.length === 0) return;
    const dto: CreateInvoiceDto = {
      ...this.invoiceForm.value,
      items: this.pendingItems.map(({ _localId, totalHT, totalTTC, ...rest }) => rest),
    };
    this.invoiceService.create(dto).subscribe({
      next: () => { this.showSuccess('Invoice created successfully'); this.loadInvoices(); this.viewMode.set('list'); },
      error: () => this.addError('Failed to create invoice'),
    });
  }

  finalize(invoice: InvoiceDto): void {
    this.invoiceService.finalize(invoice.id).subscribe({
      next: (u: InvoiceDto) => { this.selectedInvoice = u; this.showSuccess('Invoice finalized'); this.loadInvoices(); },
      error: () => this.addError('Failed to finalize invoice'),
    });
  }

  markAsPaid(invoice: InvoiceDto): void {
    this.invoiceService.markAsPaid(invoice.id).subscribe({
      next: (u: InvoiceDto) => { this.selectedInvoice = u; this.showSuccess('Invoice marked as paid'); this.loadInvoices(); },
      error: () => this.addError('Failed to mark invoice as paid'),
    });
  }

  cancelInvoice(invoice: InvoiceDto): void {
    const ref = this.dialog.open(ModalComponent, {
      width: '420px',
      data: {
        icon:        'cancel',
        iconColor:   'warn',
        title:       'Cancel Invoice',
        message:     `Invoice "${invoice.invoiceNumber}" will be cancelled. This action cannot be undone. Do you want to proceed?`,
        confirmText: 'Cancel Invoice',
        cancelText:  'Go Back',
      },
    });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.invoiceService.cancel(invoice.id).subscribe({
        next: (u: InvoiceDto) => { this.selectedInvoice = u; this.showSuccess('Invoice cancelled'); this.loadInvoices(); },
        error: () => this.addError('Failed to cancel invoice'),
      });
    });
  }

  delete(invoice: InvoiceDto): void {
    const ref = this.dialog.open(ModalComponent, {
      width: '420px',
      data: {
        icon:        'delete',
        iconColor:   'warn',
        title:       'Delete Invoice',
        message:     `Invoice "${invoice.invoiceNumber}" will be soft-deleted. Do you want to proceed?`,
        confirmText: 'Delete',
        cancelText:  'Annuler',
      },
    });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.invoiceService.delete(invoice.id).subscribe({
        next: () => {
          this.showSuccess('Invoice deleted');
          this.selectedInvoice = null;
          this.viewMode.set('list');
          this.loadInvoices();
        },
        error: () => this.addError('Failed to delete invoice'),
      });
    });
  }

  restore(invoice: InvoiceDto): void {
    this.invoiceService.restore(invoice.id).subscribe({
      next: () => { this.showSuccess('Invoice restored'); this.loadDeletedInvoices(); },
      error: () => this.addError('Failed to restore invoice'),
    });
  }

  /** Maps invoice status to the SCSS badge colour classes. */
  statusClass(status: string): Record<string, boolean> {
    return {
      'badge--amber': status === 'DRAFT',
      'badge--red':   status === 'UNPAID',
      'badge--green': status === 'PAID',
      'badge--grey':  status === 'CANCELLED',
    };
  }

  /** Sum of totalHT across all pending line items. */
  get pendingTotalHT(): number {
    return this.pendingItems.reduce((sum, i) => sum + i.totalHT, 0);
  }

  /** Sum of totalTTC across all pending line items. */
  get pendingTotalTTC(): number {
    return this.pendingItems.reduce((sum, i) => sum + i.totalTTC, 0);
  }

  trackById(_: number, item: { id: string }) { return item.id; }
  trackByLocalId(_: number, item: PendingItem) { return item._localId; }

  private addError(msg: string): void { this.errors = [msg]; setTimeout(() => this.errors = [], 5000); }
  dismissError(): void { this.errors = []; }
  private showSuccess(msg: string): void { this.successMessage = msg; setTimeout(() => this.successMessage = '', 3000); }
}