import {
  Component, OnInit, ChangeDetectorRef, ViewEncapsulation,
  DestroyRef, inject, signal, computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PRIVILEGES, AuthService } from '../../../services/auth/auth.service';
import {
  StockService,
  BonEntreResponse, CreateBonEntreRequest, UpdateBonEntreRequest,
  BonSortieResponse, CreateBonSortieRequest, UpdateBonSortieRequest,
  BonRetourResponse, CreateBonRetourRequest, UpdateBonRetourRequest,
  LigneResponseDto, LigneRequestDto, RetourSourceType, BonStatsDto,
  PagedResult,
  FournisseurResponse,
} from '../../../services/stock.service';

/** A single record that can be any of the three bon response shapes. */
export type BonRecord = BonEntreResponse | BonSortieResponse | BonRetourResponse;
import { MatTableDataSource } from '@angular/material/table';
import { HttpError } from '../../../interfaces/ErrorDto';
import { MatDialog } from '@angular/material/dialog';
import { ModalComponent } from '../../modal/modal';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PaginationComponent } from '../../pagination/pagination';
import { Observable, switchMap, EMPTY, forkJoin, map, catchError, of } from 'rxjs';
import { RouterLink } from "@angular/router";
import { ClientResponseDto, ClientsService } from '../../../services/clients/clients.service';
import { ArticleResponseDto, ArticleService } from '../../../services/articles/articles.service';

export type BonType = 'entre' | 'sortie' | 'retour';

type BonApi = {
  list:         (p: number, s: number) => Observable<PagedResult<BonRecord>>;
  listDeleted:  (p: number, s: number) => Observable<PagedResult<BonRecord>>;
  stats:        () => Observable<BonStatsDto>;
  delete:       (id: string) => Observable<any>;
};

type ViewMode = 'list' | 'list-deleted' | 'create' | 'edit' | 'view';

/** A ligne that exists only in memory during create mode (no id yet). */
export interface PendingLigne {
  _localId:  string;        // UUID-like local key for trackBy
  articleId: string;
  articleLabel: string;     // display only
  quantity:  number;
  price:     number;
  remarque:  string | null;
  total:     number;
}

@Component({
  selector: 'app-bons',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    MatIconModule, MatButtonModule, MatTooltipModule, MatProgressSpinnerModule,
    PaginationComponent,
    RouterLink
  ],
  templateUrl: './bon.html',
  styleUrl: './bon.scss',
  encapsulation: ViewEncapsulation.None,
})
export class BonsComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  readonly PRIVILEGES   = PRIVILEGES;
  readonly RetourSource = RetourSourceType;

  // ── Bon type tab ───────────────────────────────────────────────────────────
  activeBonType: BonType = 'entre';

  // ── Table ──────────────────────────────────────────────────────────────────
  dataSource = new MatTableDataSource<BonRecord>([]);
  pageNumber = signal(1);
  pageSize   = signal(10);
  pageSizeOptions = [5, 10, 25, 50];
  totalCount = 0;

  sortColumn    = '';
  sortDirection: 'asc' | 'desc' = 'asc';
  searchQuery   = '';

  // ── Stats ──────────────────────────────────────────────────────────────────
  stats: BonStatsDto | null = null;

  // ── View mode ──────────────────────────────────────────────────────────────
  viewMode = signal<ViewMode>('list');

  isList        = computed(() => this.viewMode() === 'list');
  isDeletedList = computed(() => this.viewMode() === 'list-deleted');
  isCreate      = computed(() => this.viewMode() === 'create');
  isEdit        = computed(() => this.viewMode() === 'edit');
  isView        = computed(() => this.viewMode() === 'view');

  private previousMode: ViewMode = 'list';

  // ── Alerts ─────────────────────────────────────────────────────────────────
  errors: string[] = [];
  successMessage: string | null = null;

  // ── Selection ──────────────────────────────────────────────────────────────
  selectedBon: BonRecord | null = null;

  // ── Source bon list (for retour form select) ──────────────────────────────
  allSourceBons: { id: string; numero: string; sourceType: RetourSourceType }[] = [];

  // ── Date filter ────────────────────────────────────────────────────────────
  dateFrom: string | null = null;
  dateTo: string | null = null;

  // ── Forms ──────────────────────────────────────────────────────────────────
  headerForm: FormGroup;
  ligneForm:  FormGroup;

  // ── Inline ligne editing inside form panel ─────────────────────────────────
  /** Lignes pending save during CREATE mode (no bon id yet). */
  pendingLignes: PendingLigne[] = [];
  /** Which pending ligne (by _localId) or saved ligne (by id) is being inline-edited. */
  inlineLigneLocalId: string | null = null;
  /** Whether the inline ligne form is open inside the form panel. */
  inlineLigneOpen = false;
  isInlineLigneSubmitting = false;

  // ── Ligne modal (kept for VIEW panel only) ─────────────────────────────────
  editingLigneId: string | null = null;
  isLigneSubmitting = false;

  articles: ArticleResponseDto[] = [];
  articleSearchQuery = '';
  filteredArticles: ArticleResponseDto[] = [];

  fournisseurs: FournisseurResponse[] = [];
  filteredFournisseurs: FournisseurResponse[] = [];
  fournisseurSearchQuery = '';

  clients: ClientResponseDto[] = [];
  filteredClients: ClientResponseDto[] = [];
  clientSearchQuery = '';

  constructor(
    private stock: StockService,
    public authService: AuthService,
    public clientService: ClientsService,
    private articleService: ArticleService,
    private fb: FormBuilder,
    private cdr: ChangeDetectorRef,
    private dialog: MatDialog
  ) {
    this.headerForm = this.buildHeaderForm();
    this.ligneForm  = this.buildLigneForm();
  }

  ngOnInit(): void {
    this.dataSource.filterPredicate = (data, filter) =>
      this.flattenObject(data).includes(filter);
    this.reload();
  }

  // ── Form builders ──────────────────────────────────────────────────────────
  private buildHeaderForm(): FormGroup {
    return this.fb.group({
      numero:        ['', Validators.required],
      observation:   [''],
      fournisseurId: [''],
      clientId:      [''],
      sourceId:      [''],
      sourceType:    [RetourSourceType.BonEntre],
      motif:         [''],
    });
  }

  private buildLigneForm(): FormGroup {
    return this.fb.group({
      articleId: ['', Validators.required],
      quantity:  [1,  [Validators.required, Validators.min(0.001)]],
      price:     [0,  [Validators.required, Validators.min(0)]],
      remarque:  [''],
    });
  }

  private applyTypeValidators(): void {
    const f = this.headerForm;
    ['fournisseurId', 'clientId', 'sourceId', 'motif'].forEach(ctrl =>
      f.get(ctrl)?.clearValidators()
    );

    if (this.activeBonType === 'entre') {
      f.get('fournisseurId')?.setValidators(Validators.required);
    } else if (this.activeBonType === 'sortie') {
      f.get('clientId')?.setValidators(Validators.required);
    } else {
      f.get('sourceId')?.setValidators(Validators.required);
      f.get('motif')?.setValidators(Validators.required);
    }

    ['fournisseurId', 'clientId', 'sourceId', 'motif'].forEach(ctrl =>
      f.get(ctrl)?.updateValueAndValidity()
    );
  }

  // ── Sorting / filtering ────────────────────────────────────────────────────
  sortBy(column: string): void {
    this.sortDirection = this.sortColumn === column && this.sortDirection === 'asc' ? 'desc' : 'asc';
    this.sortColumn = column;
  }

  get sortedData(): BonRecord[] {
    const data = [...this.dataSource.filteredData];
    if (!this.sortColumn) return data;
    return data.sort((a, b) => {
      let va = this.getNestedValue(a, this.sortColumn);
      let vb = this.getNestedValue(b, this.sortColumn);
      if (va == null) return 1;
      if (vb == null) return -1;
      if (typeof va === 'string') va = va.toLowerCase();
      if (typeof vb === 'string') vb = vb.toLowerCase();
      return (va < vb ? -1 : va > vb ? 1 : 0) * (this.sortDirection === 'asc' ? 1 : -1);
    });
  }

  applyFilter(): void {
    this.dataSource.filter = this.searchQuery.trim().toLowerCase();
  }

  applyDateFilter(): void {
    if (!this.dateFrom || !this.dateTo) return;

    if (this.dateFrom > this.dateTo) {
      [this.dateFrom, this.dateTo] = [this.dateTo, this.dateFrom];
    }
    const from = new Date(this.dateFrom);
    const to = new Date(this.dateTo);

    let request$: Observable<PagedResult<BonRecord>>;

    switch (this.activeBonType) {
      case 'entre':
        request$ = this.stock.getBonEntresByDateRange(from, to, this.pageNumber(), this.pageSize());
        break;
      case 'sortie':
        request$ = this.stock.getBonSortiesByDateRange(from, to,this.pageNumber(), this.pageSize());
        break;
      case 'retour':
        request$ = this.stock.getBonRetoursByDateRange(from, to, this.pageNumber(), this.pageSize());
        break;
    }

    request$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.dataSource.data = res.items;
          this.totalCount = res.totalCount;
          this.pageNumber.set(1);
          this.cdr.markForCheck();
        },
        error: () => this.flash('error', 'Date filter failed')
      });
  }

  clearDateFilter(): void { this.dateFrom = ''; this.dateTo = ''; this.pageNumber.set(1); this.load(); }

  // ── Type switching ─────────────────────────────────────────────────────────
  switchBonType(type: BonType): void {
    this.activeBonType = type;
    this.pageNumber.set(1);
    this.dateFrom    = '';
    this.dateTo      = '';
    this.searchQuery = '';
    this.setViewMode('list');
    this.reload();
  }

  // ── Card clicks ────────────────────────────────────────────────────────────
  onActiveCardClick(): void {
    if (this.isList()) return;
    this.setViewMode('list');
    this.load();
  }

  onDeletedCardClick(): void {
    if (this.isDeletedList() || this.deletedCount < 1) return;
    this.setViewMode('list-deleted');
    this.loadDeleted();
  }

  // ── Pagination handler ─────────────────────────────────────────────────────
  onPageChange(page: number): void {
    this.pageNumber.set(page);
    this.reload();
  }

  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.pageNumber.set(1);
    this.reload();
  }

  // ── Data loading ───────────────────────────────────────────────────────────
  load(): void {
    this.bonApi[this.activeBonType]
      .list(this.pageNumber(), this.pageSize())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res: PagedResult<BonRecord>) => {
          this.dataSource.data = res.items;
          this.totalCount      = res.totalCount;
          this.cdr.markForCheck();
        },
        error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Failed to load.'),
      });
  }

  loadDeleted(): void {
    this.bonApi[this.activeBonType]
      .listDeleted(this.pageNumber(), this.pageSize())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res: PagedResult<BonRecord>) => {
          this.dataSource.data = res.items;
          this.totalCount      = res.totalCount;
          this.cdr.markForCheck();
        },
        error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Failed to load deleted bons.'),
      });
  }

  loadStats(): void {
    this.bonApi[this.activeBonType]
      .stats()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => { this.stats = res; this.cdr.markForCheck(); },
        error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Failed to load stats.'),
      });
  }

  reload(): void {
    if (this.isDeletedList()) this.loadDeleted();
    else this.load();
    this.loadStats();
  }

  // ── Pagination helpers ─────────────────────────────────────────────────────
  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize()); }

  // ── Stats getters ──────────────────────────────────────────────────────────
  get activeCount():  number { return this.stats?.activeCount  ?? 0; }
  get deletedCount(): number { return this.stats?.deletedCount ?? 0; }
  get totalBons():    number { return this.stats?.totalCount   ?? 0; }

  /** Computed total of pending lignes during create mode. */
  get pendingTotal(): number {
    return this.pendingLignes.reduce((s, l) => s + l.total, 0);
  }

  // ── Source bons loader (retour form) ──────────────────────────────────────
  loadSourceBons(): void {
    forkJoin({
      entres:  this.stock.getBonEntres(1, 1000),
      sorties: this.stock.getBonSorties(1, 1000),
    }).pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ entres, sorties }) => {
          this.allSourceBons = [
            ...entres.items.map(b => ({
              id: b.id, numero: b.numero, sourceType: RetourSourceType.BonEntre,
            })),
            ...sorties.items.map(b => ({
              id: b.id, numero: b.numero, sourceType: RetourSourceType.BonSortie,
            })),
          ].sort((a, b) => a.numero.localeCompare(b.numero));
          if (this.allSourceBons.length > 0) {
            this.headerForm.patchValue({ sourceId: this.allSourceBons[0].id, sourceType: this.allSourceBons[0].sourceType });
          }
          this.cdr.markForCheck();
        },
        error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Failed to load source bons.'),
      });
  }

  loadArticles(): void {
    this.articleService.getAll(1, 1000).subscribe({
      next: (res) => {
        this.articles = res.items.filter(a => !a.isDeleted);
        this.filteredArticles = this.articles;
        this.cdr.markForCheck();
      },
      error: () => this.flash('error', 'Failed to load articles.')
    });
  }

  filterArticles(): void {
    const q = this.articleSearchQuery.toLowerCase();
    this.filteredArticles = this.articles.filter(a =>
      a.libelle.toLowerCase().includes(q) ||
      a.codeRef.toLowerCase().includes(q) ||
      a.barCode.toLowerCase().includes(q)
    );
    if (this.filteredArticles.length > 0) {
      this.ligneForm.patchValue({ articleId: this.filteredArticles[0].id });
      this.onArticleSelected(this.filteredArticles[0].id);
    }
  }

  onArticleSelected(articleId: string): void {
    const article = this.articles.find(a => a.id === articleId);
    if (article) {
      this.ligneForm.patchValue({ price: article.prix });
    }
  }

  loadFournisseurs(): void {
    this.stock.getFournisseurs(1, 1000).subscribe({
      next: (res) => {
        this.fournisseurs = res.items.filter(f => !f.isDeleted && !f.isBlocked);
        this.filteredFournisseurs = this.fournisseurs;
        if (this.filteredFournisseurs.length > 0) {
          this.headerForm.patchValue({ fournisseurId: this.filteredFournisseurs[0].id });
        }
        this.cdr.markForCheck();
      },
      error: () => this.flash('error', 'Failed to load fournisseurs.')
    });
  }

  filterFournisseurs(): void {
    const q = this.fournisseurSearchQuery.toLowerCase();
    this.filteredFournisseurs = this.fournisseurs.filter(f =>
      f.name.toLowerCase().includes(q) ||
      f.phone.toLowerCase().includes(q)
    );
    if (this.filteredFournisseurs.length > 0) {
      this.headerForm.patchValue({ fournisseurId: this.filteredFournisseurs[0].id });
    }
  }

  loadClients(): void {
    this.clientService.getAll(1, 1000).subscribe({
      next: (res) => {
        this.clients = res.items.filter(f => !f.isBlocked);
        this.filteredClients = this.clients;
        if (this.filteredClients.length > 0) {
          this.headerForm.patchValue({ clientId: this.filteredClients[0].id });
        }
        this.cdr.markForCheck();
      },
      error: () => this.flash('error', 'Failed to load clients.')
    });
  }

  filterClients(): void {
    const q = this.clientSearchQuery.toLowerCase();
    this.filteredClients = this.clients.filter(c =>
      c.name.toLowerCase().includes(q) ||
      c.email.toLowerCase().includes(q)
    );
    if (this.filteredClients.length > 0) {
      this.headerForm.patchValue({ clientId: this.filteredClients[0].id });
    }
  }

  onSourceBonChange(id: string): void {
      const match = this.allSourceBons.find(b => b.id === id);
      if (!match) return;

      this.headerForm.patchValue({ sourceType: match.sourceType });

      if (this.isCreate() && this.activeBonType === 'retour') {
        const $req: Observable<BonRecord> = match.sourceType === RetourSourceType.BonEntre
          ? this.stock.getBonEntreById(id)
          : this.stock.getBonSortieById(id);

        $req.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: (res) => {
            // Aggregate lignes by articleId
            const aggregated = new Map<string, PendingLigne>();

            for (const l of res.lignes) {
              const existing = aggregated.get(l.articleId);
              if (existing) {
                // Sum quantities and recalculate total
                existing.quantity += l.quantity;
                existing.total = existing.quantity * existing.price; // ← FIXED: recalculate total after quantity update
              } else {
                aggregated.set(l.articleId, {
                  _localId: crypto.randomUUID(),
                  articleId: l.articleId,
                  articleLabel: this.getArticleLabel(l.articleId),
                  quantity: l.quantity,
                  price: l.price,
                  remarque: l.remarque,
                  total: l.quantity * l.price,
                });
              }
            }

            // Convert map to array
            this.pendingLignes = Array.from(aggregated.values());
            this.cdr.markForCheck();
          },
          error: (err) => {
            const error = err.error as HttpError;
            this.flash('error', error.message ?? 'Failed to load source bon lignes.');
          }
        });
      }
  }

  private getArticleLabel(articleId: string): string {
    const article = this.articles.find(a => a.id === articleId);
    return article ? `${article.codeRef} — ${article.libelle}` : articleId;
  }

  // ── Navigation ─────────────────────────────────────────────────────────────
  openCreate(): void {
    if (this.isCreate()) return;
    this.previousMode = this.viewMode();
    this.headerForm.reset({ numero: '', observation: '', sourceType: RetourSourceType.BonEntre });
    this.pendingLignes = [];
    this.inlineLigneOpen = false;
    this.inlineLigneLocalId = null;
    this.applyTypeValidators();
    if (this.activeBonType === 'retour') this.loadSourceBons();
    if (this.activeBonType === 'sortie') this.loadClients();
    if (this.activeBonType === 'entre')  this.loadFournisseurs();
    this.loadArticles();
    this.setViewMode('create');
  }

  openView(bon: BonRecord): void {
    if (this.isView()) return;
    this.previousMode = this.viewMode();
    this.selectedBon  = bon;
    this.setViewMode('view');
  }

  openEdit(bon: BonRecord): void {
    if (this.isEdit()) return;
    this.previousMode = this.viewMode();
    this.selectedBon  = bon;
    this.pendingLignes = [];         // not used in edit mode
    this.inlineLigneOpen = false;
    this.inlineLigneLocalId = null;
    this.applyTypeValidators();
    if (this.activeBonType === 'retour') this.loadSourceBons();
    if (this.activeBonType === 'entre')  this.loadFournisseurs();
    if (this.activeBonType === 'sortie') this.loadClients();
    this.loadArticles();
    this.headerForm.patchValue({
      numero:        bon.numero,
      observation:   bon.observation                           ?? '',
      fournisseurId: (bon as BonEntreResponse).fournisseurId  ?? '',
      clientId:      (bon as BonSortieResponse).clientId      ?? '',
      sourceId:      (bon as BonRetourResponse).sourceId      ?? '',
      sourceType:    (bon as BonRetourResponse).sourceType    ?? RetourSourceType.BonEntre,
      motif:         (bon as BonRetourResponse).motif         ?? '',
    });
    this.setViewMode('edit');
  }

  cancel(): void {
    this.inlineLigneOpen = false;
    this.inlineLigneLocalId = null;
    this.pendingLignes = [];
    const target = this.resolveCancel();
    this.setViewMode(target);
    if (!['view', 'edit'].includes(target)) this.selectedBon = null;
    if (target !== 'edit') this.headerForm.reset();
  }

  private resolveCancel(): ViewMode {
    const cur = this.viewMode();
    if (cur === 'edit' && this.previousMode === 'view' && this.selectedBon) return 'view';
    if (cur === 'view' && (this.previousMode === 'list' || this.previousMode === 'list-deleted'))
      return this.previousMode;
    if (cur === 'create') return this.previousMode ?? 'list';
    return 'list';
  }

  // ── Inline ligne form (inside create/edit panel) ───────────────────────────

  openInlineLigneAdd(): void {
    this.inlineLigneLocalId = null;
    this.ligneForm = this.buildLigneForm();
    // auto-select first article if available
    if (this.filteredArticles.length > 0) {
      const first = this.filteredArticles[0];
      this.ligneForm.patchValue({ articleId: first.id, price: first.prix });
    }
    this.inlineLigneOpen = true;
    this.cdr.markForCheck();
  }

  openInlineLigneEdit(ligne: PendingLigne | LigneRequestDto, isLocal: boolean): void {
    this.ligneForm = this.buildLigneForm();
    if (isLocal) {
      const pl = ligne as PendingLigne;
      this.inlineLigneLocalId = pl._localId;
      this.ligneForm.patchValue({
        articleId: pl.articleId,
        quantity:  pl.quantity,
        price:     pl.price,
        remarque:  pl.remarque ?? '',
      });
    } else {
      const sl = ligne as LigneResponseDto;
      this.inlineLigneLocalId = sl.id;   // reuse field to store saved id
      this.ligneForm.patchValue({
        articleId: sl.articleId,
        quantity:  sl.quantity,
        price:     sl.price,
        remarque:  (sl as any).remarque ?? '',
      });
    }
    this.inlineLigneOpen = true;
    this.cdr.markForCheck();
  }

  closeInlineLigne(): void {
    this.inlineLigneOpen    = false;
    this.inlineLigneLocalId = null;
    this.ligneForm = this.buildLigneForm();
  }

  submitInlineLigne(): void {
    if (this.ligneForm.invalid || this.isInlineLigneSubmitting) return;
    const val = this.ligneForm.value;

    // ── CREATE mode: manage pendingLignes locally ──────────────────────────
    if (this.isCreate()) {
      const article = this.articles.find(a => a.id === val.articleId);
      const label   = article ? `${article.codeRef} — ${article.libelle}` : val.articleId;

      if (this.inlineLigneLocalId) {
        // editing existing pending ligne
        const idx = this.pendingLignes.findIndex(l => l._localId === this.inlineLigneLocalId);
        if (idx !== -1) {
          this.pendingLignes[idx] = {
            ...this.pendingLignes[idx],
            articleId:    val.articleId,
            articleLabel: label,
            quantity:     val.quantity,
            price:        val.price,
            remarque:     val.remarque || null,
            total:        val.quantity * val.price,
          };
        }
        this.closeInlineLigne();
        this.cdr.markForCheck();
        return;
      } else {
        const existingIndex = this.pendingLignes.findIndex(l => l.articleId === val.articleId);

        if (existingIndex !== -1) {
          // Merge with existing ligne of the same article
          const existing = this.pendingLignes[existingIndex];
          const newQuantity = existing.quantity + val.quantity;
          this.pendingLignes[existingIndex] = {
            ...existing,
            quantity: newQuantity,
            total: newQuantity * existing.price, // ← FIXED: recalculate total
          };
        } else {
          this.pendingLignes.push({
            _localId: crypto.randomUUID(),
            articleId: val.articleId,
            articleLabel: label,
            quantity: val.quantity,
            price: val.price,
            remarque: val.remarque || null,
            total: val.quantity * val.price,
          });
        }
        this.closeInlineLigne();
        this.cdr.markForCheck();
        return;
      }
    }
    // ── EDIT mode: modify selectedBon.lignes directly ──────────────────────
    else if (this.isEdit() && this.selectedBon) {
      const article = this.articles.find(a => a.id === val.articleId);
      const label   = article ? `${article.codeRef} — ${article.libelle}` : val.articleId;

      // Editing an existing ligne (either saved or previously added)
      if (this.inlineLigneLocalId) {
        const idx = this.selectedBon.lignes.findIndex(l => l.id === this.inlineLigneLocalId);
        if (idx !== -1) {
          // Update the ligne in place
          this.selectedBon.lignes[idx] = {
            ...this.selectedBon.lignes[idx],
            articleId: val.articleId,
            quantity:  val.quantity,
            price:     val.price,
            remarque:  val.remarque || null,
            total:     val.quantity * val.price,
          };
        }
      }
      // Adding a new ligne
      else {
        const existingIndex = this.selectedBon.lignes.findIndex(l => l.articleId === val.articleId);
        if (existingIndex !== -1) {
          // Merge with existing ligne of the same article
          const existing = this.selectedBon.lignes[existingIndex];
          const newQuantity = existing.quantity + val.quantity;
          this.selectedBon.lignes[existingIndex] = {
            ...existing,
            quantity: newQuantity,
            // price remains as originally set
          };
        } else {
          // Create a new ligne with a temporary ID
          const newLigne: LigneResponseDto = {
            id:        `temp_${crypto.randomUUID()}`,   // temporary identifier
            articleId: val.articleId,
            quantity:  val.quantity,
            price:     val.price,
            remarque:  val.remarque || null,
            // Other fields (like total) are not stored, they are computed
          } as LigneResponseDto;
          this.selectedBon.lignes.push(newLigne);
        }
      }

      // Close the inline form and refresh the view
      this.closeInlineLigne();
      this.cdr.markForCheck();
      return;
    }
  }

  removePendingLigne(localId: string): void {
    this.pendingLignes = this.pendingLignes.filter(l => l._localId !== localId);
    this.cdr.markForCheck();
  }

  // ── Submit ─────────────────────────────────────────────────────────────────
  submit(): void {
    if (this.headerForm.invalid) return;
    if (this.isCreate() && this.pendingLignes.length === 0) {
      this.flash('error', 'Please add at least one ligne before creating the bon.');
      return;
    }

    const val      = this.headerForm.value;
    const creating = this.isCreate();

    const req$ = creating
      ? this.buildCreateRequest$(val)
      : this.buildUpdateRequest$(val);

    req$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.flash('success', creating ? 'Bon created.' : 'Bon updated.');
        this.cancel();
        this.reload();
      },
      error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Operation failed.'),
    });
  }



  getClientName(clientId: string): Observable<string> {
    return this.clientService.getById(clientId).pipe(
      map(client => client.name),
      catchError(() => of(clientId))
    );
  }

  private buildCreateRequest$(val: any): Observable<any> {
    const lignes = this.pendingLignes.map(l => ({
      articleId: l.articleId,
      quantity:  l.quantity,
      price:     l.price,
      remarque:  l.remarque ?? null,
    }));

    switch (this.activeBonType) {
      case 'entre':
        return this.stock.createBonEntre({
          numero:        val.numero,
          fournisseurId: val.fournisseurId,
          observation:   val.observation || null,
          lignes,
        } as CreateBonEntreRequest);

      case 'sortie':
        return this.stock.createBonSortie({
          numero:      val.numero,
          clientId:    val.clientId,
          observation: val.observation || null,
          lignes,
        } as CreateBonSortieRequest);

      default:
        return this.stock.createBonRetour({
          numero:      val.numero,
          sourceId:    val.sourceId,
          sourceType:  val.sourceType,
          motif:       val.motif,
          observation: val.observation || null,
          lignes,
        } as CreateBonRetourRequest);
    }
  }

  private buildUpdateRequest$(val: any): Observable<any> {
    const id = this.selectedBon!.id;
    switch (this.activeBonType) {
      case 'entre':
        return this.stock.updateBonEntre(id, {
          numero: val.numero, observation: val.observation || null,
          fournisseurId: val.fournisseurId,
          lignes: val.lignes
        } as UpdateBonEntreRequest);
      case 'sortie':
        return this.stock.updateBonSortie(id, {
          numero: val.numero, observation: val.observation || null,
          clientId: val.clientId,
          lignes: val.lignes
        } as UpdateBonSortieRequest);
      default:
        return this.stock.updateBonRetour(id, {
          numero: val.numero, motif: val.motif,
          observation: val.observation || null,
          sourceId: val.sourceId,
          lignes: val.lignes
        } as UpdateBonRetourRequest);
    }
  }

  // ── Delete ─────────────────────────────────────────────────────────────────
  delete(bon: BonRecord): void {
    this.dialog
      .open(ModalComponent, {
        width: '400px',
        data: {
          title:       'Delete Bon',
          message:     `Bon "${bon.numero}" will be soft-deleted. Proceed?`,
          confirmText: 'Delete',
          showCancel:  true,
          icon:        'auto_delete',
          iconColor:   'danger',
        },
      })
      .afterClosed()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;
          return this.bonApi[this.activeBonType].delete(bon.id);
        }),
      )
      .subscribe({
        next: () => {
          if (this.isView()) this.cancel();
          this.reload();
          this.flash('success', `Bon "${bon.numero}" deleted.`);
        },
        error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Delete failed.'),
      });
  }

  // ── Ligne modal (VIEW panel only) ──────────────────────────────────────────
  openAddLigne(bon: BonRecord): void {
    this.selectedBon    = bon;
    this.editingLigneId = null;
    this.ligneForm      = this.buildLigneForm();
    this.loadArticles();
  }

  openEditLigne(bon: BonRecord, ligne: any): void {
    this.selectedBon    = bon;
    this.editingLigneId = ligne.id;
    this.ligneForm      = this.buildLigneForm();
    this.ligneForm.patchValue({
      articleId: ligne.articleId,
      quantity:  ligne.quantity,
      price:     ligne.price,
      remarque:  ligne.remarque ?? '',
    });
    this.loadArticles();
  }

  // ── Casting helpers ────────────────────────────────────────────────────────
  asEntre(b: BonRecord):  BonEntreResponse  { return b as BonEntreResponse; }
  asSortie(b: BonRecord): BonSortieResponse { return b as BonSortieResponse; }
  asRetour(b: BonRecord): BonRetourResponse { return b as BonRetourResponse; }

  getLignes(b: BonRecord): LigneResponseDto[] {
    return b.lignes as LigneResponseDto[];
  }
  getTotal(b: BonRecord): number {
    return this.getLignes(b).reduce((s, l) => s + l.quantity * l.price, 0);
  }

  /** Lignes to show in edit mode (from the live selectedBon). */
  get editLignes(): LigneResponseDto[] {
    return this.selectedBon ? this.getLignes(this.selectedBon) : [];
  }

  trackByLocalId(_: number, l: PendingLigne): string { return l._localId; }

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

  dismissError(): void   { this.errors = []; }
  trackById(_: number, b: BonRecord): string { return b.id; }
  setViewMode(mode: ViewMode): void { this.viewMode.set(mode); this.cdr.markForCheck(); }

  private flattenObject(obj: any): string {
    return Object.keys(obj).map(k => {
      const v = obj[k];
      return v && typeof v === 'object' ? this.flattenObject(v) : v;
    }).join(' ').toLowerCase();
  }

  private getNestedValue(obj: any, path: string): any {
    return path.split('.').reduce((acc, k) => acc?.[k], obj);
  }

  removeLigne(selectedBon: BonRecord, ligneId: string): void {
    selectedBon.lignes = (selectedBon.lignes as LigneResponseDto[]).filter(l => l.id !== ligneId);
    this.getTotal(selectedBon);
    this.cdr.markForCheck();
  }

  // ── API dispatch map ───────────────────────────────────────────────────────
  private readonly bonApi: Record<BonType, BonApi> = {
    entre: {
      list:        (p, s) => this.stock.getBonEntres(p, s) as any,
      listDeleted: (p, s) => this.stock.getDeletedBonEntres(p, s) as any,
      stats:       ()     => this.stock.getBonEntreStats(),
      delete:      id     => this.stock.deleteBonEntre(id)
    },
    sortie: {
      list:        (p, s) => this.stock.getBonSorties(p, s) as any,
      listDeleted: (p, s) => this.stock.getDeletedBonSorties(p, s) as any,
      stats:       ()     => this.stock.getBonSortieStats(),
      delete:      id     => this.stock.deleteBonSortie(id)
    },
    retour: {
      list:        (p, s) => this.stock.getBonRetours(p, s) as any,
      listDeleted: (p, s) => this.stock.getDeletedBonRetours(p, s) as any,
      stats:       ()     => this.stock.getBonRetourStats(),
      delete:      id     => this.stock.deleteBonRetour(id)
    },
  };
}
