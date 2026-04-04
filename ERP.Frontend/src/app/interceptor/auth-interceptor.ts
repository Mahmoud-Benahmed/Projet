import { inject } from "@angular/core";
import { catchError, switchMap, throwError, of, take } from "rxjs";
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
      }).afterClosed().subscribe(() => onClose());
    });
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
          openDialog(
            'DIALOG.SERVER_UNREACHABLE', 'ERRORS.SERVER_UNREACHABLE',
            'cloud_off', 'warn',
            () => { serverDownDialogOpen = false; },
            'Server Unreachable', 'Unable to connect to the server.'
          );
        }
        auth.logout();
        return throwError(() => error);
      }

      // ── Rate limit ─────────────────────────────────────────────────────
      if (error.status === 429) {
        const retryAfter = error.headers.get('Retry-After');
        openDialog(
          'DIALOG.RATE_LIMIT', 'ERRORS.RATE_LIMIT',
          'timer', 'warn',
          () => router.navigate(['/home']),
          'Rate Limit Reached', `Too many requests. Please wait ${retryAfter ?? 60}s.`
        );
        return throwError(() => error);
      }

      // ── Forbidden ──────────────────────────────────────────────────────
      if (error.status === 403) {
        const code = error.error?.code;
        const isInactive = code === 'AUTH_003';
        const titleKey   = isInactive ? 'DIALOG.ACCOUNT_DEACTIVATED' : 'DIALOG.ACCESS_DENIED';
        const messageKey = `ERRORS.${code}`;
        openDialog(
          titleKey, messageKey,
          isInactive ? 'person_off' : 'block', 'danger',
          () => isInactive ? auth.logout() : router.navigate(['/home']),
          isInactive ? 'Account Deactivated' : 'Access Denied',
          error.error?.message ?? 'You do not have permission.'
        );
        return throwError(() => error);
      }

      // ── Unauthorized ───────────────────────────────────────────────────
      if (error.status === 401) {
        const code = error.error?.code;

        if (code === 'AUTH_019') {
          openDialog(
            'DIALOG.SESSION_EXPIRED', 'ERRORS.AUTH_019',
            'person_off', 'danger',
            () => auth.logout(),
            'Session Expired', 'Your account no longer exists.'
          );
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
