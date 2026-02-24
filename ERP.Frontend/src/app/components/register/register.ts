import { ChangeDetectorRef, Component, HostBinding, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { ModalComponent } from '../modal/modal';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { RegisterRequest, RoleDto } from '../../interfaces/AuthDto';
import { generatePassword, checkPassword } from '../../util/PasswordUtil';

@HostBinding('class')
@Component({
  selector: 'app-register',
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatProgressSpinner
],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class RegisterComponent implements OnDestroy {

  credentials: RegisterRequest = { email: '', password: '', role: '' };
  errorMessage = '';
  successMessage = '';
  showPassword = false;
  private errorTimeout: any = null;
  isLoading:boolean = false;


  passwordErrors: string[] = [];
  passwordScore: number = 0;
  passwordStrength: string = '';

  constructor(private router: Router,
              private authService: AuthService,
              private cdr: ChangeDetectorRef,
              private dialog: MatDialog) {}

  get hostClass(): string {
    return this.passwordStrength
      ? this.getStrengthClass()
      : '';
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    this.isLoading = true;
    this.authService.register(this.credentials).subscribe({
      next: () => {
        this.isLoading = false;
          this.dialog.open(ModalComponent, {
            width: '400px',
            data: {
              title: 'Enregistrement réussi', // <-- success title
              message: "User has been registered successfully.",
              confirmText: 'OK',
              showCancel: false,
              icon: 'check_circle',
              iconColor: 'primary'
            }
          });
      },
      error: (error) =>
      {
          // skip if already handled by interceptor
          if (error.status === 0) return;
          this.dialog.open(ModalComponent, {
                width: '400px',
                data: {
                  title: 'Erreur d\'enregistrement',
                  message: error.error.message || 'L\'enregistrement a échoué. Veuillez vérifier vos informations.',
                  confirmText: 'OK',
                  showCancel: false,
                  icon: 'warning',
                  iconColor: 'warn'
                }
          });
      }
    });
  }

  generatePassword(){
    this.credentials.password= generatePassword();
    if(!this.showPassword) this.showPassword= true;
    this.onPasswordChange();
  }

  onPasswordChange(): void {
    const result = checkPassword(this.credentials.password);
    this.passwordErrors = result.errors;
    this.passwordScore = result.score;
    this.passwordStrength = result.strength;
  }

  getScore(): number {
    // map strength to 1-4 for the bars
    const map: Record<string, number> = {
      'weak': 1,
      'fair': 2,
      'strong': 3,
      'very strong': 4,
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
      'weak': 'Faible',
      'fair': 'Moyen',
      'strong': 'Fort',
      'very strong': 'Très fort',
    };
    return map[this.passwordStrength] ?? '';
  }

  get roles(): RoleDto[]{
    return this.authService.roles;
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  ngOnDestroy(): void {
    if (this.errorTimeout) clearTimeout(this.errorTimeout);
  }
}
