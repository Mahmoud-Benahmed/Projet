import { ChangeDetectorRef, Component, OnDestroy, OnInit, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../services/auth.service';
import { filter } from 'rxjs/operators';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, MatTooltipModule],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
  encapsulation: ViewEncapsulation.None,
})
export class ShellComponent implements OnInit, OnDestroy {

  mobileNavOpen = false;
  mobileNavClosing = false;

  collapsed = false;
  openGroups: Record<string, boolean> = {
    auth: false,
    articles: false,
    clients:  false
  };

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
    '/articles/deleted': 'Deleted Articles',

    '/clients': 'Clients',
    '/clients/deleted': 'Deleted Clients',

    '/audit-log': 'Audit Log',
    '/profile': 'My Profile',
  };

  constructor(private router: Router, public authService: AuthService, private cdr: ChangeDetectorRef, public theme: ThemeService) {
  }

  ngOnInit(): void {
    this.theme.init();
    // Set breadcrumb on route change
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd)
    ).subscribe((e: any) => {
      this.currentPage = this.pageMap[e.urlAfterRedirects] ?? 'Dashboard';
      if (e.urlAfterRedirects.startsWith('/users') || e.urlAfterRedirects.startsWith('/permissions')) {
        this.openGroups['auth'] = true;
      }

      if (e.urlAfterRedirects.startsWith('/articles')) {
        this.openGroups['articles'] = true;
      }

      if (e.urlAfterRedirects.startsWith('/clients')) {
        this.openGroups['clients'] = true;
      }
    });

    // Load user info from cache
    // Subscribe so it updates when profile loads/changes
    this.authService.userProfile$.subscribe(profile => {
      if (profile) {
        this.userName = profile.fullName ?? profile.email ?? '';
        this.userRole = profile.roleName ?? '';
        this.initials = this.buildInitials(this.userName || profile.email || 'U');
        this.cdr.markForCheck();
      }
    });

    this.currentPage = this.pageMap[this.router.url] ?? 'Dashboard';

    if (this.router.url.startsWith('/users') ||
        this.router.url.startsWith('/permissions')) {
      this.openGroups['auth'] = true;
    }
    
    if (this.router.url.startsWith('/articles')) {
      this.openGroups['articles'] = true;
    }

    if (this.router.url.startsWith('/clients')) {
      this.openGroups['clients'] = true;
    }
    this.cdr.markForCheck();

    window.addEventListener('resize', this.resizeListener);
  }

  private resizeListener = () => {
    if (window.innerWidth > 768) this.closeMobileNav();
  };

  toggleNav(): void {
  if (window.innerWidth <= 768) {
    this.mobileNavOpen ? this.closeMobileNav() : this.openMobileNav();
  } else {
    this.toggleSidebar();
  }
}

  toggleSidebar(): void {
    this.collapsed = !this.collapsed;
  }

  toggleGroup(key: string): void {
    this.openGroups[key] = !this.openGroups[key];
  }

  openMobileNav(): void {
    this.mobileNavOpen    = true;
    this.mobileNavClosing = false;
    document.body.style.overflow = 'hidden';
  }

  closeMobileNav(): void {
    document.body.style.overflow = ''; // ← immediately, before animation
    this.mobileNavClosing = true;
    setTimeout(() => {
      this.mobileNavOpen    = false;
      this.mobileNavClosing = false;
    }, 220);
  }

  onLogout(): void {
    this.authService.logout();
  }

  private buildInitials(name: string): string {
    return name.split(' ').map(n => n[0]).slice(0, 2).join('').toUpperCase() || 'U';
  }

  ngOnDestroy(): void {
    document.body.style.overflow = ''; // ← safety cleanup
    window.removeEventListener('resize', this.resizeListener);
  }

}
