import { ChangeDetectorRef, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthUserGetResponseDto } from '../../interfaces/AuthDto';
import { HttpError } from '../../interfaces/ErrorDto';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatTooltipModule, RouterModule],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  encapsulation: ViewEncapsulation.None,
})
export class HomeComponent implements OnInit {
  isLoading = false;
  userProfile: AuthUserGetResponseDto | null = null;
  lastLogin: string = '';
  error: string | null = null;
  successMessage: string | null = null;

  constructor(
    public authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.authService.logout();
      return;
    }

    this.lastLogin = this.getLastLogin();

    if (this.authService.UserProfile) {
      this.userProfile = this.authService.UserProfile;
    } else {
      this.authService.getMe().subscribe({
        next: (authUser) => {
          this.userProfile = authUser;
          this.authService.setUserProfile(this.userProfile);
        },
        error: (error) => {
          const err= error.error as HttpError;
          this.flash('error', err.message);
        }
      });
    }
  }


  dismissError(): void { this.error = null; }

  flash(type: 'success' | 'error', msg: string): void {
    if(type === 'success'){
      this.successMessage = msg;
      this.cdr.markForCheck();
      setTimeout(() => (this.successMessage = null), 3000);
    }
    else{
      this.error = msg;
      this.cdr.markForCheck();
      setTimeout(() => (this.error = null), 3000);
    }
  }


  private getLastLogin(): string {
    const now = new Date();
    const hours = now.getHours().toString().padStart(2, '0');
    const minutes = now.getMinutes().toString().padStart(2, '0');
    return `Today at ${hours}:${minutes}`;
  }

  logout(): void {
    this.authService.logout();
  }
}
