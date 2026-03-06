import { CurrencyConfigService } from '../../services/currency-config.service';
import { ArticleService, Article, Category, CreateArticleRequest, UpdateArticleRequest}  from './../../services/articles.service';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';

type ViewMode = 'list' | 'create' | 'edit' | 'view';

@Component({
  selector: 'app-article',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './home.html',
  styleUrls: ['./home.scss'],
})
export class ArticleComponent implements OnInit {
  articles: Article[] = [];
  categories: Category[] = [];
  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;

  viewMode: ViewMode = 'list';
  selectedArticle: Article | null = null;
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;
  searchQuery = '';

  articleForm: FormGroup;

  constructor(private articleService: ArticleService,
              private fb: FormBuilder,
              private currencyConfig: CurrencyConfigService)
  {
    this.articleForm = this.fb.group({
      libelle: ['', [Validators.required, Validators.minLength(2)]],
      prix: [0, [Validators.required, Validators.min(0)]],
      categoryId: ['', Validators.required],
    });
  }

  ngOnInit(): void {
    this.load();
    this.loadCategories();
  }

  // -------------------------------------------------------
  // Stats
  // -------------------------------------------------------
  get totalArticles(): number { return this.totalCount; }
  get activeCount(): number { return this.articles.filter(a => a.isActive).length; }
  get inactiveCount(): number { return this.articles.filter(a => !a.isActive).length; }
  get categoryCount(): number { return this.categories.length; }

  // -------------------------------------------------------
  // Load
  // -------------------------------------------------------
  load(): void {
    this.loading = true;
    this.error = null;
    this.articleService.getArticlesPagedByStatus(true, this.pageNumber, this.pageSize).subscribe({
      next: (res) => { this.articles = res.items; this.totalCount = res.totalCount; this.loading = false; },
      error: () => { this.error = 'Failed to load articles.'; this.loading = false; },
    });
  }

  loadCategories(): void {
    this.articleService.getAllCategories().subscribe({
      next: (cats) => (this.categories = cats),
    });
  }

  // -------------------------------------------------------
  // Search
  // -------------------------------------------------------
  get filteredArticles(): Article[] {
    if (!this.searchQuery.trim()) return this.articles;
    const q = this.searchQuery.toLowerCase();
    return this.articles.filter(a =>
      a.libelle.toLowerCase().includes(q) ||
      a.code.toLowerCase().includes(q) ||
      this.getCategoryName(a.categoryId).toLowerCase().includes(q)
    );
  }

  // -------------------------------------------------------
  // Pagination
  // -------------------------------------------------------
  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize); }
  prevPage(): void { if (this.pageNumber > 1) { this.pageNumber--; this.load(); } }
  nextPage(): void { if (this.pageNumber < this.totalPages) { this.pageNumber++; this.load(); } }

  // -------------------------------------------------------
  // CRUD
  // -------------------------------------------------------
  openCreate(): void {
    this.viewMode = 'create';
    this.selectedArticle = null;
    this.articleForm.reset({ libelle: '', prix: 0, categoryId: '' });
  }

  openEdit(article: Article): void {
    this.viewMode = 'edit';
    this.selectedArticle = article;
    this.articleForm.patchValue({ libelle: article.libelle, prix: article.prix, categoryId: article.categoryId });
  }

  openView(article: Article): void {
    this.viewMode = 'view';
    this.selectedArticle = article;
  }

  cancel(): void { this.viewMode = 'list'; this.selectedArticle = null; this.articleForm.reset(); }

  submit(): void {
    if (this.articleForm.invalid) return;
    const val = this.articleForm.value;
    if (this.viewMode === 'create') {
      this.articleService.createArticle(val as CreateArticleRequest).subscribe({
        next: () => { this.flash('Article created successfully.'); this.cancel(); this.load(); },
        error: () => (this.error = 'Failed to create article.'),
      });
    } else if (this.viewMode === 'edit' && this.selectedArticle) {
      this.articleService.updateArticle(this.selectedArticle.id, val as UpdateArticleRequest).subscribe({
        next: () => { this.flash('Article updated successfully.'); this.cancel(); this.load(); },
        error: () => (this.error = 'Failed to update article.'),
      });
    }
  }

  delete(article: Article): void {
    if (!confirm(`Delete "${article.libelle}"?`)) return;
    this.articleService.deleteArticle(article.id).subscribe({
      next: () => { this.flash('Article deleted.'); this.load(); },
      error: () => (this.error = 'Failed to delete article.'),
    });
  }

  toggleStatus(article: Article): void {
    const action$ = article.isActive
      ? this.articleService.deactivateArticle(article.id)
      : this.articleService.activateArticle(article.id);
    action$.subscribe({
      next: () => { this.flash(`Article ${article.isActive ? 'deactivated' : 'activated'}.`); this.load(); },
      error: () => (this.error = 'Status update failed.'),
    });
  }

  // -------------------------------------------------------
  // Helpers
  // -------------------------------------------------------
  getCategoryName(id: string): string {
    return this.categories.find(c => c.id === id)?.name ?? '—';
  }

  flash(msg: string): void {
    this.successMessage = msg;
    setTimeout(() => (this.successMessage = null), 3000);
  }

  dismissError(): void { this.error = null; }
  trackById(_: number, a: Article): string { return a.id; }

  get currencyCode(): string {
    return this.currencyConfig.code;
  }

  get currencyLocale(): string {
    return this.currencyConfig.locale;
  }
}
