import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { LoadingService } from '../services/loading.service';

export const LoadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loader = inject(LoadingService);

    // Ignore translation files
  if (req.url.includes('/assets/i18n/')) {
    return next(req);
  }

  loader.show();

  return next(req).pipe(
    finalize(() => loader.hide())
  );
};
