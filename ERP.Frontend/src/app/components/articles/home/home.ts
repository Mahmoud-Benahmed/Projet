import { CurrencyConfigService } from '../../../services/currency-config.service';
import { ArticleService, Article, Category, CreateArticleRequest, UpdateArticleRequest, ArticleStatsDto}  from '../../../services/articles.service';
import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatInput } from "@angular/material/input";
import { ModalComponent } from '../../modal/modal';
import { MatDialog } from '@angular/material/dialog';
import { HttpError } from '../../../interfaces/ErrorDto';
import { MatIcon } from "@angular/material/icon";
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink, RouterLinkActive } from "@angular/router";
import { PaginationComponent } from "../../pagination/pagination";
import { AuthService } from '../../../services/auth.service';

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
  articles: Article[] = [];
  categories: Category[] = [];
  stats: ArticleStatsDto | null= null;

  pageNumber = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 25, 50];
  totalCount: number =0;

  viewMode: ViewMode = 'list';
  selectedArticle: Article | null = null;
  loading = false;
  errors: string[]= [];
  successMessage: string | null = null;
  searchQuery = '';

  readonly barCodePattern= /^\d{13}$/.source;

  articleForm: FormGroup;

  constructor(public authService: AuthService,
              private articleService: ArticleService,
              private fb: FormBuilder,
              private currencyConfig: CurrencyConfigService,
              private dialog: MatDialog,
              private cdr: ChangeDetectorRef)
  {
    this.articleForm = this.fb.group({
      libelle: ['', [Validators.required, Validators.minLength(2)]],
      prix: [0, [Validators.required, Validators.min(0)]],
      categoryId: ['', Validators.required],
      barCode: ['', [Validators.required, Validators.maxLength(13), Validators.minLength(13)]]
    });
  }

  ngOnInit(): void {
    this.reload();
  }

  // -------------------------------------------------------
  // Stats
  // -------------------------------------------------------
  get activeCount(): number { return this.stats?.ActiveCount ?? this.articles.filter(a => !a.isDeleted).length;}
  get deletedCount(): number { return this.stats?.DeletedCount ?? this.articles.filter(a => a.isDeleted).length; }
  get categoryCount(): number { return this.categories.length; }

  // -------------------------------------------------------
  // Load
  // -------------------------------------------------------
  load(): void {
    this.loading = true;
    this.errors = [];
    this.articleService.getAllArticles(this.pageNumber, this.pageSize).subscribe({
      next: (res) => {
        this.articles = res.items; this.totalCount= res.totalCount; this.cdr.markForCheck();
      },
      error: () => { this.flash('error', 'Failed to load articles.'); this.loading = false; },
    });
  }

  loadCategories(): void {
    this.articleService.getAllCategories().subscribe({
      next: (cats) => {this.categories = cats; this.cdr.markForCheck()},
    });
  }

  loadStats():void{
    this.loading= true;
    this.errors= [];
    this.articleService.getStats().subscribe({
      next: (res)=> {this.stats= res; this.cdr.markForCheck(); },
      error: ()=> {
        this.loading= false;
        this.flash('error', 'Failed to load stats.');
      }
    })
  }

  // -------------------------------------------------------
  // Search
  // -------------------------------------------------------
  get filteredArticles(): Article[] {
    if (!this.searchQuery.trim()) return this.articles;
    const q = this.searchQuery.toLowerCase();
    return this.articles.filter(a =>
      a.libelle.toLowerCase().includes(q) ||
      a.codeRef.toLowerCase().includes(q) ||
      this.getCategoryName(a.categoryId).toLowerCase().includes(q)
    );
  }

  // -------------------------------------------------------
  // Pagination
  // -------------------------------------------------------
  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize); }
  prevPage(): void { if (this.pageNumber > 1) { this.pageNumber--; this.reload(); } }
  nextPage(): void { if (this.pageNumber < this.totalPages) { this.pageNumber++; this.reload(); } }

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
    this.articleForm.patchValue({ libelle: article.libelle, prix: article.prix, categoryId: article.categoryId, barCode: article.barCode});
    this.cdr.markForCheck()
  }

  openView(article: Article): void {
    this.viewMode = 'view';
    this.selectedArticle = article;
    this.cdr.markForCheck()
  }

  cancel(): void { this.viewMode = 'list'; this.selectedArticle = null; this.articleForm.reset(); }

  submit(): void {
    if (this.articleForm.invalid) return;
    const val = this.articleForm.value;
    if (this.viewMode === 'create') {
      this.articleService.createArticle(val as CreateArticleRequest).subscribe({
        next: () => {
          this.reload();
          this.cancel();
          this.flash('success', `Article "${val.libelle}" created successfully.`);
        },
        error: (error) => {
          let err = error.error as HttpError;
          this.flash('error', err.message);
        }
      });
    } else if (this.viewMode === 'edit' && this.selectedArticle) {
      this.articleService.updateArticle(this.selectedArticle.id, val as UpdateArticleRequest).subscribe({
        next: () => {
          this.cancel();
          this.reload();
          this.flash('success', `Article "${val.libelle}" updated successfully.`);
        },
        error: (error) =>{
          const err= error.error as HttpError;
          this.flash('error', error.message);
        }
      });
    }
  }

  delete(article: Article): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '400px',
      data: {
        title: 'Delete Article',
        message: `Article ${article.libelle} will be deleted. Do you want to procceed ?`,
        confirmText: 'Delete',
        showCancel: true,
        icon: 'auto_delete',
        iconColor: 'danger'
      }
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        if (!result) return;
          this.articleService.delete(article.id).subscribe({
            next: () => {
              if(this.viewMode==='view'){
                this.cancel();
              }
              this.flash('success', `Article "${article.libelle}" has been deleted successfully.`)
              this.reload();
            },
            error: () => (this.flash('error', `Failed to delete article "${this.selectedArticle?.libelle}"`)),
          });
        }
      );
  }

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

  flashErrors(messages: string[]): void {
    this.errors = messages;
    setTimeout(() => { this.errors = []; this.cdr.markForCheck(); }, 4000);
    this.cdr.markForCheck();
  }
  dismissError(): void { this.errors = []; }

  reload(): void{
    this.load();
    this.loadCategories();
    this.loadStats();
    this.cdr.markForCheck();
  }
  // -------------------------------------------------------
  // Helpers
  // -------------------------------------------------------
  onPageSizeChange(): void {
    this.pageNumber = 1; // reset to first page on size change
    this.reload();
  }

  getCategoryName(id: string): string {
    return this.categories.find(c => c.id === id)?.name ?? '—';
  }

  trackById(_: number, a: Article): string { return a.id; }

  get currencyCode(): string {
    return this.currencyConfig.code;
  }

  get currencyLocale(): string {
    return this.currencyConfig.locale;
  }
}
