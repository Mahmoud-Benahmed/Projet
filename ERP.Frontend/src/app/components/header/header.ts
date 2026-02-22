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

export interface NavLink {
  label: string;
  route: string;
  icon: string;
}

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
  @Input() userName: string = 'John Doe';
  @Input() userEmail: string = 'john@example.com';
  @Input() userRole: string = 'SystemAdmin';
  @Input() notificationCount: number = 3;
  @Output() logoutClicked = new EventEmitter<void>();
  @Output() sidenavToggle = new EventEmitter<void>();

  navLinks: NavLink[] = [
    { label: 'Users', route: '/users', icon: 'group' },
    { label: 'Deactivated', route: '/users/deactivated', icon: 'person_off' },
    { label: 'Settings', route: '/settings', icon: 'settings' },
  ];

  
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
