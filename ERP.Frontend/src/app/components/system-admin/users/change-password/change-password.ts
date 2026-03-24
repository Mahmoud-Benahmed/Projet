import { ModalComponent } from './../../../modal/modal';
import { HttpError } from './../../../../interfaces/ErrorDto';
import { AdminChangeProfileRequest } from './../../../../interfaces/AuthDto';
import { AuthService } from '../../../../services/auth/auth.service';
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
  errors: string[] = [];

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
        error: (error) => {
          const err= error.error as HttpError;
          if (err.code === 'VALIDATION_ERROR' && err.errors) {
            // Flatten all field error arrays into a single list
            const messages = Object.values(err.errors).flat();
            this.flashErrors(messages);
          } else {
            this.flash(err.message);
          }
        }
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

  dismissError(): void { this.errors = []; }

  flash(msg: string): void {
    this.errors = [msg];
    setTimeout(() => { this.dismissError(); this.cdr.markForCheck(); }, 4000);
  }

  flashErrors(messages: string[]): void {
    this.errors = messages;
    setTimeout(() => { this.errors = []; this.cdr.markForCheck(); }, 4000);
    this.cdr.markForCheck();
  }
}
