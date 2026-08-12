import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./auth-placeholder.component').then(
        ({ AuthPlaceholderComponent }) => AuthPlaceholderComponent,
      ),
  },
];
