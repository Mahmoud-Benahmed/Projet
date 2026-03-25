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
import { ClientService, Client, ClientStatsDto } from '../../../services/client.service';
import { PaginationComponent } from '../../pagination/pagination';
import { AuthService, PRIVILEGES } from '../../../services/auth/auth.service';
@Component({
  selector: 'app-deleted-clients',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    RouterLink,
    RouterLinkActive,
    PaginationComponent
  ],
  templateUrl: './deleted.html',
  styleUrl: './deleted.scss',
})
export class DeletedClientsComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  clients: Client[] = [];
  stats: ClientStatsDto | null = null;

  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 25, 50];

  isLoading = false;
  searchQuery = '';
  error: string | null = null;
  successMessage: string | null = null;

  selectedClient: Client | null = null;
  viewMode: 'list' | 'view' = 'list';

  readonly PRIVILEGES= PRIVILEGES;

  constructor(
    public authService: AuthService,
    private clientService: ClientService,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.reload();
  }

  // -------------------------------------------------------
  // Load
  // -------------------------------------------------------
  load(): void {
    this.isLoading = true;
    this.clientService.getDeleted(this.pageNumber, this.pageSize).subscribe({
      next: (result) => {
        this.clients   = result.items;
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
    this.clientService.getStats().subscribe({
      next: (result) => { this.stats = result; this.cdr.markForCheck(); },
      error: () => this.flash('error','Failed to load stats.'),
    });
  }

  reload(): void {
    this.load();
    this.loadStats();
    this.cdr.markForCheck();
  }

  // -------------------------------------------------------
  // Search
  // -------------------------------------------------------
  get filteredClients(): Client[] {
    if (!this.searchQuery.trim()) return this.clients;
    const q = this.searchQuery.toLowerCase();
    return this.clients.filter(c =>
      c.name.toLowerCase().includes(q)    ||
      c.email.toLowerCase().includes(q)   ||
      c.type.toLowerCase().includes(q)    ||
      c.address.toLowerCase().includes(q)
    );
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
  openView(client: Client): void {
    this.selectedClient = client;
    this.viewMode = 'view';
    this.cdr.markForCheck();
  }

  cancel(): void {
    this.selectedClient = null;
    this.viewMode = 'list';
  }

  restore(client: Client): void {
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
        this.clientService.restore(client.id).subscribe({
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
  trackById(_index: number, c: Client): string { return c.id; }
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
}
