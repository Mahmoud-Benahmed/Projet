import { AuthService, PRIVILEGES } from './../../../services/auth/auth.service';
import { ClientService, Client, CreateClientRequest, UpdateClientRequest, ClientStatsDto, ClientType } from '../../../services/client.service';
import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ModalComponent } from '../../modal/modal';
import { MatDialog } from '@angular/material/dialog';
import { HttpError } from '../../../interfaces/ErrorDto';
import { MatIcon } from "@angular/material/icon";
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink, RouterLinkActive } from "@angular/router";
import { PaginationComponent } from "../../pagination/pagination";
import { MatTableDataSource } from '@angular/material/table';

type ViewMode = 'list' | 'create' | 'edit' | 'view';

@Component({
  selector: 'app-client',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, MatIcon, RouterLink, RouterLinkActive, PaginationComponent],
  templateUrl: './home.html',
  styleUrls: ['./home.scss'],
})
export class ClientComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);


  dataSource = new MatTableDataSource<ClientResponseDto>([]);
  stats: ClientStatsDto | null = null;

  pageNumber = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 25, 50];
  totalCount = 0;

  viewMode: ViewMode = 'list';
  selectedClient: Client | null = null;
  loading = false;
  errors: string[] = [];
  successMessage: string | null = null;
  searchQuery = '';


  readonly clientTypes: ClientType[] = ['Individual', 'Company'];
  readonly PRIVILEGES= PRIVILEGES;

  clientForm: FormGroup;

  constructor(
    public authService: AuthService,
    private clientService: ClientService,
    private fb: FormBuilder,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef
  ) {
    this.clientForm = this.fb.group({
      type:       [null,  Validators.required],
      name:       ['',   [Validators.required, Validators.maxLength(200)]],
      email:      ['',   [Validators.required, Validators.email, Validators.maxLength(250)]],
      address:    ['',   [Validators.required, Validators.maxLength(500)]],
      phone:      [null,  Validators.maxLength(50)],
      taxNumber:  [null,  Validators.maxLength(100)],
    });
  }

  ngOnInit(): void {
    this.reload();
  }

  // -------------------------------------------------------
  // Load
  // -------------------------------------------------------
  load(): void {
    this.loading = true;
    this.errors = [];
    this.clientService.getAll(this.pageNumber, this.pageSize).subscribe({
      next: (res) => {
        this.clients = res.items;
        this.totalCount = res.totalCount;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => { this.flash('error','Failed to load clients.'); this.loading = false; this.cdr.markForCheck(); },
    });
  }

  loadStats(): void {
    this.clientService.getStats().subscribe({
      next: (res) => { this.stats = res; this.cdr.markForCheck(); },
      error: () => { this.flash('error', 'Failed to load stats.'); this.cdr.markForCheck(); },
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
  // CRUD
  // -------------------------------------------------------
  openCreate(): void {
    this.viewMode = 'create';
    this.selectedClient = null;
    this.clientForm.reset({ type: null, name: '', email: '', address: '', phone: null, taxNumber: null });
  }

  openEdit(client: Client): void {
    this.viewMode = 'edit';
    this.selectedClient = client;
    this.clientForm.patchValue({
      type:      client.type,
      name:      client.name,
      email:     client.email,
      address:   client.address,
      phone:     client.phone ?? null,
      taxNumber: client.taxNumber ?? null,
    });
    this.cdr.markForCheck();
  }

  openView(client: Client): void {
    this.viewMode = 'view';
    this.selectedClient = client;
    this.cdr.markForCheck();
  }

  cancel(): void {
    this.viewMode = 'list';
    this.selectedClient = null;
    this.clientForm.reset();
  }

  submit(): void {
    if (this.clientForm.invalid) return;
    const val = this.clientForm.value;

    if (this.viewMode === 'create') {
      this.clientService.create(val as CreateClientRequest).subscribe({
        next: () => {
          this.flash('success', `Client "${val.name}" created successfully.`);
          this.cancel();
          this.reload();
        },
        error: (error) => {
          const err= error.error as HttpError;
          if (err.code === 'VALIDATION_ERROR' && err.errors) {
            // Flatten all field error arrays into a single list
            const messages = Object.values(err.errors).flat();
            this.flashErrors(messages);
          } else {
            this.flash('error', err.message);
          }
        }
      });
    } else if (this.viewMode === 'edit' && this.selectedClient) {
      this.clientService.update(this.selectedClient.id, val as UpdateClientRequest).subscribe({
        next: () => {
          this.flash('success', `Client "${val.name}" has been updated successfully.`);
          this.cancel();
          this.reload();
        },
        error: (error) => {
          const err= error.error as HttpError;
          if (err.code === 'VALIDATION_ERROR' && err.errors) {
            // Flatten all field error arrays into a single list
            const messages = Object.values(err.errors).flat();
            this.flashErrors(messages);
          } else {
            this.flash('error', err.message);
          }
        }
      });
    }
  }

  delete(client: Client): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '400px',
      data: { title: 'Delete Client', message: `Client "${client.name}" will be deleted. Do you want to proceed?`, confirmText: 'Delete', showCancel: true, icon: 'auto_delete', iconColor: 'danger' }
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        if (!result) return;
        this.clientService.delete(client.id).subscribe({
          next: () => {
            if(this.viewMode==='view'){
              this.cancel();
            }
            this.flash('success', `Client "${client.name}" has been deleted successfully.`);
            this.reload();
          },
          error: () => {
            this.flash('error', `Failed to delete client "${client.name}".`);
          }
        });
      });
  }


  flash(type: 'success' | 'error', msg: string): void {
    if (type === 'success') {
      this.successMessage = msg;
      setTimeout(() => { this.successMessage = null; this.cdr.markForCheck(); }, 3000);
    } else {
      this.errors = [msg];
      setTimeout(() => { this.dismissError(); this.cdr.markForCheck(); }, 4000);
    }
    this.cdr.markForCheck();
  }

  flashErrors(messages: string[]): void {
    this.errors = messages;
    setTimeout(() => { this.errors = []; this.cdr.markForCheck(); }, 4000);
    this.cdr.markForCheck();
  }


  // -------------------------------------------------------
  // Helpers
  // -------------------------------------------------------
  dismissError(): void { this.errors = []; }
  dismissSuccess(): void { this.successMessage = null; }

  trackById(_: number, c: Client): string { return c.id; }
}
