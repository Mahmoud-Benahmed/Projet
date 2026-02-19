import { Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-login',
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent implements OnDestroy {

  credentials = { email: '', password: '' };
  errorMessage = '';
  showPassword = false;
  private errorTimeout: any = null;

  constructor(private router: Router, private authService: AuthService) {}

  ngOnInit(): void {
    if(this.authService.isLoggedIn()) {
      const role = this.authService.getRole();
      if (role === 'SystemAdmin') {
        this.router.navigate(['/register']);
      } else {
        this.router.navigate(['/home']);
      }
    }
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
      this.authService.login(this.credentials).subscribe({
        next: (response) => {
          localStorage.setItem('accessToken', response.accessToken);
          localStorage.setItem('refreshToken', response.refreshToken);
          localStorage.setItem('expiresAt', response.expiresAt);
          const role = this.authService.getRole();
          if (role === 'SystemAdmin') {
            this.router.navigate(['/register']);
          } else {
            this.router.navigate(['/home']);
          }

        },
        error: (error) => {
          this.showErrorMsg(error.error?.message || 'Login failed. Please check your credentials.');
          console.log(error.error?.message || error);

        }
      });
  }

  goToSignup(): void {
    this.router.navigate(['/register']);
  }

  showErrorMsg(message: string): void {
    if (this.errorTimeout) clearTimeout(this.errorTimeout);
    this.errorMessage = message;
    this.errorTimeout = setTimeout(() => {
      this.errorMessage = '';
      this.errorTimeout = null;
    }, 3000);
  }

  ngOnDestroy(): void {
    if (this.errorTimeout) clearTimeout(this.errorTimeout);
  }
}
