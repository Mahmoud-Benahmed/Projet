import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const path = route.routeConfig?.path ?? '';

  // 1️⃣ Not logged in
  if (!auth.isLoggedIn()) {
    return router.createUrlTree(['/login']);
  }

  const mustChangePassword = auth.getMustChangePassword();
  const userRole = auth.Role; // use a getter instead of property

  // 2️⃣ Force password change
  if (mustChangePassword && path !== 'must-change-password') {
    return router.createUrlTree(['/must-change-password']);
  }

  // 3️⃣ Prevent going back after password change
  if (!mustChangePassword && path === 'must-change-password') {
    return router.createUrlTree(['/complete-profile']);
  }

  // 4️⃣ Role-based access
  const requiredRoles = route.data['roles'] as string[] | undefined;

  if (requiredRoles?.length) {
    if (!userRole || !requiredRoles.includes(userRole)) {

      // Smart fallback
      if (userRole === 'SystemAdmin') {
        return router.createUrlTree(['/users']);
      }

      return router.createUrlTree(['/home']);
    }
  }

  return true;
};
