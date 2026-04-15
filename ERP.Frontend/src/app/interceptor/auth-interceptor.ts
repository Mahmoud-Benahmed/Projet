import { inject } from "@angular/core";
import { catchError, switchMap, throwError, take } from "rxjs";
import { AuthService } from "../services/auth/auth.service";
import { Router } from "@angular/router";
import { MatDialog } from "@angular/material/dialog";
import { HttpErrorResponse, HttpInterceptorFn } from "@angular/common/http";
import { ModalComponent } from "../components/modal/modal";
import { TranslateService } from "@ngx-translate/core";

let serverDownDialogOpen = false;
let authErrorDialogOpen = false;

export const AuthInterceptor: HttpInterceptorFn = (req, next) => {
  const auth      = inject(AuthService);
  const dialog    = inject(MatDialog);
  const router    = inject(Router);
  const translate = inject(TranslateService);

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

  const forceLogout = (titleKey: string, messageKey: string, icon: string,
                       titleFallback: string, messageFallback: string) => {
    auth.endSession();
    router.navigate(['/login']);
    if (!authErrorDialogOpen) {
      authErrorDialogOpen = true;
      openDialog(titleKey, messageKey, icon, 'warn',
        () => { authErrorDialogOpen = false; },
        titleFallback, messageFallback
      );
    }
  };

  const isPublicCall  = req.url.includes('/auth/refresh')
                     || req.url.includes('/auth/revoke')
                     || req.url.includes('/auth/login');
  const isRefreshCall = req.url.includes('/auth/refresh');

  const token   = !isPublicCall ? auth.getAccessToken() : null;
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  if (req.url.includes('/auth/login') || req.url.includes('/auth/revoke')) {
    return next(authReq);
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {

      // ── Server unreachable ───────────────────────────────────────────
      if (error.status === 0) {
        if (!isRefreshCall && !serverDownDialogOpen) {
          serverDownDialogOpen = true;
          auth.endSession();
          router.navigate(['/login']);
          openDialog(
            'DIALOG.SERVER_UNREACHABLE', 'ERRORS.SERVER_UNREACHABLE',
            'cloud_off', 'warn',
            () => { serverDownDialogOpen = false; },
            'Server Unreachable', 'Unable to connect to the server.'
          );
        }
        return throwError(() => error);
      }

      // ── Unauthorized ─────────────────────────────────────────────────
      if (error.status === 401) {
        const code = error.error?.code;

        // Specific codes that should immediately logout
        if (code === 'AUTH_006' || code === 'AUTH_008' || code === 'AUTH_018') {
          forceLogout(
            'SESSION.EXPIRED_TITLE', 'SESSION.EXPIRED_MESSAGE',
            'warning', 'Session Expired',
            'Your session has expired. Please login again.'
          );
          return throwError(() => error);
        }

        if (code === 'AUTH_002') return throwError(() => error); // invalid credentials – let component handle

        if (code === 'AUTH_007' || code === 'AUTH_019') {
          if (!authErrorDialogOpen) {
            authErrorDialogOpen = true;
            openDialog(
              'ERRORS.ACCESS_DENIED_TITLE', 'ERRORS.ACCESS_DENIED_MESSAGE',
              'lock', 'warn',
              () => { router.navigate(['/home']); },
              'Access Denied', 'You do not have permission to access this resource.'
            );
          }
          return throwError(() => error);
        }

        // Unknown 401 – attempt refresh (unless this is already the refresh call)
        if (isRefreshCall) {
          forceLogout(
            'SESSION.EXPIRED_TITLE', 'SESSION.EXPIRED_MESSAGE',
            'warning', 'Session Expired',
            'Your session has expired. Please login again.'
          );
          return throwError(() => error);
        }

        const refreshToken = auth.getRefreshToken();
        if (!refreshToken) {
          forceLogout(
            'SESSION.EXPIRED_TITLE', 'SESSION.EXPIRED_MESSAGE',
            'warning', 'Session Expired',
            'Your session has expired. Please login again.'
          );
          return throwError(() => error);
        }

        return auth.refresh({ refreshToken }).pipe(
          switchMap(response =>
            next(authReq.clone({
              setHeaders: { Authorization: `Bearer ${response.accessToken}` }
            }))
          ),
          catchError(refreshError => {
            forceLogout(
              'SESSION.EXPIRED_TITLE', 'SESSION.EXPIRED_MESSAGE',
              'warning', 'Session Expired',
              'Your session has expired. Please login again.'
            );
            return throwError(() => refreshError);
          })
        );
      }

      // ── Forbidden (non-401) ──────────────────────────────────────────
      if (error.status === 403) {
        if (!authErrorDialogOpen) {
          authErrorDialogOpen = true;
          openDialog(
            'ERRORS.ACCESS_DENIED_TITLE', 'ERRORS.ACCESS_DENIED_MESSAGE',
            'lock', 'warn',
            () => { router.navigate(['/home']); },
            'Access Denied', 'You do not have permission to access this resource.'
          );
        }
        return throwError(() => error);
      }

      // ── Gateway errors ───────────────────────────────────────────────
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