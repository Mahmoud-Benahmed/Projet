import { inject } from "@angular/core";
import { catchError, switchMap, throwError, of, take } from "rxjs";
import { AuthService } from "../services/auth/auth.service";
import { Router } from "@angular/router";
import { MatDialog } from "@angular/material/dialog";
import { HttpErrorResponse, HttpInterceptorFn } from "@angular/common/http";
import { ModalComponent } from "../components/modal/modal";
import { TranslateService } from "@ngx-translate/core";

let serverDownDialogOpen = false;
let authErrorDialogOpen = false; // Add this to prevent multiple dialogs

export const AuthInterceptor: HttpInterceptorFn = (req, next) => {
  const auth      = inject(AuthService);
  const dialog    = inject(MatDialog);
  const router    = inject(Router);
  const translate = inject(TranslateService);

  // ✅ Async translation — guaranteed to have the value
  const openDialog = (
    titleKey: string,
    messageKey: string,
    icon: string,
    iconColor: string,
    onClose: () => void,
    titleFallback = titleKey,
    messageFallback = messageKey
  ) => {
    translate.get([titleKey, messageKey, 'DIALOG.OK']).pipe(take(1)).subscribe(t => {
      const title   = t[titleKey]   === titleKey   ? titleFallback   : t[titleKey];
      const message = t[messageKey] === messageKey ? messageFallback : t[messageKey];
      const ok      = t['DIALOG.OK'] === 'DIALOG.OK' ? 'OK' : t['DIALOG.OK'];

      dialog.open(ModalComponent, {
        width: '400px',
        data: { title, message, confirmText: ok, showCancel: false, icon, iconColor }
      }).afterClosed().subscribe(() => {
        onClose();
        authErrorDialogOpen = false; // Reset dialog flag
      });
    });
  };

  const isPublicCall = req.url.includes('/auth/refresh')
                    || req.url.includes('/auth/revoke')
                    || req.url.includes('/auth/login');

  const isRefreshCall = req.url.includes('/auth/refresh');

  const token = !isPublicCall ? auth.getAccessToken() : null;
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  // Skip interceptor for login and revoke
  if (req.url.includes('/auth/login') || req.url.includes('/auth/revoke')) {
    return next(authReq);
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {

      // ── Server unreachable ─────────────────────────────────────────────
      if (error.status === 0) {
        if (!isRefreshCall && !serverDownDialogOpen) {
          serverDownDialogOpen = true;
          openDialog(
            'DIALOG.SERVER_UNREACHABLE', 'ERRORS.SERVER_UNREACHABLE',
            'cloud_off', 'warn',
            () => { serverDownDialogOpen = false; },
            'Server Unreachable', 'Unable to connect to the server.'
          );
          auth.endSession();
        }
        return throwError(() => error);
      }

      // ── Unauthorized ───────────────────────────────────────────────────
      if (error.status === 401) {
        const code = error.error?.code;
        const message = error.error?.message;

        // Handle specific error codes from Gateway
        switch (code) {
          case 'AUTH_006': // Authentication required / Invalid token
            console.log('[AuthInterceptor] Invalid or expired token');
            
            // Don't show modal for refresh token calls to avoid loops
            if (!isRefreshCall && !authErrorDialogOpen) {
              authErrorDialogOpen = true;
              openDialog(
                'SESSION.EXPIRED_TITLE', 'SESSION.EXPIRED_MESSAGE',
                'warning', 'warn',
                () => {
                  auth.logout();
                  router.navigate(['/login']);
                },
                'Session Expired', 'Your session has expired. Please login again.'
              );
            } else if (!isRefreshCall) {
              // If dialog already open, just logout
              auth.logout();
              router.navigate(['/login']);
            }
            return throwError(() => error);

          case 'AUTH_007': // Forbidden - no permission
            if (!authErrorDialogOpen) {
              authErrorDialogOpen = true;
              openDialog(
                'ERRORS.ACCESS_DENIED_TITLE', 'ERRORS.ACCESS_DENIED_MESSAGE',
                'lock', 'warn',
                () => {
                  router.navigate(['/home']);
                },
                'Access Denied', 'You do not have permission to access this resource.'
              );
            }
            return throwError(() => error);

          case 'AUTH_002': // Invalid credentials (keep your existing logic)
            return throwError(() => error);

          case 'AUTH_008': // User not found or deactivated
            if (!authErrorDialogOpen) {
              authErrorDialogOpen = true;
              openDialog(
                'ACCOUNT.INVALID_TITLE', 'ACCOUNT.INVALID_MESSAGE',
                'error', 'warn',
                () => {
                  auth.logout();
                  router.navigate(['/login']);
                },
                'Account Issue', 'Your account is no longer active or has been deleted.'
              );
            }
            return throwError(() => error);

          default:
            // Handle other 401 errors - try to refresh token
            if (!isRefreshCall) {
              const refreshToken = auth.getRefreshToken();
              if (!refreshToken) { 
                auth.logout(); 
                return throwError(() => error); 
              }

              return auth.refresh({ refreshToken }).pipe(
                switchMap(response => {
                  // Retry the original request with new token
                  return next(req.clone({
                    setHeaders: { Authorization: `Bearer ${response.accessToken}` }
                  }));
                }),
                catchError(refreshError => {
                  // Refresh failed - session is truly expired
                  if (!authErrorDialogOpen) {
                    authErrorDialogOpen = true;
                    openDialog(
                      'SESSION.EXPIRED_TITLE', 'SESSION.EXPIRED_MESSAGE',
                      'warning', 'warn',
                      () => {
                        auth.endSession();
                        router.navigate(['/login']);
                      },
                      'Session Expired', 'Your session has expired. Please login again.'
                    );
                  }
                  return throwError(() => refreshError);
                })
              );
            }

            // If this was a refresh call that failed, logout
            auth.logout();
            router.navigate(['/login']);
            return throwError(() => error);
        }
      }

      // ── Forbidden (non-401) ─────────────────────────────────────────────
      if (error.status === 403) {
        if (!authErrorDialogOpen) {
          authErrorDialogOpen = true;
          openDialog(
            'ERRORS.ACCESS_DENIED_TITLE', 'ERRORS.ACCESS_DENIED_MESSAGE',
            'lock', 'warn',
            () => {
              router.navigate(['/home']);
            },
            'Access Denied', 'You do not have permission to access this resource.'
          );
        }
        return throwError(() => error);
      }

      // ── Not found ──────────────────────────────────────────────────────
      if (error.status === 404) {
        router.navigate(['/home']);
        return throwError(() => error);
      }

      // ── Gateway errors ─────────────────────────────────────────────────
      if (error.status === 503 || error.status === 502 || error.status === 504) {
        if (!serverDownDialogOpen) {
          serverDownDialogOpen = true;
          openDialog(
            'DIALOG.SERVICE_UNAVAILABLE', 'ERRORS.SERVICE_UNAVAILABLE',
            'cloud_off', 'warn',
            () => { serverDownDialogOpen = false; router.navigate(['/home']); },
            'Service Unavailable', 'The service is temporarily unavailable.'
          );
        }
        return throwError(() => error);
      }

      return throwError(() => error);
    })
  );
};