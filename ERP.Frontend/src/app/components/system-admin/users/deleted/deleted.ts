import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit, ViewChild } from '@angular/core';
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
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../../services/auth/auth.service';
import { AuthUserGetResponseDto, PagedResultDto, UserStatsDto } from '../../../../interfaces/AuthDto';
import { PaginationComponent } from "../../../pagination/pagination";

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
    RouterLink,
    PaginationComponent
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

  pageNumber = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 25, 50];
  totalCount: number =0;

  isLoading = false;
  searchTerm = '';
  error: string | null = null;
  successMessage: string | null = null;

  constructor(
    private cdr: ChangeDetectorRef,
    public authService: AuthService,
  ) {}

  ngOnInit(): void {
    this.reload();
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
        this.flash('error', 'Failed to load deactivated users.');
      },
    });
  }

  loadStats(){
      this.authService.getStats().subscribe({
        next: (result) => this.stats = result,
        error: () =>{
          this.isLoading = false;
          this.flash('error', 'Failed to load users.');
      }
    });
  }

  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize); }
  prevPage(): void { if (this.pageNumber > 1) { this.pageNumber--; this.loadUsers(); } }
  nextPage(): void { if (this.pageNumber < this.totalPages) { this.pageNumber++; this.loadUsers(); } }
  onPageSizeChange(): void {
    this.pageNumber = 1; // reset to first page on size change
    this.reload();
  }

  applyFilter(): void {
    this.dataSource.filter = this.searchTerm.trim().toLowerCase();
  }

  restore(user: AuthUserGetResponseDto): void {
    this.authService.restore(user.id).subscribe({
      next: () => {
        this.flash('success', `${user.fullName ?? user.login} is restored but still deactivated. You can activate it later.`);
        this.reload();
      },
      error: () =>
        this.flash('error', 'Failed to restore user.')
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

  dismissError(): void { this.error = null; }

  flash(type: 'success' | 'error', msg: string): void {
    if(type === 'success'){
      this.successMessage = msg;
      this.cdr.markForCheck();
      setTimeout(() => (this.successMessage = null), 3000);
    }
    else{
      this.error = msg;
      this.cdr.markForCheck();
      setTimeout(() => (this.error = null), 3000);
    }
  }

  public reload() {
    this.loadUsers();
    this.loadStats();
  }
}
