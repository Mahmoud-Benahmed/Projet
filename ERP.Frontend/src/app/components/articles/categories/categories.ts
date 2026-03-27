import { ChangeDetectorRef, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatIcon } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AuthService, PRIVILEGES } from '../../../services/auth/auth.service';
import { ModalComponent } from '../../modal/modal';
import { PaginationComponent } from '../../pagination/pagination';
import { HttpError } from '../../../interfaces/ErrorDto';
import { CategoryRequestDto, ArticleCategoryResponseDto, CategoryService, ArticleCategoryStatsDto } from '../../../services/articles/categories.service';
import { MatTableDataSource } from '@angular/material/table';

type ViewMode = 'list' | 'list-deleted' | 'create' | 'edit' | 'view';

@Component({
  selector: 'app-article-categories',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, MatIcon, PaginationComponent],
  templateUrl: './categories.html',
  styleUrls: ['./categories.scss'],
})
export class ArticleCategoriesComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  dataSource = new MatTableDataSource<ArticleCategoryResponseDto>([]);
  stats: ArticleCategoryStatsDto | null = null;

  pageNumber = signal(1);
  pageSize = signal(10);
  pageSizeOptions = [5, 10, 25, 50];
  totalCount = 0;

  sortColumn = '';
  sortDirection: 'asc' | 'desc' = 'asc';

  private previousMode: ViewMode = 'list';

  selectedCategory: ArticleCategoryResponseDto | null = null;
  loading = false;
  errors: string[] = [];
  successMessage: string | null = null;
  searchQuery = '';

  viewMode = signal<ViewMode>('list');
  isMode = (mode: ViewMode) => computed(() => this.viewMode() === mode);

  isList        = this.isMode('list');
  isDeletedList = this.isMode('list-deleted');
  isCreate      = this.isMode('create');
  isEdit        = this.isMode('edit');
  isView        = this.isMode('view');

  readonly PRIVILEGES = PRIVILEGES;
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

  // ── Page title ────────────────────────────────────────────────────────────

  get pageTitle(): string {
    if (this.isCreate())      return 'Add Category';
    if (this.isEdit())        return 'Edit Category';
    if (this.isView())        return 'Category Details';
    if (this.isDeletedList()) return 'Deleted Categories';
    return 'List Categories';
  }

  // ── Search ────────────────────────────────────────────────────────────────

  applyFilter(): void {
    this.dataSource.filter = this.searchQuery.trim().toLowerCase();
    this.pageNumber.set(1);
  }

  // ── Pagination ────────────────────────────────────────────────────────────

  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize()); }

  onPageSizeChange(): void { this.pageNumber.set(1); this.load(); }

  get activeCategories():  number { return this.stats?.activeCategories  ?? 0; }
  get deletedCategories(): number { return this.stats?.deletedCategories ?? 0; }

  // ── Sort ──────────────────────────────────────────────────────────────────

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

    const start = (this.pageNumber() - 1) * this.pageSize();
    return filtered.slice(start, start + this.pageSize());
  }

  // ── Load ──────────────────────────────────────────────────────────────────

  load(): void {
    this.errors = [];
    this.categoryService.getPaged(this.pageNumber(), this.pageSize()).subscribe({
      next: (res) => {
        this.dataSource.data = res.items;
        this.totalCount = res.totalCount;
        this.cdr.markForCheck();
      },
      error: () => this.flash('error', 'Failed to load categories.'),
    });
  }

  listDeleted(): void {
    this.categoryService.getDeleted(this.pageNumber(), this.pageSize()).subscribe({
      next: (result) => {
        this.dataSource.data = result.items;
        this.totalCount = result.totalCount;
        this.cdr.markForCheck();
      },
      error: () => this.flash('error', 'Failed to load deleted categories.'),
    });
  }

  loadStats(): void {
    this.categoryService.getStats().subscribe({
      next: (res) => {
        this.stats = res;
        // auto-switch back to list when no deleted items remain
        if (this.isDeletedList() && res.deletedCategories === 0) {
          this.setViewMode('list');
          this.load();
        }
        this.cdr.markForCheck();
      },
      error: () => this.flash('error', 'Failed to load stats.'),
    });
  }

  reload(): void {
    if (this.isDeletedList()) {
      this.listDeleted();
    } else {
      this.load();
    }
    this.loadStats();
    this.cdr.markForCheck();
  }

  // ── Stat card clicks ──────────────────────────────────────────────────────

  onActiveCardClick(): void {
    if (this.isList()) return;
    this.setViewMode('list');
    this.load();
  }

  onDeletedCardClick(): void {
    if (this.isDeletedList() || this.deletedCategories < 1) return;
    this.setViewMode('list-deleted');
    this.listDeleted();
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────

  openCreate(): void {
    if (this.isCreate()) return;
    this.previousMode = this.viewMode();
    this.setViewMode('create');
    this.selectedCategory = null;
    this.categoryForm.reset({ name: '', tva: null });
  }

  openEdit(category: ArticleCategoryResponseDto): void {
    if (this.isEdit()) return;
    this.previousMode = this.viewMode();
    this.setViewMode('edit');
    this.selectedCategory = category;
    this.categoryForm.patchValue({ name: category.name, tva: category.tva });
    this.cdr.markForCheck();
  }

  openView(category: ArticleCategoryResponseDto): void {
    if (this.isView()) return;
    this.previousMode = this.viewMode();
    this.setViewMode('view');
    this.selectedCategory = category;
    this.cdr.markForCheck();
  }

  cancel(): void {
    this.setViewMode(this.previousMode);
    this.selectedCategory = null;
    this.categoryForm.reset();
  }

  restore(cat: ArticleCategoryResponseDto): void {
    this.categoryService.restore(cat.id).subscribe({
      next: () => {
        this.flash('success', `Category "${cat.name}" has been restored. You can find it in the Categories page.`);
        if (this.isView()) this.cancel();
        this.reload();
      },
      error: (error) => {
        const err = error.error as HttpError;
        this.flash('error', err?.message ?? error.message);
      },
    });
  }

  submit(): void {
    if (this.categoryForm.invalid) return;
    const dto: CategoryRequestDto = this.categoryForm.value;

    if (this.isCreate()) {
      this.categoryService.create(dto).subscribe({
        next: () => { this.cancel(); this.reload(); this.flash('success', `Category "${dto.name}" created successfully.`); },
        error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Failed to create category.'),
      });
    } else if (this.isEdit() && this.selectedCategory) {
      this.categoryService.update(this.selectedCategory.id, dto).subscribe({
        next: () => { this.cancel(); this.reload(); this.flash('success', `Category "${dto.name}" updated successfully.`); },
        error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Failed to update category.'),
      });
    }
  }

  delete(category: ArticleCategoryResponseDto): void {
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
            if (this.isView()) this.cancel();
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

  trackById(_: number, c: ArticleCategoryResponseDto): string { return c.id; }

  setViewMode(mode: ViewMode): void {
    this.viewMode.set(mode);
    this.cdr.markForCheck();
  }
}
