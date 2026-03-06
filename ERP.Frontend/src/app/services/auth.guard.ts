import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const path = route.routeConfig?.path ?? '';

  // Not logged in
  if (!auth.isLoggedIn()) {
    return router.createUrlTree(['/login']);
  }

  const mustChangePassword = auth.getMustChangePassword();
  const userRole = auth.Role; // use a getter instead of property

  // Force password change
  if (mustChangePassword && path !== 'must-change-password') {
    return router.createUrlTree(['/must-change-password']);
  }

  // Role-based access
  const requiredRoles = route.data['roles'] as string[] | undefined;

  if (requiredRoles?.length) {
    if (!userRole || !requiredRoles.includes(userRole)) {

      switch(userRole){
        case 'SystemAdmin':
          return router.createUrlTree(['/users']);
        case 'StockManager':
          return router.createUrlTree(['/articles']);
        default:
          return router.createUrlTree(['/home']);
      }

    }
  }

  return true;
};
