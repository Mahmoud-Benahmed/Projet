import { ModalComponent } from './../../../modal/modal';
import { HttpError } from './../../../../interfaces/ErrorDto';
import { AdminChangeProfileRequest } from './../../../../interfaces/AuthDto';
import { AuthService } from './../../../../services/auth.service';
import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { generatePassword, checkPassword } from '../../../../util/PasswordUtil';
import { MatDialog } from '@angular/material/dialog';
import { MatDialogModule } from '@angular/material/dialog';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDialogModule,
  ],
  templateUrl: './change-password.html',
  styleUrl: './change-password.scss',
})
export class ChangePasswordComponent implements OnInit {
  @ViewChild('passwordFormRef') passwordFormRef!: NgForm;

  // ── Route context ─────────────────────────────────────────────────────────
  targetUserId: string | null = null;

  // ── UI state ──────────────────────────────────────────────────────────────
  isLoading = false;
  showNewPassword = false;
  error: string | null = null;

  // ── Password validation ───────────────────────────────────────────────────
  passwordErrors: string[] = [];
  passwordScore = 0;
  passwordStrength = '';

  // ── Form ──────────────────────────────────────────────────────────────────
  adminForm: AdminChangeProfileRequest = { newPassword: '' };

  constructor(
    private authService: AuthService,
    public router: Router,
    private route: ActivatedRoute,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.targetUserId = this.route.snapshot.paramMap.get('authUserId');

    // Guard: must have privilege and a valid target that is not self
    if (!this.targetUserId || this.targetUserId === this.authService.UserId) {
      this.router.navigate(['/change-password']);
    }
  }


  // ── Submission ────────────────────────────────────────────────────────────

  onSubmit(): void {
    if (this.passwordFormRef.invalid || this.passwordErrors.length > 0) return;
    this.isLoading = true;

    this.authService
      .adminChangePassword(this.targetUserId!, this.adminForm)
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.cdr.markForCheck();
          this.dialog
            .open(ModalComponent, {
              width: '400px',
              data: {
                title: 'Password Reset',
                message:
                  'The password has been reset. The user will be required to change it on next login.',
                confirmText: 'Got it',
                showCancel: false,
                icon: 'check_circle',
                iconColor: 'success',
              },
            })
            .afterClosed()
            .subscribe(() => {
              this.router.navigate(['/users', this.targetUserId]);
            });
        },
        error: (err) => {
          this.isLoading = false;
          const error = err.error as HttpError;
          this.flash(error?.message ?? 'Failed to reset password.');
          this.cdr.markForCheck();
        },
      });
  }

  // ── Password validation ───────────────────────────────────────────────────

  // Admin flow: no current password context — pass null
  onPasswordChange(): void {
    const result = checkPassword(this.adminForm.newPassword, null);
    this.passwordErrors   = result.errors;
    this.passwordScore    = result.score;
    this.passwordStrength = result.strength;
  }

  // ── Generate ──────────────────────────────────────────────────────────────

  generatePassword(): void {
    this.adminForm.newPassword = generatePassword();
    if (!this.showNewPassword) this.showNewPassword = true;
    this.onPasswordChange();
  }

  // ── Strength meter ────────────────────────────────────────────────────────

  getScore(): number {
    const map: Record<string, number> = {
      'weak': 1, 'fair': 2, 'strong': 3, 'very strong': 4,
    };
    return map[this.passwordStrength] ?? 0;
  }

  getStrengthClass(): string {
    const map: Record<string, string> = {
      'weak':        'strength--weak',
      'fair':        'strength--fair',
      'strong':      'strength--strong',
      'very strong': 'strength--very-strong',
    };
    return map[this.passwordStrength] ?? '';
  }

  getStrengthLabel(): string {
    const map: Record<string, string> = {
      'weak': 'Weak', 'fair': 'Fair', 'strong': 'Strong', 'very strong': 'Very Strong',
    };
    return map[this.passwordStrength] ?? '';
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  dismissError(): void { this.error = null; }

  private flash(msg: string): void {
    this.error = msg;
    this.cdr.markForCheck();
    setTimeout(() => { this.error = null; this.cdr.markForCheck(); }, 4000);
  }
}
