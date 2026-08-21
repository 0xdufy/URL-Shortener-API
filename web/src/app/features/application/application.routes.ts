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
          mode: 'create',
          title: 'Create a link',
          description: 'Create a short link with a generated code or custom alias.',
        },
        loadComponent: () =>
          import('./link-form-page.component').then(
            ({ LinkFormPageComponent }) => LinkFormPageComponent,
          ),
      },
      {
        path: 'links/:shortCode/edit',
        title: 'Edit link | Shortly',
        data: {
          mode: 'edit',
          title: 'Edit link',
          description: 'Update an owned link destination or expiry.',
        },
        loadComponent: () =>
          import('./link-form-page.component').then(
            ({ LinkFormPageComponent }) => LinkFormPageComponent,
          ),
      },
      {
        path: 'links/:shortCode/analytics',
        title: 'Link analytics | Shortly',
        data: {
          title: 'Link analytics',
          description: 'Inspect aggregate trends and audience breakdowns for an owned link.',
        },
        loadComponent: () =>
          import('./link-analytics-page.component').then(
            ({ LinkAnalyticsPageComponent }) => LinkAnalyticsPageComponent,
          ),
      },
      {
        path: 'links/:shortCode',
        title: 'Link details | Shortly',
        data: {
          title: 'Link details',
          description: 'Inspect and manage an owned short link.',
        },
        loadComponent: () =>
          import('./link-details-page.component').then(
            ({ LinkDetailsPageComponent }) => LinkDetailsPageComponent,
          ),
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
        loadComponent: () =>
          import('./api-keys-page.component').then(
            ({ ApiKeysPageComponent }) => ApiKeysPageComponent,
          ),
      },
      {
        path: 'domains',
        title: 'Domains | Shortly',
        data: {
          title: 'Domains',
          description: 'Connect and verify branded domains for shortened URLs.',
        },
        loadComponent: () =>
          import('./custom-domains-page.component').then(
            ({ CustomDomainsPageComponent }) => CustomDomainsPageComponent,
          ),
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
