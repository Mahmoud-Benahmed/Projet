// category.component.ts
import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatIcon } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AuthService } from '../../../services/auth/auth.service';
import { ModalComponent } from '../../modal/modal';
import { PaginationComponent } from '../../pagination/pagination';
import { HttpError } from '../../../interfaces/ErrorDto';
import { CategoryRequestDto, CategoryResponseDto, CategoryService } from '../../../services/articles/categories.service';
import { MatTableDataSource } from '@angular/material/table';

type ViewMode = 'list' | 'create' | 'edit' | 'view';

@Component({
  selector: 'app-article-categories',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, MatIcon, PaginationComponent],
  templateUrl: './categories.html',
  styleUrls: ['./categories.scss'],
})
export class ArticleCategoriesComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

    dataSource = new MatTableDataSource<CategoryResponseDto>([]);

  pageNumber = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 25, 50];
  totalCount = 0;

  sortColumn = '';
  sortDirection: 'asc' | 'desc' = 'asc';

  viewMode: ViewMode = 'list';
  selectedCategory: CategoryResponseDto | null = null;
  loading = false;
  errors: string[] = [];
  successMessage: string | null = null;
  searchQuery = '';

  categoryForm: FormGroup;

  constructor(
    public authService: AuthService,
    private categoryService: CategoryService,
    private fb: FormBuilder,
    private dialog: MatDialog,
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

  sortBy(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
  }

  get sortedData(): CategoryResponseDto[] {
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
    this.categoryService.getPaged(this.pageNumber, this.pageSize).subscribe({
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

  reload(): void { this.load(); }

  // ── CRUD ──────────────────────────────────────────────────────────────────

  openCreate(): void {
    this.viewMode = 'create';
    this.selectedCategory = null;
    this.categoryForm.reset({ name: '', tva: null });
  }

  openEdit(category: CategoryResponseDto): void {
    this.viewMode = 'edit';
    this.selectedCategory = category;
    this.categoryForm.patchValue({ name: category.name, tva: category.tva });
    this.cdr.markForCheck();
  }

  openView(category: CategoryResponseDto): void {
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

    if (this.viewMode === 'create') {
      this.categoryService.create(dto).subscribe({
        next: () => { this.reload(); this.cancel(); this.flash('success', `Category "${dto.name}" created successfully.`); },
        error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Failed to create category.'),
      });

    } else if (this.viewMode === 'edit' && this.selectedCategory) {
      this.categoryService.update(this.selectedCategory.id, dto).subscribe({
        next: () => { this.cancel(); this.reload(); this.flash('success', `Category "${dto.name}" updated successfully.`); },
        error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Failed to update category.'),
      });
    }
  }

  delete(category: CategoryResponseDto): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '400px',
      data: {
        title:       'Delete Category',
        message:     `Category "${category.name}" will be permanently deleted. Do you want to proceed?`,
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
        this.categoryService.delete(category.id).subscribe({
          next: () => {
            if (this.viewMode === 'view') this.cancel();
            this.flash('success', `Category "${category.name}" deleted successfully.`);
            this.reload();
          },
          error: () => this.flash('error', `Failed to delete category "${category.name}".`),
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

  trackById(_: number, c: CategoryResponseDto): string { return c.id; }
}
