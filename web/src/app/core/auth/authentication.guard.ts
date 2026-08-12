import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';

import { AuthenticationSessionService } from './authentication-session.service';

export const authenticationGuard: CanActivateFn = (_route, state) => {
  const router = inject(Router);

  return inject(AuthenticationSessionService)
    .ensureAuthenticated()
    .pipe(
      map((authenticated) =>
        authenticated
          ? true
          : router.createUrlTree(['/auth/sign-in'], {
              queryParams: { returnUrl: state.url },
            }),
      ),
    );
};
