import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, FullProfile } from '../../services/auth.service';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UsersService } from '../../services/users.service';
import { forkJoin } from 'rxjs';

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


  userProfile: FullProfile | null=null;

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
    if (this.authService.isLoggedIn()!) {
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

        forkJoin({
                authUser: this.authService.getMe(),
                profile: this.userService.getMe(),
              }).subscribe({
          next: ({ authUser, profile }) => {
            this.userProfile = {
              ...profile,
              mustChangePassword: authUser.mustChangePassword,
              lastLoginAt: authUser.lastLoginAt
            };
            this.authService.setUserProfile(this.userProfile);

            if (response.mustChangePassword) {
              this.isLoading= false;
              this.router.navigate(['/must-change-password']);
              return;
            }

            if (!this.userProfile?.isProfileCompleted) {
              this.router.navigate(['/complete-profile']);
              return;
            }
            this.router.navigate(['/home']);

          },
          error: () => {
            this.authService.logout();
          }
        });
      },
      error: (error) => {
        this.isLoading = false;
        if (error.status === 0) return;
        this.snackBar.open('Failed to login. Unexpected error', 'Dismiss', { duration: 3000 });
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
