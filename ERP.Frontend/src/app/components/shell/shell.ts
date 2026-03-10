import { ChangeDetectorRef, Component, OnInit, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../services/auth.service';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, MatTooltipModule],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
  encapsulation: ViewEncapsulation.None,
})
export class ShellComponent implements OnInit {

  collapsed = false;
  openGroups: Record<string, boolean> = { auth: false };
  currentPage = 'Home';
  userName = '';
  userRole = '';
  initials = '';

  private pageMap: Record<string, string> = {
    '/home': 'Home',
    '/users': 'Users',
    '/users/deactivated': 'Deactivated',
    '/users/register': 'Register',
    '/permissions': 'Permissions',
    '/articles': 'Articles',
    '/audit-log': 'Audit Log',
    '/profile': 'My Profile',
  };

  constructor(private router: Router, private authService: AuthService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    // Set breadcrumb on route change
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd)
    ).subscribe((e: any) => {
      this.currentPage = this.pageMap[e.urlAfterRedirects] ?? 'Dashboard';
      // Auto-open auth group if on an auth route
      if (e.urlAfterRedirects.startsWith('/users') || e.urlAfterRedirects.startsWith('/permissions')) {
        this.openGroups['auth'] = true;
      }
    });

    // Load user info from cache
    const profile = this.authService.UserProfile;
    if (profile) {
      this.userName = profile.fullName ?? profile.email ?? '';
      this.userRole = profile.roleName ?? '';
      this.initials = this.buildInitials(this.userName || profile.email || 'U');
    }

    // Set initial breadcrumb
    this.currentPage = this.pageMap[this.router.url] ?? 'Dashboard';
    if (this.router.url.startsWith('/users') || this.router.url.startsWith('/permissions')) {
      this.openGroups['auth'] = true;
    }
    this.cdr.markForCheck();
  }

  toggleSidebar(): void {
    this.collapsed = !this.collapsed;
  }

  toggleGroup(key: string): void {
    this.openGroups[key] = !this.openGroups[key];
  }

  onLogout(): void {
    this.authService.logout();
  }

  private buildInitials(name: string): string {
    return name.split(' ').map(n => n[0]).slice(0, 2).join('').toUpperCase() || 'U';
  }
}
