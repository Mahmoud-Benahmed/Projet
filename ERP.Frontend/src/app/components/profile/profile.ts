import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
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
import { NgForm } from '@angular/forms';
import { NotSameAsDirective } from '../../util/NotSameAsDirective';
import { AdminChangePasswordRequest, ChangePasswordRequest } from '../../interfaces/AuthDto';

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
    NotSameAsDirective
  ],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class ProfileComponent implements OnInit {
  profile: FullProfile | null = null;
  isLoading = true;
  isEditing = false;
  isSaving = false;
  role: string= '';
  authUserId: string | null =null;

  showPasswordForm = false;
  isChangingPassword = false;
  showCurrentPassword = false;
  showNewPassword = false;

  readonly allowedRoles: string[]= ['SystemAdmin', 'HRManager'];


  adminChangePasswordForm: AdminChangePasswordRequest = {
    newPassword: ''
  };

  passwordForm: ChangePasswordRequest = {
    currentPassword: '',
    newPassword: '',
  };

  editForm: CompleteProfileDto = {
    fullName: '',
    phone: '',
  };

  constructor(private userProfileService: UserProfileService,
              private authService: AuthService,
              private snackBar: MatSnackBar,
              private route: ActivatedRoute
            ) {}

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    const routeId = this.route.snapshot.paramMap.get('authUserId');
    this.authUserId = routeId ?? this.authService.UserId;

    if (this.authUserId === null ) return;

    this.isLoading = true;

    forkJoin({
      authUser: this.authService.getUserById(this.authUserId),
      profile: this.userProfileService.getByAuthUserId(this.authUserId),
    }).subscribe({
      next: ({ authUser, profile }) => {
        // merge both responses into one object
        this.profile = {
          ...profile,
          login: authUser.login,
          roleName: authUser.roleName,
          mustChangePassword: authUser.mustChangePassword,
          lastLoginAt: authUser.lastLoginAt,
        };
        console.log(authUser.login);// undefined

        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.snackBar.open('Failed to load profile.', 'Dismiss', { duration: 3000 });
      },
    });
  }

  togglePasswordForm(): void {
    this.showPasswordForm = !this.showPasswordForm;
    if (!this.showPasswordForm) {
      // reset form when closing
      this.passwordForm = { currentPassword: '', newPassword: '' };
      this.showCurrentPassword = false;
      this.showNewPassword = false;
    }
  }

  changePassword(form: NgForm): void {
    if (form.invalid) return;

    this.isChangingPassword= true;

    if(this.isOwnProfile){
      this.authService.changePassword(this.passwordForm).subscribe({
        next: () => {
          this.isSaving= false;
          this.togglePasswordForm();
          this.snackBar.open('Password updated successfully.', 'OK', { duration: 3000 });
        },
        error: (err) => {
          this.isSaving= false;
          const message = err.error?.message || 'Failed to update password.';
          this.snackBar.open(message, 'Dismiss', { duration: 4000 });
        },
      });
    }
    else if(['SystemAdmin', 'HRManager'].includes(this.authService.Role!)){
      this.authService.adminChangePassword(this.authUserId!, this.adminChangePasswordForm).subscribe({
        next: () => {
          this.isSaving= false;
          this.togglePasswordForm();
          this.snackBar.open('Password updated successfully.', 'OK', { duration: 3000 });
        },
        error: (err) => {
          this.isSaving= false;
          const message = err.error?.message || 'Failed to update password.';
          this.snackBar.open(message, 'Dismiss', { duration: 4000 });
        },
      });
    }

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
            login: this.profile!.login,
            roleName: this.profile!.roleName,
            mustChangePassword: this.profile!.mustChangePassword,
            lastLoginAt: this.profile!.lastLoginAt,
          };
          this.isEditing = false;
          this.isSaving = false;
          this.snackBar.open('Profile updated successfully.', 'OK', { duration: 3000 });
      },
      error: () => {
          this.isSaving = false;
          this.snackBar.open('Failed to load profile.', 'Dismiss', { duration: 3000 });
      }
    });
  }

  get hasRole():boolean{
    return this.allowedRoles.includes(this.authService.Role!) && !this.isOwnProfile;
  }

  get canEditProfile(): boolean{
    return this.isOwnProfile || this.hasRole;
  }

  get isOwnProfile(): boolean {
    return this.profile?.authUserId === this.authService.UserId;
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
