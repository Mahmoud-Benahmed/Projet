import { ChangeDetectorRef, Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule, formatNumber, KeyValuePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatChipsModule } from '@angular/material/chips';
import { AuditAction, AuditLogResponseDto, AuditLogService } from '../../../services/audit-log.service';
import { MatInput, MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [
    CommonModule, FormsModule, KeyValuePipe,
    MatTableModule, MatPaginatorModule, MatCardModule,
    MatButtonModule, MatIconModule, MatFormFieldModule,
    MatSelectModule, MatButtonToggleModule, MatProgressSpinnerModule,
    MatTooltipModule, MatDividerModule, MatSnackBarModule,
    MatDialogModule, MatChipsModule,MatInputModule, MatSelectModule, MatFormFieldModule
  ],
  templateUrl: './audit-log.html',
  styleUrl: './audit-log.scss',
})
export class AuditLogComponent implements OnInit {
  @ViewChild('detailDialog') detailDialog!: TemplateRef<any>;

  displayedColumns = ['status', 'action', 'performedBy', 'targetUserId', 'ipAddress', 'timestamp', 'details'];
  dataSource = new MatTableDataSource<AuditLogResponseDto>([]);

  totalCount = 0;
  pageNumber = 1;
  pageSize = 20;
  isLoading = false;

  selectedAction: AuditAction | null = null;
  selectedStatus: boolean | null = null;
  userIdFilter = '';

  allActions: AuditAction[] = [
    'Login', 'Logout', 'TokenRefreshed', 'TokenRevoked',
    'UserRegistered', 'PasswordChanged', 'PasswordChangedByAdmin',
    'ProfileUpdated', 'UserActivated', 'UserDeactivated',
  ];

  constructor(
    private authService: AuthService,
    private auditLogService: AuditLogService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.load();
    this.cdr.markForCheck();
  }

  load(): void {
    this.isLoading = true;

    const source$ = this.userIdFilter.trim()
      ? this.auditLogService.getByUser(this.userIdFilter.trim(), this.pageNumber, this.pageSize)
      : this.selectedAction
        ? this.auditLogService.getByAction(this.selectedAction, this.pageNumber, this.pageSize)
        : this.auditLogService.getAll(this.pageNumber, this.pageSize);

    source$.subscribe({
      next: (result) => {
        this.dataSource.data = this.selectedStatus !== null
          ? result.items.filter(l => l.success === this.selectedStatus)
          : result.items;
        this.totalCount = result.totalCount;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.snackBar.open('Failed to load audit logs.', 'Dismiss', { duration: 3000 });
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.load();
  }

  onFilterChange(): void {
    this.pageNumber = 1;
    this.load();
  }

  openDetail(log: AuditLogResponseDto): void {
    this.dialog.open(this.detailDialog, { width: '500px', data: log });
  }

  confirmClear(): void {
    if (!confirm('Clear all audit logs? This cannot be undone.')) return;
    this.auditLogService.clear().subscribe({
      next: () => {
        this.snackBar.open('Audit logs cleared.', 'OK', { duration: 3000 });
        this.load();
      },
      error: () => this.snackBar.open('Failed to clear logs.', 'Dismiss', { duration: 3000 })
    });
  }

  formatAction(action: AuditAction): string {
    return action.replace(/([A-Z])/g, ' $1').trim();
  }

  getActionIcon(action: AuditAction): string {
    const map: Record<AuditAction, string> = {
      Login: 'login', Logout: 'logout',
      TokenRefreshed: 'refresh', TokenRevoked: 'block',
      UserRegistered: 'person_add', PasswordChanged: 'lock_reset',
      PasswordChangedByAdmin: 'admin_panel_settings',
      ProfileUpdated: 'edit', UserActivated: 'check_circle',
      UserDeactivated: 'cancel',
    };
    return map[action] ?? 'circle';
  }

  getActionCategory(action: AuditAction): string {
    if (['Login', 'Logout'].includes(action))                           return 'auth';
    if (['TokenRefreshed', 'TokenRevoked'].includes(action))            return 'token';
    if (['PasswordChanged', 'PasswordChangedByAdmin'].includes(action)) return 'password';
    return 'user';
  }

  hasDetails(log: AuditLogResponseDto): boolean {
    return !!(log.metadata || log.failureReason || log.userAgent);
  }

  metadataEntries(log: AuditLogResponseDto): { key: string; value: string }[] {
    if (!log.metadata) return [];
    return Object.entries(log.metadata).map(([key, value]) => ({ key, value }));
  }

  goToProfile(authUserId: string): void {
    if (!authUserId) return;

    if (authUserId === this.authService.UserId) {
      this.router.navigate(['/profile']);
      return;
    }

    this.router.navigate(['/users', authUserId]);
  }
}
