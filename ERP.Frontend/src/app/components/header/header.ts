import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatBadgeModule } from '@angular/material/badge';
import { MatTooltipModule } from '@angular/material/tooltip';
import { NavLink } from '../../interfaces/NavLink';
import { AuthService } from '../../services/auth.service';
import { AuthUserDto } from '../../interfaces/AuthDto';
import { UsersService } from '../../services/users.service';
import { forkJoin } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FullProfile } from '../../interfaces/UserProfileDto';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule,
    MatBadgeModule,
    MatTooltipModule,
  ],
  templateUrl: './header.html',
  styleUrl: './header.scss',
})
export class HeaderComponent implements OnInit {
  @Input() notificationCount: number = 3;
  @Output() logoutClicked = new EventEmitter<void>();
  @Output() sidenavToggle = new EventEmitter<void>();


  authUser: FullProfile | null = null;
  allNavLinks: NavLink[] = [
    { label: 'Home', route: '/home', icon: 'home' },
    { label: 'Settings', route: '/settings', icon: 'settings'},
    { label: 'Users', route: '/users', icon: 'group', roles: ['SystemAdmin'] },
    { label: 'Deactivated', route: '/users/deactivated', icon: 'person_off', roles: ['SystemAdmin'] },
    { label: 'Permissions', route: '/permissions', icon: 'security', roles: ['SystemAdmin']},
  ];

  constructor(private authService: AuthService,
              private userProfileService: UsersService,
            private snackBar: MatSnackBar){}

  ngOnInit(){
     forkJoin({
          authUser: this.authService.getUserById(this.authService.UserId!),
          profile: this.userProfileService.getByAuthUserId(this.authService.UserId!),
        }).subscribe({
          next: ({ authUser, profile }) => {
            // merge both responses into one object
            this.authUser = {
              ...profile,
              login: authUser.login,
              roleName: authUser.roleName,
              mustChangePassword: authUser.mustChangePassword,
              lastLoginAt: authUser.lastLoginAt ?? undefined,
            };
          },
          error: () => {
            this.snackBar.open('Failed to load profile.', 'Dismiss', { duration: 3000 });
          },
        });
  }

  get navLinks(): NavLink[] {
    return this.allNavLinks.filter(link =>
      !link.roles || link.roles.includes(this.authService.Role!)
    );
  }


  onLogout(): void {
    this.logoutClicked.emit();
  }

  onSidenavToggle(): void {
    this.sidenavToggle.emit();
  }
}
