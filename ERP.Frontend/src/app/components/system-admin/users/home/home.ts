import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../../services/auth.service';
import { Stats } from '../stats/stats';
import { AuthUserGetResponseDto, PagedResultDto, UserStatsDto } from '../../../../interfaces/AuthDto';
import { MatDialog } from '@angular/material/dialog';
import { ModalComponent } from '../../../modal/modal';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-home',
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
    MatMenuModule,
    MatTooltipModule,
    MatBadgeModule,
    MatDividerModule,
    MatSnackBarModule,
    RouterLinkActive,
    RouterLink
  ],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class UsersHomeComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  @ViewChild(MatSort) sort!: MatSort;

  displayedColumns: string[] = [
    'fullName',
    'email',
    'role',
    'createdAt',
    'lastLoginAt',
    'actions',
  ];

  dataSource = new MatTableDataSource<AuthUserGetResponseDto>([]);

  // Pagination
  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;

  // State
  isLoading = false;
  searchTerm = '';

  pageTitle= 'Active Users';
  stats: UserStatsDto | null= null;

  constructor(
    private snackBar: MatSnackBar,
    private router: Router,
    public authService: AuthService,
    private cdr: ChangeDetectorRef,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.reload();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.authService.getActivatedUsers(this.pageNumber, this.pageSize)
      .subscribe({
        next: (result: PagedResultDto<AuthUserGetResponseDto>) => {
          this.dataSource.data = result.items.filter(u => u.id !== this.currentUserId);
          this.totalCount = result.totalCount;
          this.dataSource.sort = this.sort;
          this.isLoading = false;
        },
        error: () => {
          this.isLoading = false;
          this.snackBar.open('Failed to load users.', 'Dismiss', { duration: 3000 });
        },
      });
  }

  loadStats(){
      this.authService.getStats().subscribe({
        next: (result) => this.stats = result,
        error: () =>{
          this.snackBar.open('Failed to load users.', 'Dismiss', { duration: 3000 });
      }
    });
  }


  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize); }
  prevPage(): void { if (this.pageNumber > 1) { this.pageNumber--; this.reload(); } }
  nextPage(): void { if (this.pageNumber < this.totalPages) { this.pageNumber++; this.reload(); } }


  applyFilter(): void {
    this.dataSource.filter = this.searchTerm.trim().toLowerCase();
  }

  deactivateUser(user: AuthUserGetResponseDto): void {
    this.authService.deactivate(user.id).subscribe({
      next: () => {
        this.snackBar.open(`${user.fullName ?? user.login} deactivated.`, 'OK', { duration: 3000 });
        this.reload();
      },
      error: () => this.snackBar.open('Failed to deactivate user.', 'Dismiss', { duration: 3000 })
    });
  }

  softDeleteUser(user: AuthUserGetResponseDto): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '400px',
      data: {
        title: 'Delete User',
        message: `${user.fullName ?? user.login} will be moved to deleted users. You can restore them later.`,
        confirmText: 'Delete',
        showCancel: true,
        icon: 'delete_outline',
        iconColor: 'danger'
      }
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        if (!result) return;

        this.authService.softDelete(user.id).subscribe({
          next: () => {
            this.snackBar.open(
              `${user.fullName ?? user.login} has been deleted.`,
              'OK',
              { duration: 3000 }
            );
            this.reload();
            this.cdr.markForCheck();
          },
          error: () => {
            this.snackBar.open('Failed to delete user.', 'Dismiss', { duration: 3000 });
          }
        });
      });
  }

  recoverUser(user: AuthUserGetResponseDto): void {
    this.authService.recover(user.id).subscribe({
      next: () => {
        this.snackBar.open(`${user.fullName ?? user.login} recovered.`, 'OK', { duration: 3000 });
        this.reload();
      },
      error: () => this.snackBar.open('Failed to recover user.', 'Dismiss', { duration: 3000 })
    });
  }

  deleteUser(user: AuthUserGetResponseDto): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '400px',
      data: {
        title: 'This action is irreversible',
        message: `${user.fullName} will be deleted permanently.
                    Once you confirm you cannot recover this user.
                    If you are not ure Cancel or temporarly move the user to Deleted users.
                    Are you sure you want to procceed ?`,
        confirmText: 'Delete',
        showCancel: true,
        icon: 'delete_outline',
        iconColor: 'danger'
      }
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        if (!result) return;

        this.authService.delete(user.id).subscribe({
          next: () => {
            this.snackBar.open(
              `${user.fullName ?? user.login} has been deleted.`,
              'OK',
              { duration: 3000 }
            );
            this.reload();
            this.cdr.markForCheck();
          },
          error: () => {
            this.snackBar.open('Failed to delete user.', 'Dismiss', { duration: 3000 });
          }
        });
      });
  }

  private reload() {
    this.loadUsers();
    this.loadStats();
  }
  getInitials(user: AuthUserGetResponseDto): string {
    if (user.fullName) {
      return user.fullName
        .split(' ')
        .map((n) => n[0])
        .slice(0, 2)
        .join('')
        .toUpperCase();
    }
    return user.email[0].toUpperCase();
  }

  goToProfile(authUserId: string): void {
    if (!authUserId) return;

    if (authUserId === this.authService.UserId) {
      this.router.navigate(['/profile']);
      return;
    }

    this.router.navigate(['/users', authUserId]);
  }

  goToRegister(): void{
    this.router.navigate(['/users/register'])
  }

  get currentUserId(): string | null {
    return this.authService.UserId;
  }

  get isOwnProfile(): boolean {
    return false; // in the table context, we use currentUserId comparison directly
  }

  get hasPrivilege(): boolean {
    return this.authService.Privileges.includes('ManageUsers');
  }

  get canDeactivate(): boolean {
    return this.hasPrivilege;
  }

  get canDelete(): boolean {
    return this.hasPrivilege;
  }
}
