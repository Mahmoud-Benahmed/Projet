import { inject } from "@angular/core";
import { catchError, switchMap, throwError } from "rxjs";
import { AuthService } from "../services/auth.service";
import { Router } from "@angular/router";
import { MatDialog } from "@angular/material/dialog";
import { HttpErrorResponse, HttpInterceptorFn } from "@angular/common/http";
import { ModalComponent } from "../components/modal/modal";

export const AuthInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const dialog = inject(MatDialog);

  const token = auth.getAccessToken();
  const authReq = token ? req.clone({
    setHeaders: { Authorization: `Bearer ${token}` }
  }) : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {

      // ── Server unreachable
      if (error.status === 0) {
        dialog.open(ModalComponent, {
          width: '400px',
          data: {
            title: 'Server Unreachable',
            message: 'Unable to connect to the server. Check your connection or try again later.',
            confirmText: 'OK',
            showCancel: false,
            icon: 'cloud_off',
            iconColor: 'warn'
          }
        });
        auth.logout();
        return throwError(() => error);
      }

      // ── Rate limit
      if (error.status === 429) {
        const retryAfter = error.headers.get('Retry-After');
        const content = error.error?.content
          ?? `Too many requests. Please wait ${retryAfter ?? 60} seconds before retrying.`;

        dialog.open(ModalComponent, {
          width: '400px',
          data: {
            title: 'Rate Limit Reached',
            message: content,
            confirmText: 'OK',
            showCancel: false,
            icon: 'timer',
            iconColor: 'warn'
          }
        });
        return throwError(() => error);
      }

      // ── Forbidden
      if (error.status === 403) {
          const code = error.error?.code;

          const isInactive = code === 'USER_INACTIVE';

          dialog.open(ModalComponent, {
            width: '400px',
            data: {
              title: isInactive ? 'Account Deactivated' : 'Access Denied',
              message: error.error?.content ?? 'You do not have permission to perform this action.',
              confirmText: 'OK',
              showCancel: false,
              icon: isInactive ? 'person_off' : 'block',
              iconColor: 'danger'
            }
          }).afterClosed().subscribe(() => {
            if (isInactive) auth.logout();
          });
          return throwError(()=> error);
      }
      
      if (error.status === 401)
      {
          const code = error.error?.code;

          // ── User no longer exists
          if (code === 'USER_NOT_FOUND' || code === 'USER_INACTIVE') {
            dialog.open(ModalComponent, {
              width: '400px',
              data: {
                title: 'Session Expired',
                message: error.error?.content ?? 'Your session is no longer valid. You will be logged out.',
                confirmText: 'OK',
                showCancel: false,
                icon: 'person_off',
                iconColor: 'danger'
              }
            }).afterClosed().subscribe(() => auth.logout());
            return throwError(() => error);
          }

          // ── Token expired — attempt refresh
          if (!req.url.includes('change-password')
            && !req.url.includes('revoke')
            && !req.url.includes('refresh'))
          {
            const refreshToken = auth.getRefreshToken();

            if (!refreshToken) {
              auth.logout();
              return throwError(() => error);
            }

            return auth.refresh({ refreshToken }).pipe(
              switchMap((response) => {
                const retryReq = req.clone({
                  setHeaders: { Authorization: `Bearer ${response.accessToken}` }
                });
                return next(retryReq);
              }),
              catchError((refreshError) => {
                auth.logout();
                return throwError(() => refreshError);
              })
            );
          }

          auth.logout();
          return throwError(() => error);
        }
        return throwError(() => error);
    })
  );
};
