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
import { AuthService } from '../../services/auth.service';
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

  profile: any = null;
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


  readonly allowedRoles: string[]= ['SystemAdmin', 'HRManager'];

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

    if (this.authUserId === null ) return;

    this.isLoading = true;
    if(this.authService.Role==='SystemAdmin'){
      forkJoin({
        authUser: this.authService.getById(this.authUserId),
        profile: this.userProfileService.getByAuthUserId(this.authUserId),
      }).subscribe({
        next: ({ authUser, profile }) => {
          // merge both responses into one object
          this.profile = {
            ...profile,
            ...authUser
          };
          this.stopLoading('isLoading');
          return;
        },
        error: () => {
          this.stopLoading('isLoading');
          this.snackBar.open('Failed to load profile.', 'Dismiss', { duration: 3000 });
        },
      });
    }else{
      this.authService.UserProfile;
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

  changePassword(form: NgForm): void {
    if (form.invalid) return;

    this.isChangingPassword= true;

    if(this.isOwnProfile){
      this.authService.changePassword(this.passwordForm).subscribe({
        next: () => {
          this.stopLoading('isSaving');
          this.togglePasswordForm();
          this.snackBar.open('Password updated successfully.', 'OK', { duration: 3000 });
        },
        error: (err) => {
          this.stopLoading('isSaving');
          const message = err.error?.message || 'Failed to update password.';
          this.snackBar.open(message, 'Dismiss', { duration: 4000 });
        },
      });
    }
    else if(['SystemAdmin', 'HRManager'].includes(this.authService.Role!)){
      this.authService.adminChangePassword(this.authUserId!, this.adminChangePasswordForm).subscribe({
        next: () => {
          this.stopLoading('isSaving');
          this.togglePasswordForm();
          this.snackBar.open('Password updated successfully.', 'OK', { duration: 3000 });
        },
        error: (err) => {
          this.stopLoading('isSaving');
          const message = err.error?.message || 'Failed to update password.';
          this.snackBar.open(message, 'Dismiss', { duration: 4000 });
        },
      });
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
    return this.profile?.authUserId === this.authService.UserId;
  }

  get initials(): string {
    const name:string = this.profile?.fullName ?? this.profile?.email ?? '?';
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

  stopLoading(type: 'isSaving' | 'isLoading'):void{
    if(type==='isLoading')this.isLoading= false;
    if(type==='isSaving') this.isSaving= false;
    this.cdr.markForCheck();
  }
}
