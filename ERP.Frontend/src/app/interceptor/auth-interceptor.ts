import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { MatDialog } from '@angular/material/dialog';
import { ModalComponent } from '../components/modal/modal';

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

      // server unreachable
      if (error.status === 0) {
        dialog.open(ModalComponent, {
          width: '400px',
          data: {
            title: 'Serveur inaccessible',
            message: 'Impossible de se connecter au serveur. Vérifiez votre connexion ou réessayez plus tard.',
            confirmText: 'OK',
            showCancel: false,
            icon: 'cloud_off',
            iconColor: 'warn'
          }
        });
        auth.logout();
        return throwError(() => error);
      }

      // token expired — attempt refresh
      if (error.status === 401
          && !req.url.includes('change-password')
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
            // storeTokens is already called inside auth.refresh() via tap()
            // so just retry the original request
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

      return throwError(() => error);
    })
  );
};
