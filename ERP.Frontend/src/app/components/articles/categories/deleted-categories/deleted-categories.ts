import { PRIVILEGES } from './../../../../services/auth/auth.service';
// category.component.ts
import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatIcon } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';

import { AuthService } from '../../../../services/auth/auth.service';
import { PaginationComponent } from '../../../pagination/pagination';
import { HttpError } from '../../../../interfaces/ErrorDto';
import { CategoryRequestDto, ArticleCategoryResponseDto, CategoryService, ArticleCategoryStatsDto } from '../../../../services/articles/categories.service';
import { MatTableDataSource } from '@angular/material/table';

type ViewMode = 'list' | 'create' | 'edit' | 'view';

@Component({
  selector: 'app-article-categories-deleted',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, MatIcon, PaginationComponent],
  templateUrl: './deleted-categories.html',
  styleUrls: ['./deleted-categories.scss'],
})
export class DeletedArticleCategoriesComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

    dataSource = new MatTableDataSource<ArticleCategoryResponseDto>([]);

  pageNumber = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 25, 50];
  totalCount = 0;

  sortColumn = '';
  sortDirection: 'asc' | 'desc' = 'asc';

  viewMode: ViewMode = 'list';
  selectedCategory: ArticleCategoryResponseDto | null = null;
  loading = false;
  errors: string[] = [];
  successMessage: string | null = null;
  searchQuery = '';

  stats: ArticleCategoryStatsDto | null = null;

  readonly PRIVILEGES= PRIVILEGES;

  categoryForm: FormGroup;

  constructor(
    public authService: AuthService,
    private categoryService: CategoryService,
    private fb: FormBuilder,
    private cdr: ChangeDetectorRef
  ) {
    this.categoryForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      tva:  [null, [Validators.required, Validators.min(0), Validators.max(100)]],
    });
  }

  ngOnInit(): void {
    this.reload();
  }

  // ── Search ────────────────────────────────────────────────────────────────

  applyFilter(): void {
    this.dataSource.filter = this.searchQuery.trim().toLowerCase();
    this.pageNumber = 1;
  }

  // ── Pagination ────────────────────────────────────────────────────────────

  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize); }

  onPageSizeChange(): void { this.pageNumber = 1; this.load(); }

  get activeCategories():   number { return this.stats?.activeCategories   ?? 0; }
  get deletedCategories():  number { return this.stats?.deletedCategories  ?? 0; }


  sortBy(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
  }

  get sortedData(): ArticleCategoryResponseDto[] {
    const filtered = [...this.dataSource.filteredData];

    if (this.sortColumn) {
      filtered.sort((a, b) => {
        let valA = (a as any)[this.sortColumn];
        let valB = (b as any)[this.sortColumn];

        if (valA == null) return 1;
        if (valB == null) return -1;

        if (typeof valA === 'string') valA = valA.toLowerCase();
        if (typeof valB === 'string') valB = valB.toLowerCase();

        return (valA < valB ? -1 : valA > valB ? 1 : 0) * (this.sortDirection === 'asc' ? 1 : -1);
      });
    }

    // client-side pagination slice
    const start = (this.pageNumber - 1) * this.pageSize;
    return filtered.slice(start, start + this.pageSize);
  }
  // ── Load ──────────────────────────────────────────────────────────────────

  load(): void {
    this.loading = true;
    this.errors = [];
    this.categoryService.getDeleted(this.pageNumber, this.pageSize).subscribe({
      next: (res) => {
        this.dataSource.data = res.items;
        this.totalCount = res.totalCount;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.flash('error', 'Failed to load categories.');
        this.loading = false;
      },
    });
  }

  loadStats(): void {
    this.categoryService.getStats().subscribe({
      next: (res) => { this.stats = res; this.cdr.markForCheck(); },
      error: () => this.flash('error', 'Failed to load stats.'),
    });
  }


  reload(): void { this.load(); this.loadStats(); this.cdr.markForCheck();}

  // ── CRUD ──────────────────────────────────────────────────────────────────
  openView(category: ArticleCategoryResponseDto): void {
    this.viewMode = 'view';
    this.selectedCategory = category;
    this.cdr.markForCheck();
  }

  cancel(): void {
    this.viewMode = 'list';
    this.selectedCategory = null;
    this.categoryForm.reset();
  }

  submit(): void {
    if (this.categoryForm.invalid) return;
    const dto: CategoryRequestDto = this.categoryForm.value;
    this.categoryService.create(dto).subscribe({
        next: () => { this.reload(); this.cancel(); this.flash('success', `Category "${dto.name}" created successfully.`); },
        error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Failed to create category.'),
    });
  }

  restore(cat: ArticleCategoryResponseDto): void {
    this.categoryService.restore(cat.id).subscribe({
      next: () => {
        this.flash('success', `Category "${cat.name}" has been restored. You can find it in the Categories page.`);
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

  trackById(_: number, c: ArticleCategoryResponseDto): string { return c.id; }
}
