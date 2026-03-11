import { Component, Input, Output, EventEmitter, OnInit, ChangeDetectorRef, inject, DestroyRef } from '@angular/core';
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
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthUserGetResponseDto } from '../../interfaces/AuthDto';
import { ThemeService } from '../../services/theme.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

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
  private readonly destroyRef = inject(DestroyRef);


  authUser: AuthUserGetResponseDto | null = null;
  navLinks: NavLink[] = [];
  allNavLinks: NavLink[] = [
    { label: 'Home', route: '/home', icon: 'home' },
    { label: 'Settings', route: '/settings', icon: 'settings'},
    { label: 'Users', route: '/users', icon: 'group', roles: ['SystemAdmin'] },
    { label: 'Deactivated', route: '/users/deactivated', icon: 'person_off', roles: ['SystemAdmin'] },
    { label: 'Deleted', route: '/users/deleted', icon: 'group_remove', roles: ['SystemAdmin'] },
    { label: 'Permissions', route: '/permissions', icon: 'security', roles: ['SystemAdmin']},
    { label: 'Articles', route: '/articles', icon: 'receipt_long', roles: ['SystemAdmin', 'StockManager']},
    { label: 'Audit Log', route: '/audit-log', icon: 'person_alert', roles: ['SystemAdmin']},
  ];

  constructor(private authService: AuthService,
            private snackBar: MatSnackBar,
          private cdr: ChangeDetectorRef,
  ){}

  ngOnInit(): void {
    const role = this.authService.Role;
    this.navLinks = this.allNavLinks.filter(link =>
      !link.roles || link.roles.includes(role!)
    );

    this.authService.userProfile$
    .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(profile => {
        this.authUser = profile;
        this.cdr.markForCheck();
    });

    if (!this.authService.UserProfile) {
      this.authService.getMe().subscribe({
        next: (profile) => this.authService.setUserProfile(profile),
        error: () => this.snackBar.open('Failed to load profile.', 'Dismiss', { duration: 3000 })
      });
    }
  }


  onLogout(): void {
    this.logoutClicked.emit();
  }

  onSidenavToggle(): void {
    this.sidenavToggle.emit();
  }
}
