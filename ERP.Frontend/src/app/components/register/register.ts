import { ChangeDetectorRef, Component, OnDestroy } from '@angular/core';
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
import { RegisterRequest } from '../../interfaces/AuthDto';

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

  roles = [
    { value: 'Accountant', label: 'Accountant' },
    { value: 'SalesManager', label: 'Sales Manager' },
    { value: 'StockManager', label: 'Stock Manager' },
    { value: 'SystemAdmin', label: 'System Admin' }
  ];

  constructor(private router: Router,
              private authService: AuthService,
              private cdr: ChangeDetectorRef,
              private dialog: MatDialog,) {}

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    this.isLoading = true;
    this.authService.register(this.credentials).subscribe({
      next: () => {
        this.isLoading = false;
        this.cdr.detectChanges(); // ← add this

        setTimeout(() => this.router.navigate(['/home']), 2000);
      },
      error: (error) =>
            {
              this.isLoading = false;
              this.cdr.detectChanges(); // ← add this

              // skip if already handled by interceptor
              if (error.status === 0) return;
              console.log(error);


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
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  ngOnDestroy(): void {
    if (this.errorTimeout) clearTimeout(this.errorTimeout);
  }
}
