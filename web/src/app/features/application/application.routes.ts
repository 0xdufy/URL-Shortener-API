import { Routes } from '@angular/router';

export const APPLICATION_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./application-shell.component').then(
        ({ ApplicationShellComponent }) => ApplicationShellComponent,
      ),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        title: 'Dashboard | Shortly',
        loadComponent: () =>
          import('./foundation-overview.component').then(
            ({ FoundationOverviewComponent }) => FoundationOverviewComponent,
          ),
      },
      {
        path: 'links',
        title: 'Links | Shortly',
        data: {
          title: 'Links',
          description: 'Create, organize, and manage short links from one place.',
        },
        loadComponent: loadPlaceholder,
      },
      {
        path: 'analytics',
        title: 'Analytics | Shortly',
        data: {
          title: 'Analytics',
          description: 'Understand link activity through clear, accessible reporting.',
        },
        loadComponent: loadPlaceholder,
      },
      {
        path: 'api-keys',
        title: 'API Keys | Shortly',
        data: {
          title: 'API keys',
          description: 'Manage credentials for programmatic access to the platform.',
        },
        loadComponent: loadPlaceholder,
      },
      {
        path: 'domains',
        title: 'Domains | Shortly',
        data: {
          title: 'Domains',
          description: 'Connect and verify branded domains for shortened URLs.',
        },
        loadComponent: loadPlaceholder,
      },
      {
        path: 'account',
        title: 'Account | Shortly',
        data: {
          title: 'Account',
          description: 'Manage your profile, workspace preferences, and security.',
        },
        loadComponent: loadPlaceholder,
      },
    ],
  },
];

function loadPlaceholder() {
  return import('./feature-placeholder.component').then(
    ({ FeaturePlaceholderComponent }) => FeaturePlaceholderComponent,
  );
}
