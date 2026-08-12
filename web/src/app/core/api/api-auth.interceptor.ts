import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { AuthenticationStateService } from '../auth/authentication-state.service';
import { API_BASE_URL } from '../config/api-base-url.token';
import {
  SEND_BROWSER_CREDENTIALS,
  SEND_CSRF_TOKEN,
  SKIP_ACCESS_TOKEN,
} from './api-request-context';
import { isApiUrl } from './api-url';

export const apiAuthInterceptor: HttpInterceptorFn = (request, next) => {
  const apiBaseUrl = inject(API_BASE_URL);
  if (!isApiUrl(request.url, apiBaseUrl)) {
    return next(request);
  }

  const authenticationState = inject(AuthenticationStateService);
  let headers = request.headers;
  const accessToken = authenticationState.accessToken();
  const csrfToken = authenticationState.csrfToken();

  if (
    !request.context.get(SKIP_ACCESS_TOKEN) &&
    accessToken !== null &&
    !headers.has('Authorization')
  ) {
    headers = headers.set('Authorization', `Bearer ${accessToken}`);
  }

  if (request.context.get(SEND_CSRF_TOKEN) && csrfToken !== null) {
    headers = headers.set('X-XSRF-TOKEN', csrfToken);
  }

  return next(
    request.clone({
      headers,
      withCredentials: request.context.get(SEND_BROWSER_CREDENTIALS),
    }),
  );
};
