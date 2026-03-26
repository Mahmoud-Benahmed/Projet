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
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { FormBuilder, FormGroup, FormsModule, Validators } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ArticleResponseDto, ArticleService, ArticleStatsDto, PagedResult } from '../../../services/articles.service';
import { PaginationComponent } from "../../pagination/pagination";
import { AuthService, PRIVILEGES } from '../../../services/auth/auth.service';
import { HttpError } from '../../../interfaces/ErrorDto';
import { ArticleCategoryResponseDto } from '../../../services/articles/categories.service';

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

  articles: ArticleResponseDto[] = [];
  stats: ArticleStatsDto | null = null;
  categories: ArticleCategoryResponseDto[] = [];

  dataSource = new MatTableDataSource<ArticleResponseDto>([]);

  totalCount= 0;
  pageNumber = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 25, 50];

  isLoading = false;
  error: string | null = null;
  successMessage: string | null = null;
  searchQuery = '';

  sortColumn: string = '';
  sortDirection: 'asc' | 'desc' = 'asc';

  selectedArticle: ArticleResponseDto | null = null;
  viewMode: 'list' | 'view' = 'list';

  // FIX 8: declare currency properties referenced in the template
  currencyCode = 'EUR';
  currencyLocale = 'fr-FR';

  readonly PRIVILEGES= PRIVILEGES;

  articleForm: FormGroup;


  constructor(
    public authService: AuthService,
    private articleService: ArticleService,
    private fb: FormBuilder,
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
    this.dataSource.filterPredicate = (data, filter) => {
      return this.flattenObject(data).includes(filter);
    };
    this.reload();
  }

  load(): void {
    this.isLoading = true;
    this.articleService.getDeletedArticles(this.pageNumber, this.pageSize).subscribe({
      next: (result: PagedResult<ArticleResponseDto>) => {
        this.articles = result.items;
        this.dataSource.data = result.items;
        this.totalCount = result.totalCount;
        this.dataSource.sort = this.sort;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.flash('error', 'Failed to load deleted articles.');
      },
    });
  }

  loadStats(): void {
    this.articleService.getStats().subscribe({
      next: (result) => {this.stats = result;},
      error: () => {
        this.flash('error', 'Failed to load stats.');
      },
    });
  }

  loadCategories(): void {
    this.articleService.getAllCategories().subscribe({
      next: (cats) => {
        this.categories = cats;
        this.cdr.markForCheck();},
      error: () => {
        this.flash('error', 'Failed to load categories.');}
    });
  }

  // FIX 11: trackById was referenced in the template but never defined
  trackById(_index: number, ArticleResponseDto: ArticleResponseDto): string {
    return ArticleResponseDto.id;
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
    // keep dataSource filter in sync for any MatTable native filtering needs
    this.dataSource.filter = this.searchQuery.trim().toLowerCase();
  }

  get filteredArticles(): ArticleResponseDto[] {
    if (!this.searchQuery.trim()) return this.articles;
    const q = this.searchQuery.toLowerCase();
    return this.articles.filter(
      (a) =>
        a.libelle.toLowerCase().includes(q) ||
        a.codeRef.toLowerCase().includes(q) ||
        a.category.name.toLowerCase().includes(q),
    );
  }

  restore(ArticleResponseDto: ArticleResponseDto): void {
    this.articleService.restore(ArticleResponseDto.id).subscribe({
      next: () => {
        this.flash('success', `ArticleResponseDto "${ArticleResponseDto.libelle}" has been restored. You can find it in the Articles page.`);
        this.reload();
        if(this.viewMode==='view'){
          this.cancel();
        }
      },
      error: (error) =>{
        const err= error.error as HttpError;
        this.flash('error', error.message);
      }
    });
  }

  onPageSizeChange(): void {
    this.pageNumber = 1; // reset to first page on size change
    this.reload();
  }

  openView(ArticleResponseDto: ArticleResponseDto): void {
    this.selectedArticle = ArticleResponseDto;
    this.viewMode = 'view';
    this.cdr.markForCheck();
  }

  flash(type: 'success' | 'error', msg: string): void {
    if(type === 'success'){
      this.successMessage = msg; setTimeout(() => (this.successMessage = null), 3000);
    }
    else{
      this.error = msg; setTimeout(() => (this.error = null), 3000);
    }
    this.cdr.markForCheck();
  }
  dismissError(): void { this.error = null; }

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
