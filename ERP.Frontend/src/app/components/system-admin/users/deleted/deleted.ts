import { Component, DestroyRef, inject, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
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
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../../services/auth.service';
import { AuthUserGetResponseDto, PagedResultDto, UserStatsDto } from '../../../../interfaces/AuthDto';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ModalComponent } from '../../../modal/modal';
import { MatDialog } from '@angular/material/dialog';

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
    RouterLink
],
  templateUrl: './deleted.html',
  styleUrl: './deleted.scss',
})
export class DeletedUsersComponent implements OnInit {
  @ViewChild(MatSort) sort!: MatSort;
  private readonly destroyRef= inject(DestroyRef);

  stats: UserStatsDto | null= null;

  displayedColumns: string[] = [
    'fullName',
    'email',
    'role',
    'createdAt',
    'lastLoginAt',
    'actions',
  ];

  dataSource = new MatTableDataSource<AuthUserGetResponseDto>([]);

  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;

  isLoading = false;
  searchTerm = '';

  constructor(
    private snackBar: MatSnackBar,
    private authService: AuthService,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.loadUsers();
    this.loadStats();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.authService.getDeleted(this.pageNumber, this.pageSize).subscribe({
      next: (result: PagedResultDto<AuthUserGetResponseDto>) => {
        this.dataSource.data = result.items;

        this.totalCount = result.totalCount;

        this.dataSource.sort = this.sort;
        this.isLoading = false;

      },
      error: () => {
        this.isLoading = false;
        this.snackBar.open('Failed to load deactivated users.', 'Dismiss', { duration: 3000 });
      },
    });
  }

  loadStats(){
      this.authService.getStats().subscribe({
        next: (result) => this.stats = result,
        error: () =>{
          this.isLoading = false;
          this.snackBar.open('Failed to load users.', 'Dismiss', { duration: 3000 });
      }
    });
  }

  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize); }
  prevPage(): void { if (this.pageNumber > 1) { this.pageNumber--; this.loadUsers(); } }
  nextPage(): void { if (this.pageNumber < this.totalPages) { this.pageNumber++; this.loadUsers(); } }


  applyFilter(): void {
    this.dataSource.filter = this.searchTerm.trim().toLowerCase();
  }

  recover(user: AuthUserGetResponseDto): void {

    this.authService.recover(user.id).subscribe({
      next: () => {
        const dialogRef = this.dialog.open(ModalComponent, {
          width: '400px',
          data: {
            title: 'User recovered successfully',
            message: `${user.fullName ?? user.login} is recovered but still deactivated. You can activate it later.`,
            confirmText: 'Ok',
            showCancel: false,
            icon: 'settings_backup_restore',
            iconColor: 'success'
          }
        });

        dialogRef.afterClosed()
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe(restul => this.reload());
      },
      error: () =>
        this.snackBar.open('Failed to reactivate user.', 'Dismiss', { duration: 3000 }),
    });
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

  private reload() {
    this.loadUsers();
    this.loadStats();
  }
}
