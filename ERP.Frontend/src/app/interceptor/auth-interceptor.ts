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

  const isRefreshCall = req.url.includes('/auth/refresh');

  const token = !isPublicCall ? auth.getAccessToken() : null;
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  // ← keep early return only for login/revoke, NOT refresh
  // refresh needs error handling too
  if (req.url.includes('/auth/login') || req.url.includes('/auth/revoke')) {
    return next(authReq);
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {

      // ── Server unreachable — don't logout if this is a refresh attempt ──
      if (error.status === 0) {
        if (!isRefreshCall && !serverDownDialogOpen) {   // ← guard added
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

        if (code === 'AUTH_019') { /* ... unchanged */ }
        if (code === 'AUTH_002') return throwError(() => error);
        if (code === 'AUTH_008') { auth.logout(); return throwError(() => error); }

        // ← guard: don't attempt refresh if this IS the refresh call
        if (!isRefreshCall) {
          const refreshToken = auth.getRefreshToken();
          if (!refreshToken) { auth.logout(); return throwError(() => error); }

          return auth.refresh({ refreshToken }).pipe(
            switchMap(response => next(req.clone({
              setHeaders: { Authorization: `Bearer ${response.accessToken}` }
            }))),
            catchError(refreshError => {
              auth.endSession();
              return throwError(() => refreshError);
            })
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
