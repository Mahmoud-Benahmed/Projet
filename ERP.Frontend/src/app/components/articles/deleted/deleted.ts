import { Component, DestroyRef, inject, OnInit, ViewChild, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { FormBuilder, FormGroup, FormsModule, Validators } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ModalComponent } from '../../modal/modal';
import { MatDialog } from '@angular/material/dialog';
import { Article, ArticleService, ArticleStatsDto, Category, PagedResult } from '../../../services/articles.service';
import { PaginationComponent } from "../../pagination/pagination";

@Component({
  selector: 'app-deactivated',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatInputModule,
    MatFormFieldModule,
    MatTooltipModule,
    MatDividerModule,
    MatSnackBarModule,
    RouterLinkActive,
    RouterLink,
    PaginationComponent
],
  templateUrl: './deleted.html',
  styleUrl: './deleted.scss',
})
export class DeletedArticlesComponent implements OnInit {
  @ViewChild(MatSort) sort!: MatSort;

  private readonly destroyRef = inject(DestroyRef);

  articles: Article[] = [];
  stats: ArticleStatsDto | null = null;
  categories: Category[] = [];

  dataSource = new MatTableDataSource<Article>([]);

  totalCount= 0;
  pageNumber = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 25, 50];

  isLoading = false;
  searchQuery = '';


  selectedArticle: Article | null = null;
  viewMode: 'list' | 'view' | 'edit'= 'list';

  // FIX 8: declare currency properties referenced in the template
  currencyCode = 'EUR';
  currencyLocale = 'fr-FR';

  articleForm: FormGroup;

  constructor(
    private snackBar: MatSnackBar,
    private articleService: ArticleService,
    private fb: FormBuilder,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef
  )   {
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

  load(): void {
    this.isLoading = true;
    this.articleService.getDeletedArticles(this.pageNumber, this.pageSize).subscribe({
      next: (result: PagedResult<Article>) => {
        // FIX 10: also populate this.articles so filteredArticles getter works correctly
        this.articles = result.items;
        this.dataSource.data = result.items;
        this.totalCount = result.totalCount;
        this.dataSource.sort = this.sort;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.snackBar.open('Failed to load deleted articles.', 'Dismiss', { duration: 3000 });
      },
    });
  }

  loadStats(): void {
    this.articleService.getStats().subscribe({
      next: (result) => {this.stats = result;},
      error: () => {
        this.snackBar.open('Failed to load stats.', 'Dismiss', { duration: 3000 });
      },
    });
  }

  loadCategories(): void {
    this.articleService.getAllCategories().subscribe({
      next: (cats) => {
        this.categories = cats;
        this.cdr.markForCheck();
      },
    });
  }

  // FIX 11: trackById was referenced in the template but never defined
  trackById(_index: number, article: Article): string {
    return article.id;
  }

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  prevPage(): void {
    if (this.pageNumber > 1) {
      this.pageNumber--;
      this.reload();
    }
  }

  nextPage(): void {
    if (this.pageNumber < this.totalPages) {
      this.pageNumber++;
      this.reload();
    }
  }

  applyFilter(): void {
    // keep dataSource filter in sync for any MatTable native filtering needs
    this.dataSource.filter = this.searchQuery.trim().toLowerCase();
  }

  get filteredArticles(): Article[] {
    if (!this.searchQuery.trim()) return this.articles;
    const q = this.searchQuery.toLowerCase();
    return this.articles.filter(
      (a) =>
        a.libelle.toLowerCase().includes(q) ||
        a.codeRef.toLowerCase().includes(q) ||
        this.getCategoryName(a.categoryId).toLowerCase().includes(q),
    );
  }

  getCategoryName(id: string): string {
    return this.categories.find((c) => c.id === id)?.name ?? '—';
  }

  recover(article: Article): void {
    this.articleService.recover(article.id).subscribe({
      next: () => {
        const dialogRef = this.dialog.open(ModalComponent, {
          width: '400px',
          data: {
            title: 'Article recovered successfully',
            message: `Article "${article.libelle}" has been recovered. You can find it in the Articles page.`,
            confirmText: 'Ok',
            showCancel: false,
            icon: 'settings_backup_restore',
            iconColor: 'success',
          },
        });

        dialogRef
          .afterClosed()
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe(() => this.reload());
      },
      error: () =>
        this.snackBar.open('Failed to recover article.', 'Dismiss', { duration: 3000 }),
    });
  }

  onPageSizeChange(): void {
    this.pageNumber = 1; // reset to first page on size change
    this.reload();
  }

  openView(article: Article): void {
    this.selectedArticle = article;
    this.viewMode = 'view';
    this.cdr.markForCheck();
  }
  openEdit(article: Article): void {
    this.viewMode = 'edit';
    this.selectedArticle = article;
    this.articleForm.patchValue({ libelle: article.libelle, prix: article.prix, categoryId: article.categoryId, barCode: article.barCode});
    this.cdr.markForCheck()
  }

  // Replace cancel
  cancel(): void {
    this.selectedArticle = null;
    this.viewMode = 'list';
    this.articleForm.reset();
  }

  reload(): void {
    this.loadCategories(); // FIX 9: loadCategories() was defined but never called
    this.load();
    this.loadStats();
    this.cdr.markForCheck();
  }
}
