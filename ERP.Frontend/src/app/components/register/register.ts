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
import { MatSelectModule } from '@angular/material/select';

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
    MatSelectModule
  ],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class RegisterComponent implements OnDestroy {

  credentials = { email: '', password: '', role: '' };
  errorMessage = '';
  successMessage = '';
  showPassword = false;
  private errorTimeout: any = null;

  roles = [
    { value: 'Accountant', label: 'Accountant' },
    { value: 'SalesManager', label: 'Sales Manager' },
    { value: 'StockManager', label: 'Stock Manager' },
    { value: 'SystemAdmin', label: 'System Admin' }
  ];

  constructor(private router: Router, private authService: AuthService) {}

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    this.authService.register(this.credentials).subscribe({
      next: () => {
        this.showSuccessMsg('Account created successfully! Redirecting to home...');
        setTimeout(() => this.router.navigate(['/home']), 2000);
      },
      error: (error) => {
        const errors = error.error?.errors;
        if (errors) {
          // flatten all error messages from all fields
          this.showErrorMsg(
            Object.values(errors)
              .flat()
              .join('\n')
          );
        } else {
          this.showErrorMsg(error.error?.message || error.error?.title || 'Registration failed.');
        }
      }
    });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  showErrorMsg(message: string): void {
    if (this.errorTimeout) clearTimeout(this.errorTimeout);
    this.errorMessage = message;
    this.successMessage = '';
    this.errorTimeout = setTimeout(() => {
      this.errorMessage = '';
      this.errorTimeout = null;
    }, 3000);
  }

  showSuccessMsg(message: string): void {
    this.successMessage = message;
    this.errorMessage = '';
  }

  ngOnDestroy(): void {
    if (this.errorTimeout) clearTimeout(this.errorTimeout);
  }

  generatePassword(): void {
    const chars = {
      upper: 'ABCDEFGHIJKLMNOPQRSTUVWXYZ',
      lower: 'abcdefghijklmnopqrstuvwxyz',
      numbers: '0123456789',
      symbols: '!@#$%^&*'
    };

    const allChars = chars.upper + chars.lower + chars.numbers + chars.symbols;

    // random length between 8 and 20
    const length = Math.floor(Math.random() * (20 - 8 + 1)) + 8;

    // guarantee at least one of each type
    const password = [
      chars.upper[Math.floor(Math.random() * chars.upper.length)],
      chars.lower[Math.floor(Math.random() * chars.lower.length)],
      chars.numbers[Math.floor(Math.random() * chars.numbers.length)],
      chars.symbols[Math.floor(Math.random() * chars.symbols.length)],
      // fill remaining characters randomly
      ...Array.from({ length: length - 4 }, () => allChars[Math.floor(Math.random() * allChars.length)])
    ]
    // shuffle so the guaranteed chars aren't always at the start
    .sort(() => Math.random() - 0.5)
    .join('');

    this.credentials.password = password;
    this.showPassword = true; // show password so admin can see/copy it
  }
}
