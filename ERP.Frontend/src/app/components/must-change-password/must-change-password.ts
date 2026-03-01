import { Component, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../services/auth.service';
import { NotSameAsDirective } from '../../util/NotSameAsDirective';
import { generatePassword, checkPassword } from '../../util/PasswordUtil';
import { SameAsDirective } from '../../util/SameAsDirective';
import { ChangePasswordRequestDto } from '../../interfaces/AuthDto';

@Component({
  selector: 'app-must-change-password',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    NotSameAsDirective,
    SameAsDirective
  ],
  templateUrl: './must-change-password.html',
  styleUrl: './must-change-password.scss',
})
export class MustChangePasswordComponent {
  @ViewChild('passwordFormRef') passwordFormRef!: NgForm;

  isLoading = false;
  showCurrentPassword = false;
  showNewPassword = false;

  passwordErrors: string[] = [];
  passwordScore: number = 0;
  passwordStrength: string = '';

  passwordForm: ChangePasswordRequestDto = {
    currentPassword: '',
    newPassword: '',
  };

  constructor(
    private authService: AuthService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {}

  onSubmit(form: NgForm): void {
    if (form.invalid) return;

    this.isLoading = true;
    this.authService.changePassword(this.passwordForm).subscribe({
      next: () => {
        this.isLoading = false;
        this.authService.clearMustChangePassword();
        this.snackBar.open('Password changed successfully!', 'OK', { duration: 3000 });
        this.router.navigate(['/complete-profile']);
      },
      error: (err) => {
        this.isLoading = false;
        const message = err.error?.message || 'Failed to change password. Please try again.';
        this.snackBar.open(message, 'Dismiss', { duration: 4000 });
      },
    });
  }

  logout(): void {
    this.authService.logout();
  }


  onPasswordChange(): void {
    const result = checkPassword(this.passwordForm.newPassword);
    this.passwordErrors = result.errors;
    this.passwordScore = result.score;
    this.passwordStrength = result.strength;

    // revalidate currentPassword when newPassword changes
    const currentPwdControl = this.passwordFormRef?.controls?.['currentPassword'];
    if (currentPwdControl) {
      currentPwdControl.updateValueAndValidity();
    }
  }

  onCurrentPasswordChange(): void {
    const newPwdControl = this.passwordFormRef?.controls?.['newPassword'];
    if (newPwdControl) {
      newPwdControl.updateValueAndValidity();
    }
  }

  generatePassword(): void {
    this.passwordForm.newPassword = generatePassword();
    if (!this.showNewPassword) this.showNewPassword = true;
    this.onPasswordChange();
  }

  getScore(): number {
    const map: Record<string, number> = {
      'weak': 1, 'fair': 2, 'strong': 3, 'very strong': 4,
    };
    return map[this.passwordStrength] ?? 0;
  }

  getStrengthClass(): string {
    const map: Record<string, string> = {
      'weak': 'strength--weak',
      'fair': 'strength--fair',
      'strong': 'strength--strong',
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
}
