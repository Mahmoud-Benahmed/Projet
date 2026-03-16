import { inject } from "@angular/core";
import { catchError, switchMap, throwError } from "rxjs";
import { AuthService } from "../services/auth.service";
import { Router } from "@angular/router";
import { MatDialog } from "@angular/material/dialog";
import { HttpErrorResponse, HttpInterceptorFn } from "@angular/common/http";
import { ModalComponent } from "../components/modal/modal";

// Remove: import { routes } from "../app.routes";

let serverDownDialogOpen = false;

export const AuthInterceptor: HttpInterceptorFn = (req, next) => {
  const auth   = inject(AuthService);
  const dialog = inject(MatDialog);
  const router = inject(Router);

  const token = auth.getAccessToken();
  const authReq = token ? req.clone({
    setHeaders: { Authorization: `Bearer ${token}` }
  }) : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {

      // ── Server unreachable ─────────────────────────────────────────────
      if (error.status === 0) {  // ← was missing
        if (!serverDownDialogOpen) {
          serverDownDialogOpen = true;
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
          }).afterClosed().subscribe(() => {
            serverDownDialogOpen = false;
          });
        }
        auth.logout();
        return throwError(() => error);
      }

      // ── Rate limit ─────────────────────────────────────────────────────
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

      // ── Forbidden ──────────────────────────────────────────────────────
      if (error.status === 403) {
        const code = error.error?.code;
        const isInactive = code === 'AUTH_003';

        dialog.open(ModalComponent, {
          width: '400px',
          data: {
            title: isInactive ? 'Account Deactivated' : 'Access Denied',
            message: error.error?.message ?? 'You do not have permission to perform this action.',
            confirmText: 'OK',
            showCancel: false,
            icon: isInactive ? 'person_off' : 'block',
            iconColor: 'danger'
          }
        }).afterClosed().subscribe(() => {
          if (isInactive) {
            auth.logout();
          } else {
            router.navigate(['/home']);
          }
        });
        return throwError(() => error);
      }

      // ── Unauthorized ───────────────────────────────────────────────────
      if (error.status === 401) {
        const code = error.error?.code;

        // User deleted or inactive — session invalid
        if (code === 'AUTH_009' || code === 'AUTH_003') {
          dialog.open(ModalComponent, {
            width: '400px',
            data: {
              title: 'Session Expired',
              message: 'Your session is no longer valid. You will be logged out.',
              confirmText: 'OK',
              showCancel: false,
              icon: 'person_off',
              iconColor: 'danger'
            }
          }).afterClosed().subscribe(() => auth.logout());
          return throwError(() => error);
        }

        // Wrong current password — user is authenticated, just typed wrong
        if (code === 'AUTH_002') {
          return throwError(() => error);
        }

        // Security violation
        if (code === 'AUTH_008') {
          auth.logout();
          return throwError(() => error);
        }

        // Token expired — attempt refresh
        if (!req.url.includes('revoke') && !req.url.includes('refresh')) {
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
