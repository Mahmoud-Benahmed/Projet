import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { UsersService as UserProfileService } from '../../services/users.service';
import { AuthService } from '../../services/auth.service';
import { UserProfileResponseDto, CompleteProfileDto, FullProfile } from '../../interfaces/UserProfileDto';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class ProfileComponent implements OnInit {
  profile: FullProfile | null = null;  isLoading = true;
  isEditing = false;
  isSaving = false;
  role: string= '';

  editForm: CompleteProfileDto = {
    fullName: '',
    phone: '',
  };

  constructor(
    private userProfileService: UserProfileService,
    private authService: AuthService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    const authUserId = this.authService.getUserId();
    if (!authUserId) return;

    this.isLoading = true;

    forkJoin({
      authUser: this.authService.getUserById(authUserId),
      profile: this.userProfileService.getByAuthUserId(authUserId),
    }).subscribe({
      next: ({ authUser, profile }) => {
        // merge both responses into one object
        this.profile = {
          ...profile,
          role: authUser.role,
          mustChangePassword: authUser.mustChangePassword,
          lastLoginAt: authUser.lastLoginAt,
        };
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.snackBar.open('Failed to load profile.', 'Dismiss', { duration: 3000 });
      },
    });
  }

  startEditing(): void {
    if (!this.profile) return;
    this.editForm = {
      fullName: this.profile.fullName ?? '',
      phone: this.profile.phone ?? '',
    };
    this.isEditing = true;
  }

  cancelEditing(): void {
    this.isEditing = false;
  }

  saveProfile(): void {
    if (!this.profile) return;
    this.isSaving = true;

    this.userProfileService.completeProfile(this.profile.authUserId, this.editForm).subscribe({
      next: (updated) => {
          this.profile = {
            ...updated,
            role: this.profile!.role,
            mustChangePassword: this.profile!.mustChangePassword,
            lastLoginAt: this.profile!.lastLoginAt,
          };
          this.isEditing = false;
          this.isSaving = false;
          this.snackBar.open('Profile updated successfully.', 'OK', { duration: 3000 });
      },
      error: () => {
          this.isLoading = false;
          this.snackBar.open('Failed to load profile.', 'Dismiss', { duration: 3000 });
      }
    });
  }

  get initials(): string {
    const name = this.profile?.fullName ?? this.profile?.email ?? '?';
    return name
      .split(' ')
      .map((n) => n[0])
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }

  get memberSince(): string {
    if (!this.profile?.createdAt) return '—';
    return new Date(this.profile.createdAt).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }
}
