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
import { Stats } from "../stats/stats";
import { AuthService } from '../../../../services/auth.service';
import { AuthUserGetResponseDto, PagedResultDto } from '../../../../interfaces/AuthDto';

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
    Stats
],
  templateUrl: './deactivated.html',
  styleUrl: './deactivated.scss',
})
export class DeactivatedComponent implements OnInit {
  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(Stats) statsComponent!: Stats;


  displayedColumns: string[] = [
    'fullName',
    'email',
    'phone',
    'isProfileCompleted',
    'createdAt',
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
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.authService.getDeactivatedUsers(this.pageNumber, this.pageSize).subscribe({
      next: (result: PagedResultDto<AuthUserGetResponseDto>) => {
        this.dataSource.data = result.items.filter(u => u.id !== this.authService.UserId);
        console.log(this.dataSource.data);

        this.totalCount = result.totalCount - 1; // account for the filtered out user
        this.dataSource.sort = this.sort;
        this.isLoading = false;
        this.statsComponent.loadStats();
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

  activateUser(user: AuthUserGetResponseDto): void {
    this.authService.activate(user.id).subscribe({
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

  goToProfile(authUserId: string): void {
    this.router.navigate(['/users', authUserId]);
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
}
