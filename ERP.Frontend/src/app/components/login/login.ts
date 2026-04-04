import { AuthService } from '../../services/auth/auth.service';
import { Component, OnInit, OnDestroy, ChangeDetectorRef, ViewEncapsulation, inject } from '@angular/core';
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
import { environment } from '../../environment';
import { UserSettingsService } from '../../services/user-settings.service';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';

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
    MatDialogModule,
    TranslatePipe
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss',
  encapsulation: ViewEncapsulation.None
})
export class LoginComponent implements OnInit{
  private langSub?: Subscription;

  readonly year: number = new Date().getFullYear();
  userProfile: AuthUserGetResponseDto | null = null;

  credentials = { login: '', password: '' };
  showPassword = false;
  isLoading = false;

  constructor(
    private router: Router,
    private authService: AuthService,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef,
    public userSettings: UserSettingsService,
    public translate: TranslateService
  ) {}


  ngOnInit(): void {
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/home']);
    }

    this.langSub = this.translate.onLangChange.subscribe(() => {
      this.cdr.detectChanges();
    });

    this.cdr.detectChanges();
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
            this.isLoading = false;
            this.userProfile = authUser;
            this.authService.setUserProfile(this.userProfile);

            if (response.mustChangePassword && environment.production) {
              this.stopLoading();
              this.router.navigate(['/must-change-password']);
              return;
            }
            this.router.navigate(['/home']);
          },
          error: () => {
            this.stopLoading();
            this.authService.logout();
          }
        });
      },
      error: (error) => {
        this.stopLoading();
        if ([0, 403, 429].includes(error.status)) return;
        const code = error.error?.code ?? 'UNKNOWN';
        const key  = `ERRORS.${code}`;
        const msg  = this.translate.instant(key);
        const display = msg === key ? (error.error?.message ?? msg) : msg;

        this.dialog.open(ModalComponent, {
          width: '400px',
          data: {
            title:       this.translate.instant('DIALOG.ACCESS_DENIED'),
            message:     display,
            confirmText: this.translate.instant('DIALOG.OK'),
            showCancel:  false,
            icon:        'dangerous',
            iconColor:   'danger'
          }
        });
      }
    });
  }

  stopLoading() {
    this.isLoading = false;
    this.cdr.markForCheck();
  }

  ngOnDestroy(): void {
    this.langSub?.unsubscribe();
  }
}
