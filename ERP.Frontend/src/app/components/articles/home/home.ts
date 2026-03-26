// article.component.ts
import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatIcon } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ArticleService, ArticleResponseDto, ArticleStatsDto, CreateArticleRequestDto, UpdateArticleRequestDto } from './../../../services/articles/articles.service';
import { AuthService, PRIVILEGES } from '../../../services/auth/auth.service';
import { CurrencyConfigService } from '../../../services/currency-config.service';
import { ModalComponent } from '../../modal/modal';
import { PaginationComponent } from '../../pagination/pagination';
import { HttpError } from '../../../interfaces/ErrorDto';
import { ArticleCategoryResponseDto, CategoryService } from '../../../services/articles/categories.service';
import { MatTableDataSource } from '@angular/material/table';

type ViewMode = 'list' | 'create' | 'edit' | 'view';

@Component({
  selector: 'app-article',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, MatIcon, RouterLink, RouterLinkActive, PaginationComponent],
  templateUrl: './home.html',
  styleUrls: ['./home.scss'],
})
export class ArticleComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  dataSource = new MatTableDataSource<ArticleResponseDto>([]);

  categories: ArticleCategoryResponseDto[] = [];
  stats: ArticleStatsDto | null = null;

  pageNumber = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 25, 50];
  totalCount = 0;

  viewMode: ViewMode = 'list';
  selectedArticle: ArticleResponseDto | null = null;
  loading = false;
  errors: string[] = [];
  successMessage: string | null = null;
  searchQuery = '';

  readonly barCodePattern = /^\d{8,13}$/.source;
  readonly PRIVILEGES= PRIVILEGES;
  articleForm: FormGroup;

  sortColumn: string = '';
  sortDirection: 'asc' | 'desc' = 'asc';

  constructor(
    public authService: AuthService,
    private articleService: ArticleService,
    private categoryService: CategoryService,
    private currencyConfig: CurrencyConfigService,
    private fb: FormBuilder,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef
  ) {
    this.articleForm = this.fb.group({
      libelle:    ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
      prix:       [null, [Validators.required, Validators.min(0.01)]],
      categoryId: ['', Validators.required],
      barCode:    ['', [Validators.required, Validators.minLength(8), Validators.maxLength(13)]],
      tva:        [null, [Validators.min(0.01), Validators.max(100)]],
    });
  }

  ngOnInit(): void {
    this.dataSource.filterPredicate = (data, filter) => {
      return this.flattenObject(data).includes(filter);
    };

    this.reload();
  }

  // ── Stats ─────────────────────────────────────────────────────────────────

  get activeCount():    number { return this.stats?.activeCount    ?? 0; }
  get deletedCount():   number { return this.stats?.deletedCount   ?? 0; }
  get totalCountStat(): number { return this.stats?.totalCount     ?? 0; }
  get categoriesCount():number { return this.stats?.categoriesCount ?? 0; }

  // ── Currency ──────────────────────────────────────────────────────────────

  get currencyCode():   string { return this.currencyConfig.code;   }
  get currencyLocale(): string { return this.currencyConfig.locale; }

  // ── Search ────────────────────────────────────────────────────────────────

  sortBy(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
  }

  get sortedData() {
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

  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize); }
  onPageSizeChange(): void { this.pageNumber = 1; this.load(); }

  // ── Load ──────────────────────────────────────────────────────────────────

  load(): void {
    this.loading = true;
    this.errors = [];
    this.articleService.getAll(this.pageNumber, this.pageSize).subscribe({
      next: (res) => {
        this.dataSource.data = res.items;
        this.totalCount = res.totalCount;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.flash('error', 'Failed to load articles.');
        this.loading = false;
      },
    });
  }

  loadCategories(): void {
    this.categoryService.getAll().subscribe({
      next: (cats) => { this.categories = cats; this.cdr.markForCheck(); },
      error: () => this.flash('error', 'Failed to load categories.'),
    });
  }

  loadStats(): void {
    this.articleService.getStats().subscribe({
      next: (res) => { this.stats = res; this.cdr.markForCheck(); },
      error: () => this.flash('error', 'Failed to load stats.'),
    });
  }

  reload(): void {
    this.load();
    this.loadCategories();
    this.loadStats();
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────

  openCreate(): void {
    this.viewMode = 'create';
    this.selectedArticle = null;
    this.articleForm.reset({ libelle: '', prix: null, categoryId: '', barCode: '', tva: null });
  }

  openEdit(article: ArticleResponseDto): void {
    this.viewMode = 'edit';
    this.selectedArticle = article;
    this.articleForm.patchValue({
      libelle:    article.libelle,
      prix:       article.prix,
      categoryId: article.category.id,
      barCode:    article.barCode,
      tva:        article.tva,
    });
    this.cdr.markForCheck();
  }

  openView(article: ArticleResponseDto): void {
    this.viewMode = 'view';
    this.selectedArticle = article;
    this.cdr.markForCheck();
  }

  cancel(): void {
    this.viewMode = 'list';
    this.selectedArticle = null;
    this.articleForm.reset();
  }

  submit(): void {
    if (this.articleForm.invalid) return;
    const val = this.articleForm.value;

    if (this.viewMode === 'create') {
      const dto: CreateArticleRequestDto = {
        libelle:    val.libelle,
        prix:       val.prix,
        categoryId: val.categoryId,
        barCode:    val.barCode,
        tva:        val.tva ?? undefined,
      };
      this.articleService.create(dto).subscribe({
        next: () => { this.reload(); this.cancel(); this.flash('success', `Article "${val.libelle}" created successfully.`); },
        error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Failed to create article.'),
      });

    } else if (this.viewMode === 'edit' && this.selectedArticle) {
      const dto: UpdateArticleRequestDto = {
        libelle:    val.libelle,
        prix:       val.prix,
        categoryId: val.categoryId,
        barCode:    val.barCode ?? undefined,
        tva:        val.tva ?? undefined,
      };
      this.articleService.update(this.selectedArticle.id, dto).subscribe({
        next: () => { this.cancel(); this.reload(); this.flash('success', `Article "${val.libelle}" updated successfully.`); },
        error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Failed to update article.'),
      });
    }
  }

  delete(article: ArticleResponseDto): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '400px',
      data: {
        title:       'Delete Article',
        message:     `Article "${article.libelle}" will be permanently deleted. Do you want to proceed?`,
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
        this.articleService.delete(article.id).subscribe({
          next: () => {
            if (this.viewMode === 'view') this.cancel();
            this.flash('success', `Article "${article.libelle}" deleted successfully.`);
            this.reload();
          },
          error: () => this.flash('error', `Failed to delete article "${article.libelle}".`),
        });
      });
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

  trackById(_: number, a: ArticleResponseDto): string { return a.id; }
  private flattenObject(obj: any): string {
    return Object.keys(obj)
      .map(key => {
        const value = obj[key];
        if (value && typeof value === 'object') {
          return this.flattenObject(value);
        }
        return value;
      })
      .join(' ')
      .toLowerCase();
  }

  private getNestedValue(obj: any, path: string): any {
    return path.split('.').reduce((acc, key) => acc?.[key], obj);
  }

}
