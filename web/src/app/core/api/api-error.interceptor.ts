import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { AuthenticationStateService } from '../auth/authentication-state.service';
import { API_BASE_URL } from '../config/api-base-url.token';
import { ApiError, ApiErrorDetail, ApiFailureKind } from './api-error';
import { CLIENT_REQUEST_ID_HEADER } from './api-correlation.interceptor';
import { SKIP_ACCESS_TOKEN } from './api-request-context';
import { isApiUrl } from './api-url';

interface ParsedErrorEnvelope {
  readonly traceId?: string;
  readonly code?: string;
  readonly message?: string;
  readonly details: readonly ApiErrorDetail[];
}

interface FailureClassification {
  readonly kind: ApiFailureKind;
  readonly code: string;
  readonly message: string;
  readonly isUserActionable: boolean;
}

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  if (!isApiUrl(request.url, inject(API_BASE_URL))) {
    return next(request);
  }

  const authenticationState = inject(AuthenticationStateService);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      const parsed = parseEnvelope(error.error);
      const classification = classifyFailure(error.status, parsed);
      const apiError = new ApiError({
        status: error.status,
        code: parsed.code ?? classification.code,
        message: parsed.message ?? classification.message,
        details: parsed.details,
        traceId: parsed.traceId,
        clientRequestId: request.headers.get(CLIENT_REQUEST_ID_HEADER) ?? undefined,
        retryAfterSeconds: parseRetryAfter(error.headers.get('Retry-After')),
        kind: classification.kind,
        isUserActionable: classification.isUserActionable,
      });

      if (
        error.status === 401 &&
        (!request.context.get(SKIP_ACCESS_TOKEN) || apiError.code === 'INVALID_SESSION')
      ) {
        authenticationState.markUnauthorized();
      }

      return throwError(() => apiError);
    }),
  );
};

function classifyFailure(status: number, parsed: ParsedErrorEnvelope): FailureClassification {
  if (status >= 500 && status <= 599) {
    return {
      kind: 'service',
      code: 'SERVICE_ERROR',
      message: 'The service could not complete the request. Try again later.',
      isUserActionable: false,
    };
  }

  switch (status) {
    case 0:
      return {
        kind: 'connectivity',
        code: 'CONNECTIVITY_ERROR',
        message: 'Unable to reach the service. Check your connection and try again.',
        isUserActionable: true,
      };
    case 400:
      return {
        kind:
          parsed.code === 'VALIDATION_ERROR' || parsed.details.length > 0
            ? 'validation'
            : 'unexpected',
        code: 'BAD_REQUEST',
        message: 'The request could not be completed.',
        isUserActionable: parsed.code === 'VALIDATION_ERROR' || parsed.details.length > 0,
      };
    case 401:
      return {
        kind: 'authentication',
        code: 'AUTHENTICATION_REQUIRED',
        message: 'Sign in to continue.',
        isUserActionable: true,
      };
    case 403:
      return {
        kind: 'authorization',
        code: 'FORBIDDEN',
        message: 'You do not have permission to perform this action.',
        isUserActionable: true,
      };
    case 404:
      return {
        kind: 'not-found',
        code: 'NOT_FOUND',
        message: 'The requested resource was not found.',
        isUserActionable: true,
      };
    case 409:
      return {
        kind: 'conflict',
        code: 'CONFLICT',
        message: 'The request conflicts with the current resource state.',
        isUserActionable: true,
      };
    case 410:
      return {
        kind: 'gone',
        code: 'GONE',
        message: 'The requested resource is no longer available.',
        isUserActionable: true,
      };
    case 429:
      return {
        kind: 'rate-limited',
        code: 'RATE_LIMITED',
        message: 'Too many requests. Wait before trying again.',
        isUserActionable: true,
      };
    default:
      return {
        kind: 'unexpected',
        code: 'UNEXPECTED_ERROR',
        message: 'The request could not be completed.',
        isUserActionable: false,
      };
  }
}

function parseEnvelope(value: unknown): ParsedErrorEnvelope {
  if (!isRecord(value)) {
    return { details: [] };
  }

  const error = isRecord(value['error']) ? value['error'] : undefined;
  const rawDetails = error && Array.isArray(error['details']) ? error['details'] : [];
  const details = rawDetails.flatMap((detail): readonly ApiErrorDetail[] => {
    if (
      !isRecord(detail) ||
      typeof detail['field'] !== 'string' ||
      typeof detail['message'] !== 'string'
    ) {
      return [];
    }

    return [{ field: detail['field'], message: detail['message'] }];
  });

  return {
    traceId: typeof value['traceId'] === 'string' ? value['traceId'] : undefined,
    code: error && typeof error['code'] === 'string' ? error['code'] : undefined,
    message: error && typeof error['message'] === 'string' ? error['message'] : undefined,
    details,
  };
}

function parseRetryAfter(value: string | null): number | undefined {
  if (value === null) {
    return undefined;
  }

  const seconds = Number(value);
  if (Number.isFinite(seconds) && seconds >= 0) {
    return Math.ceil(seconds);
  }

  const retryAt = Date.parse(value);
  return Number.isNaN(retryAt) ? undefined : Math.max(0, Math.ceil((retryAt - Date.now()) / 1000));
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}
