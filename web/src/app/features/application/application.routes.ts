import { Routes } from '@angular/router';

export const APPLICATION_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./application-shell-placeholder.component').then(
        ({ ApplicationShellPlaceholderComponent }) => ApplicationShellPlaceholderComponent,
      ),
  },
];
