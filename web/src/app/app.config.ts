import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { environment } from '../environments/environment';
import { routes } from './app.routes';
import { apiAuthInterceptor } from './core/api/api-auth.interceptor';
import { apiCorrelationInterceptor } from './core/api/api-correlation.interceptor';
import { apiErrorInterceptor } from './core/api/api-error.interceptor';
import { API_BASE_URL } from './core/config/api-base-url.token';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([apiAuthInterceptor, apiCorrelationInterceptor, apiErrorInterceptor]),
    ),
    { provide: API_BASE_URL, useValue: environment.apiBaseUrl },
  ],
};
