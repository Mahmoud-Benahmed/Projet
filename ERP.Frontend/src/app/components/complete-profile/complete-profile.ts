import { Component } from '@angular/core';
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
import { AuthService, FullProfile } from '../../services/auth.service';
import { UsersService } from '../../services/users.service';
import { CompleteProfileDto } from '../../interfaces/UserProfileDto';

@Component({
  selector: 'app-complete-profile',
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
  ],
  templateUrl: './complete-profile.html',
  styleUrl: './complete-profile.scss',
})
export class CompleteProfileComponent {
  userProfile: FullProfile | null = null;
  isLoading = false;

  form: CompleteProfileDto = {
    fullName: '',
    phone: '',
  };

  constructor(
    private authService: AuthService,
    private usersService: UsersService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.userProfile= this.authService.UserProfile;

    if (!this.userProfile?.authUserId) {
      this.router.navigate(['/home']);
      return;
    }

    if (this.userProfile?.isProfileCompleted) {
      this.router.navigate(['/home']);
    }
  }

  onSubmit(ngForm: NgForm): void {
    if (ngForm.invalid) return;

    this.isLoading = true;

    this.usersService.completeProfile(this.userProfile?.authUserId!, this.form).subscribe({
      next: (updated) => {
        this.userProfile = {
            ...updated,
            mustChangePassword: this.userProfile!.mustChangePassword,
            lastLoginAt: this.userProfile!.lastLoginAt,
          };
        this.authService.setUserProfile(this.userProfile);  // ← add this
        this.isLoading = false;
        this.snackBar.open('Profile completed. Welcome!', 'OK', { duration: 3000 });
        this.router.navigate(['/home']);
      },
      error: (err) => {
        this.isLoading = false;
        const message = err.error?.message || 'Failed to complete profile.';
        this.snackBar.open(message, 'Dismiss', { duration: 4000 });
      },
    });
  }

  skip(): void {
    this.router.navigate(['/home']);
  }
}
