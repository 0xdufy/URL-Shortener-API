import { Injectable, computed, signal } from '@angular/core';

import {
  AuthenticatedUser,
  AuthenticationSession,
  CurrentAuthenticationSession,
} from '../api/api.models';

export type AuthenticationStateReason = 'initial' | 'authenticated' | 'signed-out' | 'unauthorized';

@Injectable({ providedIn: 'root' })
export class AuthenticationStateService {
  private readonly accessTokenValue = signal<string | null>(null);
  private readonly csrfTokenValue = signal<string | null>(null);
  private readonly accessTokenExpiresAtUtcValue = signal<string | null>(null);
  private readonly refreshSessionExpiresAtUtcValue = signal<string | null>(null);
  private readonly userValue = signal<AuthenticatedUser | null>(null);
  private readonly reasonValue = signal<AuthenticationStateReason>('initial');

  readonly accessToken = this.accessTokenValue.asReadonly();
  readonly csrfToken = this.csrfTokenValue.asReadonly();
  readonly accessTokenExpiresAtUtc = this.accessTokenExpiresAtUtcValue.asReadonly();
  readonly refreshSessionExpiresAtUtc = this.refreshSessionExpiresAtUtcValue.asReadonly();
  readonly user = this.userValue.asReadonly();
  readonly reason = this.reasonValue.asReadonly();
  readonly isAuthenticated = computed(
    () => this.accessTokenValue() !== null && this.userValue() !== null,
  );

  acceptSession(session: AuthenticationSession): void {
    this.accessTokenValue.set(session.accessToken);
    this.csrfTokenValue.set(session.csrfToken);
    this.accessTokenExpiresAtUtcValue.set(session.accessTokenExpiresAtUtc);
    this.refreshSessionExpiresAtUtcValue.set(session.refreshSessionExpiresAtUtc);
    this.userValue.set(session.user);
    this.reasonValue.set('authenticated');
  }

  acceptCsrfToken(csrfToken: string): void {
    this.csrfTokenValue.set(csrfToken);
  }

  reconcileCurrentSession(session: CurrentAuthenticationSession): void {
    this.userValue.set(session.user);
    this.refreshSessionExpiresAtUtcValue.set(session.refreshSessionExpiresAtUtc);
  }

  markUnauthorized(): void {
    if (this.reasonValue() === 'unauthorized' && !this.isAuthenticated()) {
      return;
    }

    this.clearCredentials();
    this.reasonValue.set('unauthorized');
  }

  markSignedOut(): void {
    this.clearCredentials();
    this.reasonValue.set('signed-out');
  }

  private clearCredentials(): void {
    this.accessTokenValue.set(null);
    this.csrfTokenValue.set(null);
    this.accessTokenExpiresAtUtcValue.set(null);
    this.refreshSessionExpiresAtUtcValue.set(null);
    this.userValue.set(null);
  }
}
