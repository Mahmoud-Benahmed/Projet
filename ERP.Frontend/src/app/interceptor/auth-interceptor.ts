import { inject } from "@angular/core";
import { catchError, switchMap, throwError } from "rxjs";
import { AuthService } from "../services/auth/auth.service";
import { Router } from "@angular/router";
import { MatDialog } from "@angular/material/dialog";
import { HttpErrorResponse, HttpInterceptorFn } from "@angular/common/http";
import { ModalComponent } from "../components/modal/modal";
import { TranslateService } from "@ngx-translate/core";

let serverDownDialogOpen = false;

export const AuthInterceptor: HttpInterceptorFn = (req, next) => {
  const auth      = inject(AuthService);
  const dialog    = inject(MatDialog);
  const router    = inject(Router);
  const translate = inject(TranslateService);

  const t = (code: string, fallback?: string): string => {
    const key = `ERRORS.${code}`;
    const msg = translate.instant(key);
    return msg === key ? (fallback ?? msg) : msg;
  };

  const td = (key: string, fallback: string): string => {
    const full = `DIALOG.${key}`;
    const msg = translate.instant(full);
    return msg === full ? fallback : msg;
  };

  const isPublicCall = req.url.includes('/auth/refresh')
                    || req.url.includes('/auth/revoke')
                    || req.url.includes('/auth/login');

  const token = !isPublicCall ? auth.getAccessToken() : null;
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  if (isPublicCall) return next(authReq);

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {

      // ── Server unreachable ─────────────────────────────────────────────
      if (error.status === 0) {
        if (!serverDownDialogOpen) {
          serverDownDialogOpen = true;
          dialog.open(ModalComponent, {
            width: '400px',
            data: {
              title:       td('SERVER_UNREACHABLE', 'Server Unreachable'),
              message:     t('SERVER_UNREACHABLE'),
              confirmText: td('OK', 'OK'),
              showCancel:  false,
              icon:        'cloud_off',
              iconColor:   'warn'
            }
          }).afterClosed().subscribe(() => serverDownDialogOpen = false);
        }
        auth.logout();
        return throwError(() => error);
      }

      // ── Rate limit ─────────────────────────────────────────────────────
      if (error.status === 429) {
        const retryAfter = error.headers.get('Retry-After');
        dialog.open(ModalComponent, {
          width: '400px',
          data: {
            title:       td('RATE_LIMIT', 'Rate Limit Reached'),
            message:     t('RATE_LIMIT', `Too many requests. Please wait ${retryAfter ?? 60}s.`),
            confirmText: td('OK', 'OK'),
            showCancel:  false,
            icon:        'timer',
            iconColor:   'warn'
          }
        }).afterClosed().subscribe(() => router.navigate(['/home']));
        return throwError(() => error);
      }

      // ── Forbidden ──────────────────────────────────────────────────────
      if (error.status === 403) {
        const code = error.error?.code;
        const isInactive = code === 'AUTH_003';
        dialog.open(ModalComponent, {
          width: '400px',
          data: {
            title:       isInactive ? td('ACCOUNT_DEACTIVATED', 'Account Deactivated') : td('ACCESS_DENIED', 'Access Denied'),
            message:     t(code, error.error?.message ?? 'You do not have permission.'),
            confirmText: td('OK', 'OK'),
            showCancel:  false,
            icon:        isInactive ? 'person_off' : 'block',
            iconColor:   'danger'
          }
        }).afterClosed().subscribe(() => {
          isInactive ? auth.logout() : router.navigate(['/home']);
        });
        return throwError(() => error);
      }

      // ── Unauthorized ───────────────────────────────────────────────────
      if (error.status === 401) {
        const code = error.error?.code;

        if (code === 'AUTH_009') {
          dialog.open(ModalComponent, {
            width: '400px',
            data: {
              title:       td('SESSION_EXPIRED', 'Session Expired'),
              message:     t('AUTH_009'),
              confirmText: td('OK', 'OK'),
              showCancel:  false,
              icon:        'person_off',
              iconColor:   'danger'
            }
          }).afterClosed().subscribe(() => auth.logout());
          return throwError(() => error);
        }

        if (code === 'AUTH_002') return throwError(() => error);

        if (code === 'AUTH_008') {
          auth.logout();
          return throwError(() => error);
        }

        if (!req.url.includes('revoke') && !req.url.includes('refresh')) {
          const refreshToken = auth.getRefreshToken();
          if (!refreshToken) { auth.logout(); return throwError(() => error); }

          return auth.refresh({ refreshToken }).pipe(
            switchMap(response => next(req.clone({
              setHeaders: { Authorization: `Bearer ${response.accessToken}` }
            }))),
            catchError(refreshError => { auth.logout(); return throwError(() => refreshError); })
          );
        }

        auth.logout();
        return throwError(() => error);
      }

      // ── Not found ──────────────────────────────────────────────────────
      if (error.status === 404) {
        router.navigate(['/home']);
        return throwError(() => error); // ← was missing the return
      }

      // ── Gateway errors ─────────────────────────────────────────────────
      if (error.status === 503 || error.status === 502 || error.status === 504) {
        if (!serverDownDialogOpen) {
          serverDownDialogOpen = true;
          dialog.open(ModalComponent, {
            width: '400px',
            data: {
              title:       td('SERVICE_UNAVAILABLE', 'Service Unavailable'),
              message:     t('SERVICE_UNAVAILABLE'),
              confirmText: td('OK', 'OK'),
              showCancel:  false,
              icon:        'cloud_off',
              iconColor:   'warn'
            }
          }).afterClosed().subscribe(() => {
            serverDownDialogOpen = false;
            router.navigate(['/home']);
          });
        }
        return throwError(() => error);
      }

      return throwError(() => error);
    })
  );
};
