import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatIcon } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatTableDataSource } from '@angular/material/table';
import { AuthService, PRIVILEGES } from '../../../../services/auth/auth.service';
import { ModalComponent } from '../../../modal/modal';
import { PaginationComponent } from '../../../pagination/pagination';
import { HttpError } from '../../../../interfaces/ErrorDto';
import { CategoriesService, CategoryStatsDto, CreateCategoryRequestDto, UpdateCategoryRequestDto } from '../../../../services/clients/categories.service';
import { ClientCategoryResponseDto } from '../../../../services/clients/categories.service';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { CustomToggleComponent } from "../../../toggle-slider/toggle-slider";

type ViewMode = 'list' | 'create' | 'edit' | 'view';

@Component({
  selector: 'app-client-categories-deleted',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, MatIcon, PaginationComponent, ReactiveFormsModule, CustomToggleComponent],
  templateUrl: './deleted-categories.html',
  styleUrls: ['./deleted-categories.scss'],
})
export class DeletedClientCategoriesComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  dataSource = new MatTableDataSource<ClientCategoryResponseDto>([]);

  stats: CategoryStatsDto | null = null;

  pageNumber = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 25, 50];
  totalCount = 0;

  viewMode: ViewMode = 'list';
  selectedCategory: ClientCategoryResponseDto | null = null;
  loading = false;
  errors: string[] = [];
  successMessage: string | null = null;
  searchQuery = '';

  readonly PRIVILEGES = PRIVILEGES;

  categoryForm: FormGroup;

  sortColumn: string = '';
  sortDirection: 'asc' | 'desc' = 'asc';

  constructor(
    public authService: AuthService,
    private categoriesService: CategoriesService,
    private fb: FormBuilder,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef
  ) {
    this.categoryForm = this.fb.group({
      name:                  ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      code:                  ['', [Validators.required, Validators.minLength(2), Validators.maxLength(20)]],
      delaiRetour:           [null, [Validators.required, Validators.min(0)]],
      discountRate:          [null, [Validators.min(0), Validators.max(100)]],
      creditLimitMultiplier: [null, [Validators.min(0)]],
      useBulkPricing:        [false]
    });
  }

  ngOnInit(): void {
    this.dataSource.filterPredicate = (data, filter) =>
      this.flattenObject(data).includes(filter);
    this.reload();
  }

  // ── Stats ─────────────────────────────────────────────────────────────────

  get totalCategories():    number { return this.stats?.totalCategories    ?? 0; }
  get activeCategories():   number { return this.stats?.activeCategories   ?? 0; }
  get inactiveCategories(): number { return this.stats?.inactiveCategories ?? 0; }
  get deletedCategories():  number { return this.stats?.deletedCategories  ?? 0; }

  // ── Sorting ───────────────────────────────────────────────────────────────

  sortBy(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
  }

  get sortedData(): ClientCategoryResponseDto[] {
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
    this.categoriesService.getDeleted(this.pageNumber, this.pageSize).subscribe({
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
    this.categoriesService.getStats().subscribe({
      next: (res) => { this.stats = res; this.cdr.markForCheck(); },
      error: () => this.flash('error', 'Failed to load stats.'),
    });
  }

  reload(): void {
    this.load();
    this.loadStats();
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────

  openView(category: ClientCategoryResponseDto): void {
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
    const val = this.categoryForm.value;

    const dto: CreateCategoryRequestDto = {
        name:                  val.name,
        code:                  val.code,
        delaiRetour:           val.delaiRetour,
        useBulkPricing:        val.useBulkPricing ?? false,
        discountRate:          val.discountRate ?? null,
        creditLimitMultiplier: val.creditLimitMultiplier ?? null,
      };
      this.categoriesService.create(dto).subscribe({
        next: () => {
          this.reload();
          this.cancel();
          this.flash('success', `Category "${val.name}" created successfully.`);
        },
        error: (err) => this.flash('error', (err.error as HttpError)?.message ?? 'Failed to create category.'),
      });
  }

  restore(cat: ClientCategoryResponseDto): void {
    this.categoriesService.restore(cat.id).subscribe({
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

  trackById(_: number, c: ClientCategoryResponseDto): string { return c.id; }

  private flattenObject(obj: any): string {
    return Object.keys(obj)
      .map(key => {
        const value = obj[key];
        if (value && typeof value === 'object') return this.flattenObject(value);
        return value;
      })
      .join(' ')
      .toLowerCase();
  }

  private getNestedValue(obj: any, path: string): any {
    return path.split('.').reduce((acc, key) => acc?.[key], obj);
  }
}
