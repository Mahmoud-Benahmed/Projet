import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AuthInterceptor } from './interceptor/auth-interceptor';
import { LoadingInterceptor } from './interceptor/loading-interceptor';
import { registerLocaleData } from '@angular/common';
import localeFrMA from '@angular/common/locales/fr-MA';
import localeFrTN from '@angular/common/locales/fr-TN';
import localeEnUS from '@angular/common/locales/en';
import localeFrFR from '@angular/common/locales/fr';
import localeEnGB from '@angular/common/locales/en-GB';

registerLocaleData(localeFrMA);
registerLocaleData(localeFrTN);
registerLocaleData(localeEnUS);
registerLocaleData(localeFrFR);
registerLocaleData(localeEnGB);


export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([LoadingInterceptor, AuthInterceptor]))
  ]
};
