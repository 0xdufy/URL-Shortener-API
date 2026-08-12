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
        data: {
          eyebrow: 'Workspace overview',
          title: 'Your link dashboard',
          description: 'Find, review, and manage the short links owned by this account.',
        },
        loadComponent: () =>
          import('./owned-links-page.component').then(
            ({ OwnedLinksPageComponent }) => OwnedLinksPageComponent,
          ),
      },
      {
        path: 'links',
        title: 'Links | Shortly',
        data: {
          title: 'Links',
          eyebrow: 'Link management',
          description: 'Search, filter, and inspect every short link owned by this account.',
        },
        loadComponent: () =>
          import('./owned-links-page.component').then(
            ({ OwnedLinksPageComponent }) => OwnedLinksPageComponent,
          ),
      },
      {
        path: 'links/new',
        title: 'Create link | Shortly',
        data: {
          title: 'Create a link',
          description: 'The create-link workflow will be available here.',
        },
        loadComponent: loadPlaceholder,
      },
      {
        path: 'links/:shortCode',
        title: 'Link details | Shortly',
        data: {
          title: 'Link details',
          description: 'The complete link details and lifecycle actions will be available here.',
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
