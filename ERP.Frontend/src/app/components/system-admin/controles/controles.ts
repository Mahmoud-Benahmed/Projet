import { AuthService } from './../../../services/auth/auth.service';
import { ControleRequestDto } from './../../../services/auth/controle.service';
import { ControleService } from '../../../services/auth/controle.service';
import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatIcon } from '@angular/material/icon';
import { ModalComponent } from '../../modal/modal';
import { MatDialog } from '@angular/material/dialog';
import { HttpError } from '../../../interfaces/ErrorDto';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PaginationComponent } from '../../pagination/pagination';
import { ControleResponseDto, PagedResultDto } from '../../../interfaces/AuthDto';

type ViewMode = 'list' | 'create' | 'edit' | 'view';

@Component({
  selector: 'app-controle',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, MatIcon, PaginationComponent],
  templateUrl: './controles.html',
  styleUrls: ['./controles.scss'],
})
export class ControleComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  controles: ControleResponseDto[] = [];

  pageNumber = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 25, 50];
  totalCount: number =0;

  viewMode: ViewMode = 'list';
  selectedControle: ControleResponseDto | null = null;
  loading = false;
  errors: string[] = [];
  successMessage: string | null = null;
  searchQuery = '';

  controleForm: FormGroup;

  constructor(
    public authService: AuthService,
    private controleService: ControleService,
    private fb: FormBuilder,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef
  ) {
    this.controleForm = this.fb.group({
      category: ['', [Validators.required, Validators.minLength(2)]],
      libelle: ['', [Validators.required, Validators.minLength(2)]],
      description: ['', [Validators.required, Validators.minLength(5)]],
    });
  }

  ngOnInit(): void {
    this.reload();
  }

  // ── Pagination ────────────────────────────────────────────────────────────

    get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize); }
    prevPage(): void { if (this.pageNumber > 1) { this.pageNumber--; this.reload(); } }
    nextPage(): void { if (this.pageNumber < this.totalPages) { this.pageNumber++; this.reload(); } }
    onPageSizeChange(): void {
      this.pageNumber = 1;
      this.reload();
    }

  // ── Search / filter ───────────────────────────────────────────────────────

  get filteredControles(): ControleResponseDto[] {
    let result = this.controles;
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(c =>
        c.libelle.toLowerCase().includes(q) ||
        c.category.toLowerCase().includes(q) ||
        c.description.toLowerCase().includes(q)
      );
    }
    // ✅ Slice to current page
    const start = (this.pageNumber - 1) * this.pageSize;
    return result.slice(start, start + this.pageSize);
  }

  // ── Load ──────────────────────────────────────────────────────────────────

  load(): void {
    this.loading = true;
    this.errors = [];
    this.controleService.getAll().subscribe({
      next: (res: ControleResponseDto[]) => {
        this.controles = res;
        this.totalCount = res.length;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.flash('error', 'Failed to load controles.');
        this.loading = false;
      },
    });
  }

  reload(): void {
    this.load();
    this.cdr.markForCheck();
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────

  openCreate(): void {
    this.viewMode = 'create';
    this.selectedControle = null;
    this.controleForm.reset({ category: '', libelle: '', description: '' });
  }

  openEdit(controle: ControleResponseDto): void {
    this.viewMode = 'edit';
    this.selectedControle = controle;
    this.controleForm.patchValue({
      category: controle.category,
      libelle: controle.libelle,
      description: controle.description,
    });
    this.cdr.markForCheck();
  }

  openView(controle: ControleResponseDto): void {
    this.viewMode = 'view';
    this.selectedControle = controle;
    this.cdr.markForCheck();
  }

  cancel(): void {
    this.viewMode = 'list';
    this.selectedControle = null;
    this.controleForm.reset();
  }

  submit(): void {
    if (this.controleForm.invalid) return;
    const val = this.controleForm.value as ControleRequestDto;

    if (this.viewMode === 'create') {
      this.controleService.create(val).subscribe({
        next: () => {
          this.reload();
          this.cancel();
          this.flash('success', `Controle "${val.libelle}" created successfully.`);
        },
        error: (error) => {
          const err = error.error as HttpError;
          this.flash('error', err?.message ?? 'Failed to create controle.');
        },
      });
    } else if (this.viewMode === 'edit' && this.selectedControle) {
      this.controleService.update(this.selectedControle.id, val).subscribe({
        next: () => {
          this.cancel();
          this.reload();
          this.flash('success', `Controle "${val.libelle}" updated successfully.`);
        },
        error: (error) => {
          const err = error.error as HttpError;
          this.flash('error', err?.message ?? 'Failed to update controle.');
        },
      });
    }
  }

  delete(controle: ControleResponseDto): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '400px',
      data: {
        title: 'Delete Controle',
        message: `Controle "${controle.libelle}" will be permanently deleted. Do you want to proceed?`,
        confirmText: 'Delete',
        showCancel: true,
        icon: 'auto_delete',
        iconColor: 'danger',
      },
    });

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result) return;
        this.controleService.delete(controle.id).subscribe({
          next: () => {
            if (this.viewMode === 'view') this.cancel();
            this.flash('success', `Controle "${controle.libelle}" deleted successfully.`);
            this.reload();
          },
          error: () => this.flash('error', `Failed to delete controle "${controle.libelle}".`),
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

  dismissError(): void {
    this.errors = [];
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  trackById(_: number, c: ControleResponseDto): string {
    return c.id;
  }
}
