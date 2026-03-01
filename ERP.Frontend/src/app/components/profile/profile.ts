import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
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
import { AuthService, FullProfile } from '../../services/auth.service';
import { CompleteProfileDto, UserProfileResponseDto } from '../../interfaces/UserProfileDto';
import { forkJoin } from 'rxjs';
import { NgForm } from '@angular/forms';
import { NotSameAsDirective } from '../../util/NotSameAsDirective';
import { AdminChangePasswordRequestDto, ChangePasswordRequestDto } from '../../interfaces/AuthDto';
import { SameAsDirective } from "../../util/SameAsDirective";
import { checkPassword, generatePassword } from '../../util/PasswordUtil';

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
    NotSameAsDirective,
    SameAsDirective
],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class ProfileComponent implements OnInit {

  @ViewChild('passwordFormRef') passwordFormRef!: NgForm;

  userProfile: FullProfile|null = null;
  isLoading = true;
  isEditing = false;
  isSaving = false;
  role: string= '';
  authUserId: string | null =null;

  showPasswordForm = false;
  isChangingPassword = false;
  showCurrentPassword = false;
  showNewPassword = false;

  passwordErrors: string[] = [];
  passwordScore: number = 0;
  passwordStrength: string = '';

  readonly passwordPattern = /^[^<>&"'\/]{8,}$/.source;


  adminChangePasswordForm: AdminChangePasswordRequestDto = {
    newPassword: ''
  };

  passwordForm: ChangePasswordRequestDto = {
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
              private route: ActivatedRoute,
              private cdr: ChangeDetectorRef,
            ) {}

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
      const routeId = this.route.snapshot.paramMap.get('authUserId');
      this.authUserId = routeId ?? this.authService.UserId;

      if (!this.authUserId) {
        this.isLoading = false;
        return;
      }

      if (this.authService.Role === 'SystemAdmin') {
        forkJoin({
          authUser: this.authService.getById(this.authUserId),
          profile: this.userProfileService.getByAuthUserId(this.authUserId),
        }).subscribe({
          next: ({ authUser, profile }) => {
            this.userProfile = {
              ...profile,
              mustChangePassword: authUser.mustChangePassword,
              lastLoginAt: authUser.lastLoginAt
            };
            this.stopLoading('isLoading');
          },
          error: () => {
            this.stopLoading('isLoading');
            this.snackBar.open('Failed to load profile.', 'Dismiss', { duration: 3000 });
          }
        });
      } else {
            const cached = this.authService.UserProfile;
            if (cached) {
              this.userProfile = cached;
              this.isLoading = false;
            } else {
                // Cache miss (e.g. page refresh) — fetch fresh data
                forkJoin({
                  authUser: this.authService.getMe(),
                  profile: this.userProfileService.getMe(),
                }).subscribe({
                  next: ({ authUser, profile }) => {
                    this.userProfile = {
                      ...profile,
                      mustChangePassword: authUser.mustChangePassword,
                      lastLoginAt: authUser.lastLoginAt,
                    };
                    this.authService.setUserProfile(this.userProfile);
                    this.stopLoading('isLoading');
                  },
                  error: () => {
                    this.stopLoading('isLoading');
                    this.snackBar.open('Failed to load profile.', 'Dismiss', { duration: 3000 });
                  }
                });
            }
      }
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

  // Fix: use isChangingPassword consistently
  changePassword(form: NgForm): void {
      if (form.invalid) return;
      this.isChangingPassword = true;

      const stop = () => {
        this.isChangingPassword = false;
        this.cdr.markForCheck();
      };

      const onSuccess = () => {
        stop();
        this.togglePasswordForm();
        this.snackBar.open('Password updated successfully.', 'OK', { duration: 3000 });
      };

      const onError = (err: any) => {
        stop();
        this.snackBar.open(err.error?.message || 'Failed to update password.', 'Dismiss', { duration: 4000 });
      };

      if (this.isOwnProfile) {
        this.authService.changePassword(this.passwordForm).subscribe({ next: onSuccess, error: onError });
      } else if (['SystemAdmin', 'HRManager'].includes(this.authService.Role!)) {
        this.authService.adminChangePassword(this.authUserId!, this.adminChangePasswordForm).subscribe({ next: onSuccess, error: onError });
      }
  }

  onPasswordChange(): void {
    const result = checkPassword(this.passwordForm.newPassword);
    this.passwordErrors = result.errors;
    this.passwordScore = result.score;
    this.passwordStrength = result.strength;

    // revalidate currentPassword when newPassword changes
    const currentPwdControl = this.passwordFormRef?.controls?.['currentPassword'];
    if (currentPwdControl) {
      currentPwdControl.updateValueAndValidity();
    }
  }


  onCurrentPasswordChange(): void {
    const newPwdControl = this.passwordFormRef?.controls?.['newPassword'];
    if (newPwdControl) {
      newPwdControl.updateValueAndValidity();
    }
  }

  startEditing(): void {
    if (!this.userProfile) return;
    this.editForm = {
      fullName: this.userProfile.fullName ?? '',
      phone: this.userProfile.phone ?? '',
    };
    this.isEditing = true;
  }

  cancelEditing(): void {
    this.isEditing = false;
  }

  saveProfile(): void {
    if (!this.userProfile) return;
    this.isSaving = true;

    this.userProfileService.completeProfile(this.userProfile.authUserId, this.editForm).subscribe({
      next: (updated) => {
          this.userProfile = {
            ...updated,
            mustChangePassword: this.userProfile!.mustChangePassword,
            lastLoginAt: this.userProfile!.lastLoginAt,
          };
          this.authService.setUserProfile(this.userProfile);  // ← add this
          this.isEditing = false;
          this.stopLoading('isSaving');
          this.snackBar.open('Profile updated successfully.', 'OK', { duration: 3000 });
      },
      error: () => {
          this.stopLoading('isSaving');
          this.snackBar.open('Failed to load profile.', 'Dismiss', { duration: 3000 });
      }
    });
  }



  generatePassword(): void {
    this.passwordForm.newPassword = generatePassword();
    if (!this.showNewPassword) this.showNewPassword = true;
    this.onPasswordChange();
  }

  getScore(): number {
    const map: Record<string, number> = {
      'weak': 1, 'fair': 2, 'strong': 3, 'very strong': 4,
    };
    return map[this.passwordStrength] ?? 0;
  }

  getStrengthClass(): string {
    const map: Record<string, string> = {
      'weak': 'strength--weak',
      'fair': 'strength--fair',
      'strong': 'strength--strong',
      'very strong': 'strength--very-strong',
    };
    return map[this.passwordStrength] ?? '';
  }

  getStrengthLabel(): string {
    const map: Record<string, string> = {
      'weak': 'Weak', 'fair': 'Fair', 'strong': 'Strong', 'very strong': 'Very Strong',
    };
    return map[this.passwordStrength] ?? '';
  }

  get hasPrivilege():boolean{
    return this.authService.Privileges.includes('ManageUsers') && !this.isOwnProfile;
  }

  get canEditProfile(): boolean{
    return this.isOwnProfile || this.hasPrivilege;
  }

  get isOwnProfile(): boolean {
    return this.userProfile?.authUserId === this.authService.UserId;
  }

  get initials(): string {
    const name:string = this.userProfile?.fullName ?? this.userProfile?.email ?? '?';
    return name
      .split(' ')
      .map((n) => n[0])
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }

  get memberSince(): string {
    if (!this.userProfile?.createdAt) return '—';
    return new Date(this.userProfile.createdAt).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }

  stopLoading(type: 'isSaving' | 'isLoading'):void{
    if(type==='isLoading')this.isLoading= false;
    if(type==='isSaving') this.isSaving= false;
    this.cdr.markForCheck();
  }
}
