import { Component, DestroyRef, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ModalComponent } from '../../modal/modal';
import { PaginationComponent } from '../../pagination/pagination';
import { AuthService, PRIVILEGES } from '../../../services/auth/auth.service';
import { ClientResponseDto, ClientsService, ClientStatsDto } from '../../../services/clients/clients.service';
import { MatTableDataSource } from '@angular/material/table';
import { CurrencyConfigService } from '../../../services/currency-config.service';


@Component({
  selector: 'app-deleted-clients',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    PaginationComponent
  ],
  templateUrl: './deleted.html',
  styleUrl: './deleted.scss',
})
export class DeletedClientsComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  dataSource = new MatTableDataSource<ClientResponseDto>([]);
  stats: ClientStatsDto | null = null;

  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 25, 50];

  isLoading = false;
  searchQuery = '';
  sortColumn: string = '';
  sortDirection: 'asc' | 'desc' = 'asc';
  error: string | null = null;
  successMessage: string | null = null;

  selectedClient: ClientResponseDto | null = null;
  viewMode:any = 'list';

  readonly PRIVILEGES= PRIVILEGES;

  constructor(
    public authService: AuthService,
    private clientsService: ClientsService,
    private dialog: MatDialog,
    private currencyConfig: CurrencyConfigService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.dataSource.filterPredicate = (data, filter) =>
      this.flattenObject(data).includes(filter);
    this.reload();
  }

  // -------------------------------------------------------
  // Load
  // -------------------------------------------------------
  load(): void {
    this.isLoading = true;
    this.clientsService.getDeleted(this.pageNumber, this.pageSize).subscribe({
      next: (result) => {
        this.dataSource.data   = result.items;
        this.totalCount = result.totalCount;
        this.isLoading  = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.flash('error','Failed to load deleted clients.');
      },
    });
  }

  loadStats(): void {
    this.clientsService.getStats().subscribe({
      next: (res) => { this.stats = res; this.cdr.markForCheck(); },
      error: () => this.flash('error', 'Failed to load stats.'),
    });
  }

  reload(): void {
    this.load();
    this.loadStats();
    this.cdr.markForCheck();
  }

  get totalClients():   number { return this.stats?.totalClients   ?? 0; }
  get activeClients():  number { return this.stats?.activeClients  ?? 0; }
  get blockedClients(): number { return this.stats?.blockedClients ?? 0; }
  get deletedClients(): number { return this.stats?.deletedClients ?? 0; }

    get currencyCode():   string { return this.currencyConfig.code;   }
  get currencyLocale(): string { return this.currencyConfig.locale; }

  getCategoryNames(client: ClientResponseDto): string {
    return this.clientsService.getCategoryNames(client);
  }

  // -------------------------------------------------------
  // Search
  // -------------------------------------------------------
  sortBy(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
  }

  get sortedData(): ClientResponseDto[] {
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
  // -------------------------------------------------------
  // Pagination
  // -------------------------------------------------------
  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize); }

  onPageSizeChange(): void {
    this.pageNumber = 1;
    this.reload();
  }

  // -------------------------------------------------------
  // Actions
  // -------------------------------------------------------
  openView(client: ClientResponseDto): void {
    this.selectedClient = client;
    this.viewMode = 'view';
    this.cdr.markForCheck();
  }

  cancel(): void {
    this.selectedClient = null;
    this.viewMode = 'list';
  }

  restore(client: ClientResponseDto): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '400px',
      data: {
        title: 'Restore Client',
        message: `Restore client "${client.name}"? It will reappear in the active clients list.`,
        confirmText: 'Restore',
        showCancel: true,
        icon: 'settings_backup_restore',
        iconColor: 'success'
      }
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        if (!result) return;
        this.clientsService.restore(client.id).subscribe({
          next: () => {
            if(this.viewMode==='view'){
              this.cancel();
            }
            this.flash('success', `Client ${client.name} has been restored. You can find it in the Clients page.`);
             this.reload();
          },
          error: () => this.flash('error', 'Failed to restore client.'),
        });
      });
  }

  // -------------------------------------------------------
  // Helpers
  // -------------------------------------------------------
  trackById(_index: number, c: ClientResponseDto): string { return c.id; }
  flash(type: 'success' | 'error', msg: string): void {
    if(type === 'success'){
      this.successMessage = msg; setTimeout(() => (this.successMessage = null), 3000);
    }
    else{
      this.error = msg; setTimeout(() => (this.error = null), 3000);
    }
    this.cdr.markForCheck();
  }
  dismissError(): void { this.error = null; }

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
