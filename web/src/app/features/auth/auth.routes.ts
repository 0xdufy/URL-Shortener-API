import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./authentication-layout.component').then(
        ({ AuthenticationLayoutComponent }) => AuthenticationLayoutComponent,
      ),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'sign-in' },
      {
        path: 'sign-in',
        title: 'Sign in | Shortly',
        data: { mode: 'sign-in' },
        loadComponent: loadAuthenticationForm,
      },
      {
        path: 'register',
        title: 'Create account | Shortly',
        data: { mode: 'register' },
        loadComponent: loadAuthenticationForm,
      },
      { path: '**', redirectTo: 'sign-in' },
    ],
  },
];

function loadAuthenticationForm() {
  return import('./authentication-form.component').then(
    ({ AuthenticationFormComponent }) => AuthenticationFormComponent,
  );
}
