import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../../services/auth/auth.service';
import { NotSameAsDirective } from '../../../util/NotSameAsDirective';
import { generatePassword, checkPassword } from '../../../util/PasswordUtil';
import { SameAsDirective } from '../../../util/SameAsDirective';
import { ChangeProfilePasswordRequestDto } from '../../../interfaces/AuthDto';
import { MatDialog } from '@angular/material/dialog';
import { ModalComponent } from '../../modal/modal';
import { HttpError } from '../../../interfaces/ErrorDto';

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
    RouterLink
],
  templateUrl: './must-change-password.html',
  styleUrl: './must-change-password.scss',
})
export class MustChangePasswordComponent implements OnInit{
  @ViewChild('passwordFormRef') passwordFormRef!: NgForm;
  mustChangePassword: boolean = false;
  errors: string[] = [];
  successMessage: string | null = null;

  isLoading = false;
  showCurrentPassword = false;
  showNewPassword = false;

  passwordErrors: string[] = [];
  passwordScore: number = 0;
  passwordStrength: string = '';

  passwordForm: ChangeProfilePasswordRequestDto = {
    currentPassword: '',
    newPassword: '',
  };

  constructor(
    private authService: AuthService,
    private router: Router,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(){
    this.mustChangePassword = this.authService.getMustChangePassword();
  }

  onSubmit(): void {
    this.isLoading = true;
    this.authService.changeProfilePassword(this.passwordForm).subscribe({
     next: () => {
        this.isLoading = false;
        if (this.mustChangePassword) this.authService.clearMustChangePassword();
            this.dialog.open(ModalComponent, {
              width: '400px',
              data: {
                title: 'Password change',
                message: `Password has been changed successfully and you will use it on next login.`,
                confirmText: 'Understood',
                showCancel: false,
                icon: 'info',
                iconColor: 'success'
              }
            });
            this.router.navigate(['/profile']);
      },
      error: (error) => {
        this.isLoading = false;
        const err = error.error as HttpError;
        if (err.code === 'VALIDATION_ERROR' && err.errors) {
          const messages = Object.values(err.errors).flat();
          this.flashErrors(messages);
        } else {
          this.flash('error', err.message);
        }
        this.cdr.markForCheck();
      }
    });
  }

  logout(): void {
    this.authService.logout();
  }


  onPasswordChange(): void {
    const result = checkPassword(this.passwordForm.newPassword, this.passwordForm.currentPassword);
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
    this.onPasswordChange();

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

  dismissError(): void { this.errors = []; }

  flash(type: 'success' | 'error', msg: string): void {
    if(type === 'success'){
      this.successMessage = msg;
      this.cdr.markForCheck();
      setTimeout(() => { this.dismissError(); this.cdr.markForCheck(); }, 3000);
    }
    else{
      this.errors = [msg];
      this.cdr.markForCheck();
      setTimeout(() => { this.dismissError(); this.cdr.markForCheck(); }, 3000);
    }
  }

  flashErrors(messages: string[]): void {
    this.errors = messages;
    setTimeout(() => { this.errors = []; this.cdr.markForCheck(); }, 4000);
    this.cdr.markForCheck();
  }
}
