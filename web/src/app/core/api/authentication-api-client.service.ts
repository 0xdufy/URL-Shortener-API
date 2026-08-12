import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, finalize, tap } from 'rxjs';

import { AuthenticationStateService } from '../auth/authentication-state.service';
import { API_BASE_URL } from '../config/api-base-url.token';
import {
  SEND_BROWSER_CREDENTIALS,
  SEND_CSRF_TOKEN,
  SKIP_ACCESS_TOKEN,
} from './api-request-context';
import {
  AuthenticationSession,
  BrowserAuthenticationBootstrap,
  CredentialsRequest,
  CurrentAuthenticationSession,
} from './api.models';
import { normalizeApiBaseUrl } from './api-url';

@Injectable({ providedIn: 'root' })
export class AuthenticationApiClient {
  private readonly http = inject(HttpClient);
  private readonly authenticationState = inject(AuthenticationStateService);
  private readonly baseUrl = `${normalizeApiBaseUrl(inject(API_BASE_URL))}/auth`;

  bootstrap(): Observable<BrowserAuthenticationBootstrap> {
    return this.http
      .get<BrowserAuthenticationBootstrap>(`${this.baseUrl}/bootstrap`, {
        context: this.browserSessionContext(false),
      })
      .pipe(tap(({ csrfToken }) => this.authenticationState.acceptCsrfToken(csrfToken)));
  }

  register(request: CredentialsRequest): Observable<AuthenticationSession> {
    return this.http
      .post<AuthenticationSession>(`${this.baseUrl}/register`, request, {
        context: this.browserSessionContext(false),
      })
      .pipe(tap((session) => this.authenticationState.acceptSession(session)));
  }

  signIn(request: CredentialsRequest): Observable<AuthenticationSession> {
    return this.http
      .post<AuthenticationSession>(`${this.baseUrl}/sign-in`, request, {
        context: this.browserSessionContext(false),
      })
      .pipe(tap((session) => this.authenticationState.acceptSession(session)));
  }

  refresh(): Observable<AuthenticationSession> {
    return this.http
      .post<AuthenticationSession>(`${this.baseUrl}/refresh`, null, {
        context: this.browserSessionContext(true),
      })
      .pipe(tap((session) => this.authenticationState.acceptSession(session)));
  }

  current(): Observable<CurrentAuthenticationSession> {
    return this.http
      .get<CurrentAuthenticationSession>(`${this.baseUrl}/me`)
      .pipe(tap((session) => this.authenticationState.reconcileCurrentSession(session)));
  }

  signOut(): Observable<void> {
    return this.http
      .post<void>(`${this.baseUrl}/sign-out`, null, {
        context: this.browserSessionContext(true),
      })
      .pipe(finalize(() => this.authenticationState.markSignedOut()));
  }

  private browserSessionContext(includeCsrfToken: boolean): HttpContext {
    return new HttpContext()
      .set(SKIP_ACCESS_TOKEN, true)
      .set(SEND_BROWSER_CREDENTIALS, true)
      .set(SEND_CSRF_TOKEN, includeCsrfToken);
  }
}
