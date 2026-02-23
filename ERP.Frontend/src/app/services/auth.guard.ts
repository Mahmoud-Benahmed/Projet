import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn) {
    router.navigate(['/login']);
    return false;
  }

  const requiredRoles = route.data['roles'] as string[];
  if (requiredRoles && requiredRoles.length > 0) {
    const userRole = auth.Role;
    if (!userRole || !requiredRoles.includes(userRole)) {
      if (userRole === 'SystemAdmin') {
        router.navigate(['/users']);
      } else {
        router.navigate(['/home']);
      }
      return false;
    }
  }

  return true;
};
