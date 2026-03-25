import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule, registerLocaleData } from '@angular/common';
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
import { AuthService, PRIVILEGES } from '../../../../services/auth/auth.service';
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
  templateUrl: './deactivated.html',
  styleUrl: './deactivated.scss',
})
export class DeactivatedComponent implements OnInit {
  @ViewChild(MatSort) sort!: MatSort;

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
  pageSizeOptions = [5, 10, 25, 50];

  sortColumn: string = '';
  sortDirection: 'asc' | 'desc' = 'asc';


  isLoading = false;
  searchTerm = '';
  error: string | null = null;
  successMessage: string | null = null;

  readonly PRIVILEGES= PRIVILEGES;

  constructor(
    public authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.reload()
  }

  loadUsers(): void {
    this.isLoading = true;
    this.authService.getDeactivatedUsers(this.pageNumber, this.pageSize).subscribe({
      next: (result: PagedResultDto<AuthUserGetResponseDto>) => {
        this.dataSource.data = result.items;

        this.totalCount = result.totalCount;
        this.dataSource.sort = this.sort;
        this.isLoading = false;
        this.loadStats();

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

  sortBy(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
  }

  get sortedData() {
    const data = [...this.dataSource.filteredData];
    if (!this.sortColumn) return data;

    return data.sort((a, b) => {
      let valA = (a as any)[this.sortColumn];
      let valB = (b as any)[this.sortColumn];

      if (valA == null) return 1;
      if (valB == null) return -1;

      if (typeof valA === 'string') valA = valA.toLowerCase();
      if (typeof valB === 'string') valB = valB.toLowerCase();

      return (valA < valB ? -1 : valA > valB ? 1 : 0) * (this.sortDirection === 'asc' ? 1 : -1);
    });
  }


  applyFilter(): void {
    this.dataSource.filter = this.searchTerm.trim().toLowerCase();
  }

  activateUser(user: AuthUserGetResponseDto): void {
    this.authService.activate(user.id).subscribe({
      next: () => {
        this.flash('success',`User "${user.fullName}" has been reactivated.`);
        this.reload();
      },
      error: () =>
        this.flash('error', `Failed to reactivate user "${user.fullName}".`),
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

  onPageSizeChange(): void {
    this.pageNumber = 1; // reset to first page on size change
    this.reload();
  }

  reload():void{
    this.loadStats();
    this.loadUsers();
    this.cdr.markForCheck();
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
}
