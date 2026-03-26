import { AuthService, PRIVILEGES } from './../../services/auth/auth.service';
import { ChangeDetectorRef, Component, OnDestroy, OnInit, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { filter } from 'rxjs/operators';
import { ThemeService } from '../../services/theme.service';
import { Subscription } from 'rxjs';

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

  breadcrumbs: { label: string; link?: string }[] = [];
  private subs = new Subscription();

  collapsed = false;
  openGroups: Record<string, boolean> = {
    auth: false,
    articles: false,
    clients:  false
  };

  userName = '';
  userRole = '';
  initials = '';
  readonly PRIVILEGES= PRIVILEGES;


  constructor(private router: Router, public authService: AuthService, private cdr: ChangeDetectorRef, public theme: ThemeService) {
  }


  ngOnInit(): void {
    this.theme.init();

    this.subs.add(
      this.router.events.pipe(
        filter(e => e instanceof NavigationEnd)
      ).subscribe((e: any) => {
        const url: string = e.urlAfterRedirects;
        this.breadcrumbs = this.getBreadcrumbs(url);
        if (url.startsWith('/users') || url.startsWith('/permissions')) this.openGroups['auth'] = true;
        if (url.startsWith('/articles')) this.openGroups['articles'] = true;
        if (url.startsWith('/clients'))  this.openGroups['clients']  = true;
      })
    );

    this.subs.add(
      this.authService.userProfile$.subscribe(profile => {
        if (profile) {
          this.userName = profile.fullName ?? profile.email ?? '';
          this.userRole = profile.roleName ?? '';
          this.initials = this.buildInitials(this.userName || profile.email || 'U');
          this.cdr.markForCheck();
        }
      })
    );

    const url = this.router.url;
    this.breadcrumbs = this.getBreadcrumbs(url);
    if (url.startsWith('/users') || url.startsWith('/permissions')) this.openGroups['auth'] = true;
    if (url.startsWith('/articles')) this.openGroups['articles'] = true;
    if (url.startsWith('/clients'))  this.openGroups['clients']  = true;

    this.cdr.markForCheck();
    window.addEventListener('resize', this.resizeListener);
  }

  private getBreadcrumbs(url: string): { label: string; link?: string }[] {
    if (url.startsWith('/change-password/')) {
      return [{ label: 'Users', link: '/users' }, { label: 'Reset Password' }];
    }
    if (url.startsWith('/users/register'))    return [{ label: 'Users', link: '/users' }, { label: 'Register' }];
    if (url.startsWith('/users/deactivated')) return [{ label: 'Users', link: '/users' }, { label: 'Deactivated' }];
    if (url.startsWith('/users/deleted'))     return [{ label: 'Users', link: '/users' }, { label: 'Deleted' }];
    if (url.startsWith('/users/categories'))  return [{ label: 'Users', link: '/users' }, { label: 'Controles' }];
    if (url.startsWith('/users/roles'))       return [{ label: 'Users', link: '/roles' }, { label: 'Roles' }];
    if (url.startsWith('/users/'))            return [{ label: 'Users', link: '/users' }, { label: 'Profile' }];
    if (url.startsWith('/users'))             return [{ label: 'Users' }];

    if (url.startsWith('/articles/categories/deleted'))  return [{ label: 'Articles', link: '/articles/categories/deleted' }, { label: 'Deleted' }];
    if (url.startsWith('/articles/categories'))  return [{ label: 'Articles', link: '/articles/categories' }, { label: 'Categories' }];
    if (url.startsWith('/articles/deleted'))  return [{ label: 'Articles', link: '/articles' }, { label: 'Deleted' }];
    if (url.startsWith('/articles'))          return [{ label: 'Articles' }];

    if (url.startsWith('/clients/categories/deleted'))  return [{ label: 'Clients', link: '/clients/categories' }, { label: 'Deleted' }];
    if (url.startsWith('/clients/categories'))  return [{ label: 'Clients', link: '/clients/categories' }, { label: 'Categories' }];
    if (url.startsWith('/clients/deleted'))   return [{ label: 'Clients', link: '/clients' }, { label: 'Deleted' }];
    if (url.startsWith('/clients'))           return [{ label: 'Clients' }];

    if (url.startsWith('/permissions'))       return [{ label: 'Permissions' }];
    if (url.startsWith('/audit-log'))         return [{ label: 'Audit Log' }];
    if (url.startsWith('/profile'))           return [{ label: 'My Profile' }];
    if (url.startsWith('/change-password'))   return [{ label: 'Change Password' }];
    if (url.startsWith('/home'))              return [{ label: 'Home' }];

    return [{ label: 'Dashboard' }];
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
    this.subs.unsubscribe();
    document.body.style.overflow = '';
    window.removeEventListener('resize', this.resizeListener);
  }
}
