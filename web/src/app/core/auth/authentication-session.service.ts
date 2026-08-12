import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, switchMap } from 'rxjs';

import { AuthenticationApiClient } from '../api/authentication-api-client.service';
import { AuthenticationStateService } from './authentication-state.service';

@Injectable({ providedIn: 'root' })
export class AuthenticationSessionService {
  private readonly authenticationApi = inject(AuthenticationApiClient);
  private readonly authenticationState = inject(AuthenticationStateService);

  ensureAuthenticated(): Observable<boolean> {
    if (this.authenticationState.isAuthenticated()) {
      return of(true);
    }

    if (this.authenticationState.reason() !== 'initial') {
      return of(false);
    }

    return this.authenticationApi.bootstrap().pipe(
      switchMap(() => this.authenticationApi.refresh()),
      switchMap(() => this.authenticationApi.current()),
      map(() => true),
      catchError(() => {
        this.authenticationState.markUnauthorized();
        return of(false);
      }),
    );
  }
}
