import { Component, OnInit } from '@angular/core';
import { AuthService, FullProfile } from '../../services/auth.service';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { UsersService } from '../../services/users.service';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatTooltipModule, RouterModule],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class HomeComponent implements OnInit {
  isLoading: boolean = false;

  userProfile: FullProfile | null=null;
  userRole: string| null= null;

  constructor(private authService: AuthService, private userProfileService: UsersService) {}

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.authService.logout();
      return;
    }

    // Use cached profile if already loaded
    if (this.authService.UserProfile) {
      this.userProfile = this.authService.UserProfile;
      this.userRole= this.authService.Role;
    }
  }

  logout(): void {
    this.authService.logout();
  }

  get firstName(): string {
    return this.userProfile?.email?.split('@')[0] ?? 'there';
  }

  get initials(): string {
    return (this.userProfile?.email?.split('@')[0] ?? '?')
      .split('.')
      .map(n => n[0])
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }
}
