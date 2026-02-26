import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AuthResponse } from '../../interfaces/AuthDto';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UsersService } from '../../services/users.service';

@Component({
  selector: 'app-login',
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDialogModule
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent implements OnInit, OnDestroy {

  credentials = { login: '', password: '' };
  showPassword = false;
  isLoading = false;
  private errorTimeout: any = null;

  constructor(
    private router: Router,
    private authService: AuthService,
    private userService: UsersService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    if (this.authService.isLoggedIn!) {
      this.router.navigate(['/home']);
    }
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    this.isLoading = true;
    this.authService.login(this.credentials).subscribe({
      next: (response) => {
        this.authService.storeTokens(response); // storeTokens already saves mustChangePassword if you update it

        if (response.mustChangePassword) {
          this.isLoading= false;
          this.router.navigate(['/must-change-password']);
          return;
        }

        const authUserId= this.authService.UserId!;
        this.userService.getByAuthUserId(authUserId).subscribe({
          next: (profile)=> {
            this.isLoading = false;
            if (!profile.isProfileCompleted) {
              this.router.navigate(['/complete-profile']);
              return;
            }
            const role = this.authService.Role!;
            this.router.navigate(['/home']);
          },
          error: () => {
            this.isLoading = false;
            // profile not found, redirect to complete profile
            this.router.navigate(['/complete-profile']);
        }
      });

        const role = this.authService.Role!;
        this.router.navigate([role === 'SystemAdmin' ? '/users' : '/home']);
      },
      error: (error) => {
        this.isLoading = false;
        if (error.status === 0) return;
        this.snackBar.open('Failed to login, please check your credentials.', 'Dismiss', { duration: 3000 });
      }
    });
  }

  goToSignup(): void {
    this.router.navigate(['/register']);
  }

  ngOnDestroy(): void {
    if (this.errorTimeout) clearTimeout(this.errorTimeout);
  }
}
