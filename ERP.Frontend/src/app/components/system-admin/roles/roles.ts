import { RoleCreateDto, RoleService, RoleUpdateDto } from '../../../services/auth/roles.service';
import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatIcon } from '@angular/material/icon';
import { ModalComponent } from '../../modal/modal';
import { MatDialog } from '@angular/material/dialog';
import { HttpError } from '../../../interfaces/ErrorDto';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PaginationComponent } from '../../pagination/pagination';
import { AuthService } from '../../../services/auth/auth.service';
import { PagedResultDto, RoleResponseDto } from '../../../interfaces/AuthDto';

type ViewMode = 'list' | 'create' | 'edit' | 'view';

@Component({
  selector: 'app-role',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, MatIcon, PaginationComponent],
  templateUrl: './roles.html',
  styleUrls: ['./roles.scss'],
})
export class RoleComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  roles: RoleResponseDto[] = [];

  pageNumber = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 25, 50];
  totalCount = 0;

  viewMode: ViewMode = 'list';
  selectedRole: RoleResponseDto | null = null;
  loading = false;
  errors: string[] = [];
  successMessage: string | null = null;
  searchQuery = '';

  roleForm: FormGroup;

  constructor(
    public authService: AuthService,
    private roleService: RoleService,
    private fb: FormBuilder,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef
  ) {
    this.roleForm = this.fb.group({
      libelle: ['', [Validators.required, Validators.minLength(2)]],
    });
  }

  ngOnInit(): void {
    this.reload();
  }

  // ── Pagination ────────────────────────────────────────────────────────────

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  // ── Search ────────────────────────────────────────────────────────────────

  get filteredRoles(): RoleResponseDto[] {
    if (!this.searchQuery.trim()) return this.roles;
    const q = this.searchQuery.toLowerCase();
    return this.roles.filter((r) => r.libelle.toLowerCase().includes(q));
  }

  // ── Load ──────────────────────────────────────────────────────────────────

  load(): void {
    this.loading = true;
    this.errors = [];
    this.roleService.getAll().subscribe({
      next: (res: RoleResponseDto[]) => {
        this.roles = res;
        this.totalCount = res.length;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.flash('error', 'Failed to load roles.');
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
    this.selectedRole = null;
    this.roleForm.reset({ libelle: '' });
  }

  openEdit(role: RoleResponseDto): void {
    this.viewMode = 'edit';
    this.selectedRole = role;
    this.roleForm.patchValue({ libelle: role.libelle });
    this.cdr.markForCheck();
  }

  openView(role: RoleResponseDto): void {
    this.viewMode = 'view';
    this.selectedRole = role;
    this.cdr.markForCheck();
  }

  cancel(): void {
    this.viewMode = 'list';
    this.selectedRole = null;
    this.roleForm.reset();
  }

  submit(): void {
    if (this.roleForm.invalid) return;
    const val = this.roleForm.value;

    if (this.viewMode === 'create') {
      const dto: RoleCreateDto = { libelle: val.libelle };
      this.roleService.create(dto).subscribe({
        next: () => {
          this.reload();
          this.cancel();
          this.flash('success', `Role "${val.libelle}" created successfully.`);
        },
        error: (error) => {
          const err = error.error as HttpError;
          this.flash('error', err?.message ?? 'Failed to create role.');
        },
      });
    } else if (this.viewMode === 'edit' && this.selectedRole) {
      const dto: RoleUpdateDto = { libelle: val.libelle };
      this.roleService.update(this.selectedRole.id, dto).subscribe({
        next: () => {
          this.cancel();
          this.reload();
          this.flash('success', `Role "${val.libelle}" updated successfully.`);
        },
        error: (error) => {
          const err = error.error as HttpError;
          this.flash('error', err?.message ?? 'Failed to update role.');
        },
      });
    }
  }

  delete(role: RoleResponseDto): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '400px',
      data: {
        title: 'Delete Role',
        message: `Role "${role.libelle}" will be permanently deleted. Do you want to proceed?`,
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
        this.roleService.delete(role.id).subscribe({
          next: () => {
            if (this.viewMode === 'view') this.cancel();
            this.flash('success', `Role "${role.libelle}" deleted successfully.`);
            this.reload();
          },
          error: () => this.flash('error', `Failed to delete role "${role.libelle}".`),
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

  onPageSizeChange(): void {
    this.pageNumber = 1;
    this.reload();
  }

  trackById(_: number, r: RoleResponseDto): string {
    return r.id;
  }
}
