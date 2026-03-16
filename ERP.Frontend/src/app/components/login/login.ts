import { AuthService } from '../../services/auth.service';
import { Component, OnInit, OnDestroy, ChangeDetectorRef, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthUserGetResponseDto } from '../../interfaces/AuthDto';
import { ModalComponent } from '../modal/modal';
import { HttpError } from '../../interfaces/ErrorDto';

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
  styleUrl: './login.scss',
  encapsulation: ViewEncapsulation.None
})
export class LoginComponent implements OnInit{

  userProfile: AuthUserGetResponseDto | null = null;

  credentials = { login: '', password: '' };
  showPassword = false;
  isLoading = false;

  constructor(
    private router: Router,
    private authService: AuthService,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    if (this.authService.isLoggedIn()!) {
      this.router.navigate(['/home']);
    }
    this.credentials={
      login: "admin_erp1234",
      password: "Admin@1234"
    }
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    this.isLoading = true;
    this.authService.login(this.credentials).subscribe({
      next: (response) => {
        this.authService.getMe().subscribe({
          next: (authUser) => {
            this.userProfile = authUser;
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
        if ([0, 403, 429].includes(error.status)) return;
        let err = error.error as HttpError
        this.dialog.open(ModalComponent, {
              width: '400px',
              data: {
                title: "Error",
                message: err.message,
                confirmText: 'Ok',
                showCancel: false,
                icon: 'check_circle',
                iconColor: 'danger'
              }
          });
      }
    });
  }

  goToSignup(): void {
    this.router.navigate(['/register']);
  }

  stopLoading() {
    this.isLoading = false;
    this.cdr.markForCheck();
  }
}
