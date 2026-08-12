import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { API_BASE_URL } from '../config/api-base-url.token';
import { isApiUrl } from './api-url';

export const CLIENT_REQUEST_ID_HEADER = 'X-Client-Request-ID';

export const apiCorrelationInterceptor: HttpInterceptorFn = (request, next) => {
  if (
    !isApiUrl(request.url, inject(API_BASE_URL)) ||
    request.headers.has(CLIENT_REQUEST_ID_HEADER)
  ) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: { [CLIENT_REQUEST_ID_HEADER]: createClientRequestId() },
    }),
  );
};

function createClientRequestId(): string {
  if (typeof globalThis.crypto?.randomUUID === 'function') {
    return globalThis.crypto.randomUUID();
  }

  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;
}
