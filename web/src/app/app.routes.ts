import { Routes } from '@angular/router';

import { authenticationGuard } from './core/auth/authentication.guard';

export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () =>
      import('./features/auth/auth.routes').then(({ AUTH_ROUTES }) => AUTH_ROUTES),
  },
  {
    path: 'app',
    canActivate: [authenticationGuard],
    loadChildren: () =>
      import('./features/application/application.routes').then(
        ({ APPLICATION_ROUTES }) => APPLICATION_ROUTES,
      ),
  },
  { path: '', pathMatch: 'full', redirectTo: 'app' },
  { path: '**', redirectTo: 'app' },
];
