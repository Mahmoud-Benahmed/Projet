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
import { AuthService } from '../../services/auth.service';
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

  ngOnInit(){
    this.usersService.getByAuthUserId(this.authService.UserId!).subscribe({
      next: (profile)=>{
        if(profile.isProfileCompleted){
          this.router.navigate(["/profile"]);
        }
      },
      error:(err)=>{
        const message = err.error?.message || 'Failed to load profile.';
        this.snackBar.open(message, 'Dismiss', { duration: 4000 });
        this.router.navigate(["/home"]);
      }
    });
  }

  onSubmit(ngForm: NgForm): void {
    if (ngForm.invalid) return;

    this.isLoading = true;
    const userId = this.authService.UserId!;

    this.usersService.completeProfile(userId, this.form).subscribe({
      next: () => {
        this.isLoading = false;
        this.snackBar.open('Profile completed. Welcome!', 'OK', { duration: 3000 });
        const role = this.authService.Role!;
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
    const role = this.authService.Role!;
    this.router.navigate(['/home']);
  }
}
