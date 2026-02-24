import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const path = route.routeConfig?.path ?? '';

  if (!auth.isLoggedIn) {
    router.navigate(['/login']);
    return false;
  }

  // Step 1 — must change password first
  if (auth.mustChangePassword) {
    if (path !== 'must-change-password') {
      router.navigate(['/must-change-password']);
      return false;
    }
    return true;
  }

  // Step 2 — block going back to must-change-password once done
  if (path === 'must-change-password') {
    router.navigate(['/complete-profile']);
    return false;
  }

  // Step 3 — complete-profile is always accessible after password change
  if (path === 'complete-profile') {
    return true;
  }

  // Step 4 — role-based access
  const requiredRoles = route.data['roles'] as string[];
  if (requiredRoles?.length > 0) {
    const userRole = auth.Role;
    if (!userRole || !requiredRoles.includes(userRole)) {
      router.navigate([userRole === 'SystemAdmin' ? '/users' : '/home']);
      return false;
    }
  }

  return true;
};
