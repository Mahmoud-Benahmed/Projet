import { PRIVILEGES } from './../../../services/auth/auth.service';
import { ChangeDetectorRef, Component, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink, RouterLinkActive, RouterModule } from '@angular/router';
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
import { AuthService } from '../../../services/auth/auth.service';
import { NgForm } from '@angular/forms';
import { checkPassword, generatePassword } from '../../../util/PasswordUtil';
import { AdminChangeProfileRequest, AuthUserGetResponseDto, ChangeProfilePasswordRequestDto, UpdateProfileDto } from '../../../interfaces/AuthDto';
import { HttpError } from '../../../interfaces/ErrorDto';
import { MatDialog } from '@angular/material/dialog';
import { ModalComponent } from '../../modal/modal';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

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
    RouterLink,
    TranslatePipe
  ],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class ProfileComponent implements OnInit {

  @ViewChild('passwordFormRef') passwordFormRef!: NgForm;

  private translate = inject(TranslateService);

  infoCollapsed    = false;
  accountCollapsed = false;

  userProfile: AuthUserGetResponseDto | null = null;
  isLoading = true;
  isEditing = false;
  isSaving = false;
  role: string = '';
  authUserId: string | null = null;
  noDataChange: boolean = true;

  showPasswordForm = false;
  isChangingPassword = false;
  showCurrentPassword = false;
  showNewPassword = false;

  passwordErrors: string[] = [];
  passwordScore: number = 0;
  passwordStrength: string = '';

  readonly emailPattern = /^(([^<>()[\]\\.,;:\s@"]+(\.[^<>()[\]\\.,;:\s@"]+)*)|.(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/.source;
  readonly fullNamePattern = /^\p{L}+(\s\p{L}+)*$/u;

  adminChangePasswordForm: AdminChangeProfileRequest = {
    newPassword: ''
  };

  passwordForm: ChangeProfilePasswordRequestDto = {
    currentPassword: '',
    newPassword: '',
  };

  editForm: UpdateProfileDto = {
    fullName: '',
    email: '',
  };

  constructor(
    private authService: AuthService,
    private dialog: MatDialog,
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

    if (this.authService.hasPrivilege(PRIVILEGES.USERS.VIEW_USERS) && this.authService.UserId !== this.authUserId) {
      this.authService.getById(this.authUserId).subscribe({
        next: (authUser) => {
          this.userProfile = authUser;
          this.stopLoading('isLoading');
        },
        error: () => {
          this.stopLoading('isLoading');
          this.showErrorDialog(this.translate.instant('USERS.PROFILE.LOAD_ERROR_TITLE'), this.translate.instant('USERS.PROFILE.LOAD_ERROR_MESSAGE'));
        }
      });
    } else {
      const cached = this.authService.UserProfile;
      if (cached) {
        this.userProfile = cached;
        this.isLoading = false;
      } else {
        this.authService.getMe().subscribe({
          next: (authUser) => {
            this.userProfile = authUser;
            this.authService.setUserProfile(this.userProfile);
            this.stopLoading('isLoading');
          },
          error: () => {
            this.stopLoading('isLoading');
            this.showErrorDialog(this.translate.instant('USERS.PROFILE.LOAD_ERROR_TITLE'), this.translate.instant('USERS.PROFILE.LOAD_ERROR_MESSAGE'));
          }
        });
      }
    }
  }

  private showErrorDialog(title: string, message: string): void {
    this.dialog.open(ModalComponent, {
      width: '400px',
      data: {
        title: title,
        message: message,
        confirmText: this.translate.instant('COMMON.OK'),
        showCancel: false,
        icon: 'info',
        iconColor: 'warn'
      }
    });
  }

  private showSuccessDialog(message: string): void {
    this.dialog.open(ModalComponent, {
      width: '400px',
      data: {
        title: this.translate.instant('USERS.PROFILE.SUCCESS_TITLE'),
        message: message,
        confirmText: this.translate.instant('COMMON.OK'),
        showCancel: false,
        icon: 'check_circle',
        iconColor: 'success'
      }
    });
  }

  togglePasswordForm(): void {
    this.showPasswordForm = !this.showPasswordForm;
    if (!this.showPasswordForm) {
      this.passwordForm = { currentPassword: '', newPassword: '' };
      this.showCurrentPassword = false;
      this.showNewPassword = false;
    }
  }

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
      this.showSuccessDialog(this.translate.instant('SUCCESS.PASSWORD_CHANGED'));
    };

    const onError = (err: any) => {
      stop();
      const error = err.error as HttpError;
      this.passwordErrors.push(error.message);
    };

    if (this.isOwnProfile) {
      this.authService.changeProfilePassword(this.passwordForm).subscribe({ next: onSuccess, error: onError });
    } else if (this.hasPrivilege) {
      this.authService.adminChangePassword(this.authUserId!, this.adminChangePasswordForm).subscribe({ next: onSuccess, error: onError });
    }
  }

  onPasswordChange(): void {
    const pwd = this.hasPrivilege
      ? this.adminChangePasswordForm.newPassword
      : this.passwordForm.newPassword;

    const currentPasswd = this.hasPrivilege
      ? null
      : this.passwordForm.currentPassword;

    const result = checkPassword(pwd, currentPasswd);
    this.passwordErrors = result.errors;
    this.passwordScore = result.score;
    this.passwordStrength = result.strength;

    const currentPwdControl = this.passwordFormRef?.controls?.['currentPassword'];
    if (currentPwdControl) {
      currentPwdControl.updateValueAndValidity();
    }
  }

  onCurrentPasswordChange(): void {
    this.onPasswordChange();
    const newPwdControl = this.passwordFormRef?.controls?.['newPassword'];
    if (newPwdControl) {
      newPwdControl.updateValueAndValidity();
    }
  }

  startEditing(): void {
    if (!this.userProfile) return;
    this.editForm = {
      fullName: this.userProfile.fullName ?? '',
      email: this.userProfile.email ?? '',
    };
    this.noDataChange = true;
    this.isEditing = true;
  }

  cancelEditing(): void {
    this.isEditing = false;
  }

  checkChanges() {
    const profile = this.authService.UserProfile;
    if (!profile) return;
    this.noDataChange = this.editForm.email === profile.email
      && this.editForm.fullName === profile.fullName;
  }

  saveProfile(): void {
    if (!this.userProfile) return;
    this.isSaving = true;

    this.authService.update(this.userProfile.id, this.editForm).subscribe({
      next: (updated) => {
        this.userProfile = {
          ...updated,
          mustChangePassword: this.userProfile!.mustChangePassword,
          lastLoginAt: this.userProfile!.lastLoginAt,
        };
        this.authService.setUserProfile(this.userProfile);
        this.isEditing = false;
        this.stopLoading('isSaving');
        this.showSuccessDialog(this.translate.instant('SUCCESS.USER_UPDATED'));
      },
      error: (error) => {
        const err = error.error as HttpError;
        this.stopLoading('isSaving');
        this.showErrorDialog(this.translate.instant('USERS.PROFILE.UPDATE_ERROR_TITLE'), err.message);
      }
    });
  }

  generatePassword(): void {
    const pwd = generatePassword();

    if (this.hasPrivilege) {
      this.adminChangePasswordForm.newPassword = pwd;
    } else {
      this.passwordForm.newPassword = pwd;
    }
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
    const strengthKey = this.passwordStrength.toUpperCase().replace(' ', '_');
    return this.translate.instant(`VALIDATION.PASSWORD_${strengthKey}`);
  }

  get hasPrivilege(): boolean {
    return this.authService.hasPrivilege(PRIVILEGES.USERS.UPDATE_USER) && !this.isOwnProfile;
  }

  get isOwnProfile(): boolean {
    return this.selectedUserId === this.authService.UserId;
  }

  get canEditProfile(): boolean {
    return this.isOwnProfile || this.hasPrivilege;
  }

  get selectedUserId(): string | undefined {
    return this.userProfile?.id;
  }

  get initials(): string {
    const name: string = this.userProfile?.fullName ?? this.userProfile?.email ?? '?';
    const words = name.split(' ').filter(w => w.length > 0);

    if (words.length === 0) return '?';
    if (words.length === 1) return words[0][0].toUpperCase();

    const firstLetter = words[0][0];
    const lastLetter = words[words.length - 1][0];

    return (firstLetter + lastLetter).toUpperCase();
  }

  get memberSince(): string {
    if (!this.userProfile?.createdAt) return '—';
    return new Date(this.userProfile.createdAt).toLocaleDateString(this.translate.currentLang === 'fr' ? 'fr-FR' : 'en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }

  stopLoading(type: 'isSaving' | 'isLoading'): void {
    if (type === 'isLoading') this.isLoading = false;
    if (type === 'isSaving') this.isSaving = false;
    this.cdr.markForCheck();
  }
}
