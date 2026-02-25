import { Component, Input, Output, EventEmitter } from '@angular/core';
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
export class HeaderComponent {
  @Input() userName: string = '-';
  @Input() userEmail: string = '-';
  @Input() userRole: string = '-';
  @Input() notificationCount: number = 3;
  @Output() logoutClicked = new EventEmitter<void>();
  @Output() sidenavToggle = new EventEmitter<void>();

  allNavLinks: NavLink[] = [
    { label: 'Home', route: '/home', icon: 'home' },
    { label: 'Users', route: '/users', icon: 'group', roles: ['SystemAdmin'] },
    { label: 'Deactivated', route: '/users/deactivated', icon: 'person_off', roles: ['SystemAdmin'] },
    { label: 'Settings', route: '/settings', icon: 'settings' },
  ];

  get navLinks(): NavLink[] {
    return this.allNavLinks.filter(link =>
      !link.roles || link.roles.includes(this.userRole)
    );
  }


  get initials(): string {
    return this.userName
      .split(' ')
      .map((n) => n[0])
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }



  onLogout(): void {
    this.logoutClicked.emit();
  }

  onSidenavToggle(): void {
    this.sidenavToggle.emit();
  }
}
