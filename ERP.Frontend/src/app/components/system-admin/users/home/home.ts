import { Component, OnInit, ViewChild } from '@angular/core';
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
import { UsersService } from '../../../../services/users.service';
import { UserProfileResponseDto, PagedResultDto } from '../../../../interfaces/UserProfileDto';
import { Router } from '@angular/router';
import { AuthService } from '../../../../services/auth.service';
import { Stats } from '../stats/stats';

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
    Stats
  ],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class UsersHomeComponent implements OnInit {
  @ViewChild(MatSort) sort!: MatSort;

  displayedColumns: string[] = [
    'fullName',
    'email',
    'phone',
    'isProfileCompleted',
    'createdAt',
    'actions',
  ];

  dataSource = new MatTableDataSource<UserProfileResponseDto>([]);

  // Pagination
  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;

  // State
  isLoading = false;
  searchTerm = '';

  constructor(
    private userProfileService: UsersService,
    private snackBar: MatSnackBar,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.userProfileService
      .getActiveUsers(this.pageNumber, this.pageSize)
      .subscribe({
        next: (result: PagedResultDto<UserProfileResponseDto>) => {
          this.dataSource.data = result.items;
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



  onPageChange(event: PageEvent): void {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadUsers();
  }

  applyFilter(): void {
    this.dataSource.filter = this.searchTerm.trim().toLowerCase();
  }

  deactivateUser(user: UserProfileResponseDto): void {
    this.userProfileService.deactivate(user.id).subscribe({
      next: () => {
        this.snackBar.open(`${user.fullName ?? user.email} deactivated.`, 'OK', {
          duration: 3000,
        });
        this.loadUsers();
      },
      error: () =>
        this.snackBar.open('Failed to deactivate user.', 'Dismiss', {
          duration: 3000,
        }),
    });
  }

  deleteUser(user: UserProfileResponseDto): void {
    this.userProfileService.delete(user.id).subscribe({
      next: () => {
        this.snackBar.open(`User deleted.`, 'OK', { duration: 3000 });
        this.loadUsers();
      },
      error: () =>
        this.snackBar.open('Failed to delete user.', 'Dismiss', {
          duration: 3000,
        }),
    });
  }

  getInitials(user: UserProfileResponseDto): string {
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
    this.router.navigate(['/users', authUserId]);
  }

  goToRegister(): void{
    this.router.navigate(['/users/register'])
  }
}
