import { AuthService } from '../../services/auth.service';
import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { forkJoin } from 'rxjs';
import { AuthUserGetResponseDto } from '../../interfaces/AuthDto';

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


  userProfile: AuthUserGetResponseDto | null=null;

  credentials = { login: '', password: '' };
  showPassword = false;
  isLoading = false;
  private errorTimeout: any = null;

  constructor(
    private router: Router,
    private authService: AuthService,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
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

        this.authService.getMe().subscribe({
          next: (authUser) => {
            this.userProfile = authUser
            this.authService.setUserProfile(this.userProfile);

            if (response.mustChangePassword) {
              this.stopLoading();
              this.router.navigate(['/must-change-password']);
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
        this.stopLoading();
        if (error.status === 0) return;
        this.snackBar.open('Failed to login. Unexpected error', 'Dismiss', { duration: 3000 });
      }
    });
  }

  goToSignup(): void {
    this.router.navigate(['/register']);
  }

  stopLoading(){
    this.isLoading= false;
    this.cdr.markForCheck();
  }

  ngOnDestroy(): void {
    if (this.errorTimeout) clearTimeout(this.errorTimeout);
  }
}
