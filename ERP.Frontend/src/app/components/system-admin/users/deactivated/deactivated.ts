import { Component, OnInit, ViewChild } from '@angular/core';
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
import { Router } from '@angular/router';
import { UsersService } from '../../../../services/users.service';
import { UserProfileResponseDto, PagedResultDto } from '../../../../interfaces/UserProfileDto';

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
  ],
  templateUrl: './deactivated.html',
  styleUrl: './deactivated.scss',
})
export class DeactivatedComponent implements OnInit {
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

  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;

  isLoading = false;
  searchTerm = '';

  constructor(
    private usersService: UsersService,
    private snackBar: MatSnackBar,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.usersService.getDeactivatedUsers(this.pageNumber, this.pageSize).subscribe({
      next: (result: PagedResultDto<UserProfileResponseDto>) => {
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

  onPageChange(event: PageEvent): void {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadUsers();
  }

  applyFilter(): void {
    this.dataSource.filter = this.searchTerm.trim().toLowerCase();
  }

  activateUser(user: UserProfileResponseDto): void {
    this.usersService.activate(user.id).subscribe({
      next: () => {
        this.snackBar.open(
          `${user.fullName ?? user.email} reactivated.`, 'OK',
          { duration: 3000 }
        );
        this.loadUsers();
      },
      error: () =>
        this.snackBar.open('Failed to reactivate user.', 'Dismiss', { duration: 3000 }),
    });
  }

  deleteUser(user: UserProfileResponseDto): void {
    this.usersService.delete(user.id).subscribe({
      next: () => {
        this.snackBar.open('User deleted.', 'OK', { duration: 3000 });
        this.loadUsers();
      },
      error: () =>
        this.snackBar.open('Failed to delete user.', 'Dismiss', { duration: 3000 }),
    });
  }

  goToProfile(authUserId: string): void {
    this.router.navigate(['/users', authUserId]);
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
}
