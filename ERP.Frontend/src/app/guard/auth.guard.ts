import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService, PRIVILEGES } from '../services/auth/auth.service';

export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  const path   = route.routeConfig?.path ?? '';

  // ── Not logged in
  if (!auth.isLoggedIn()) {
    return router.createUrlTree(['/login']);
  }

  // ── Force password change
  if (auth.getMustChangePassword() && path !== 'must-change-password') {
    return router.createUrlTree(['/must-change-password']);
  }

  // ── Privilege-based access
  const requiredPrivileges = route.data['privileges'] as string[] | undefined;

  if (requiredPrivileges?.length) {
    const hasAccess = requiredPrivileges.some(p => auth.hasPrivilege(p));

    if (!hasAccess) {
      // Redirect to the most relevant page based on what the user CAN access
      if (auth.hasPrivilege(PRIVILEGES.VIEW_USERS))    return router.createUrlTree(['/users']);
      if (auth.hasPrivilege(PRIVILEGES.VIEW_ARTICLES)) return router.createUrlTree(['/articles']);
      if (auth.hasPrivilege(PRIVILEGES.VIEW_CLIENTS))  return router.createUrlTree(['/clients']);
      return router.createUrlTree(['/home']);
    }
  }

  return true;
};
