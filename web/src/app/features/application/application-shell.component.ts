import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthenticationApiClient } from '../../core/api/authentication-api-client.service';
import { AuthenticationStateService } from '../../core/auth/authentication-state.service';
import { safeReturnUrl } from '../../core/auth/safe-return-url';
import { IconComponent, IconName } from '../../shared/ui/icon/icon.component';
import { ToastViewportComponent } from '../../shared/ui/toast/toast-viewport.component';
import { ToastService } from '../../shared/ui/toast/toast.service';

interface NavigationItem {
  readonly label: string;
  readonly path: string;
  readonly icon: IconName;
}

@Component({
  selector: 'app-application-shell',
  imports: [IconComponent, RouterLink, RouterLinkActive, RouterOutlet, ToastViewportComponent],
  template: `
    <a class="skip-link" href="#main-content">Skip to main content</a>

    <div class="app-shell">
      <header class="top-bar">
        <button
          type="button"
          class="menu-button"
          aria-label="Open navigation"
          aria-controls="primary-navigation"
          [attr.aria-expanded]="navigationOpen()"
          (click)="toggleNavigation()"
        >
          <app-icon [name]="navigationOpen() ? 'close' : 'menu'" />
        </button>

        <a class="brand compact-brand" routerLink="/app/dashboard" (click)="closeNavigation()">
          <span class="brand-mark" aria-hidden="true">S</span>
          <span>Shortly</span>
        </a>

        <a class="account-button" routerLink="/app/account" aria-label="Open account settings">
          <span class="avatar" aria-hidden="true">{{ userInitials() }}</span>
          <span class="account-copy">
            <strong>{{ authenticationState.user()?.email }}</strong>
            <small>Account settings</small>
          </span>
          <app-icon name="chevron-down" />
        </a>
      </header>

      <aside id="primary-navigation" class="sidebar" [class.is-open]="navigationOpen()">
        <a class="brand desktop-brand" routerLink="/app/dashboard">
          <span class="brand-mark" aria-hidden="true">S</span>
          <span>Shortly</span>
        </a>

        <nav aria-label="Primary navigation">
          <p class="nav-label">Workspace</p>
          @for (item of primaryNavigation; track item.path) {
            <a
              [routerLink]="item.path"
              routerLinkActive="active"
              [routerLinkActiveOptions]="{ exact: true }"
              (click)="closeNavigation()"
            >
              <app-icon [name]="item.icon" />
              <span>{{ item.label }}</span>
            </a>
          }

          <p class="nav-label nav-label-secondary">Manage</p>
          @for (item of managementNavigation; track item.path) {
            <a
              [routerLink]="item.path"
              routerLinkActive="active"
              [routerLinkActiveOptions]="{ exact: true }"
              (click)="closeNavigation()"
            >
              <app-icon [name]="item.icon" />
              <span>{{ item.label }}</span>
            </a>
          }
        </nav>

        <div class="sidebar-footer">
          <div class="signed-in-user">
            <span class="avatar" aria-hidden="true">{{ userInitials() }}</span>
            <span>
              <strong>Signed in</strong>
              <small>{{ authenticationState.user()?.email }}</small>
            </span>
          </div>
          <a
            routerLink="/app/account"
            routerLinkActive="active"
            [routerLinkActiveOptions]="{ exact: true }"
            (click)="closeNavigation()"
          >
            <app-icon name="account" />
            <span>Account</span>
          </a>
          <button
            type="button"
            class="sign-out-button"
            [disabled]="signingOut()"
            (click)="signOut()"
          >
            <app-icon name="logout" />
            <span>{{ signingOut() ? 'Signing out…' : 'Sign out' }}</span>
          </button>
          <p>URL Shortener <span aria-hidden="true">·</span> v1 foundation</p>
        </div>
      </aside>

      @if (navigationOpen()) {
        <button
          type="button"
          class="nav-backdrop"
          aria-label="Close navigation"
          (click)="closeNavigation()"
        ></button>
      }

      <main id="main-content" class="main-content" tabindex="-1">
        <router-outlet />
      </main>
    </div>

    <app-toast-viewport />
  `,
  styleUrl: './application-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApplicationShellComponent {
  protected readonly authenticationState = inject(AuthenticationStateService);
  private readonly authenticationApi = inject(AuthenticationApiClient);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);
  protected readonly navigationOpen = signal(false);
  protected readonly signingOut = signal(false);
  protected readonly userInitials = computed(() => {
    const email = this.authenticationState.user()?.email ?? '';
    return email.slice(0, 2).toUpperCase() || 'U';
  });
  protected readonly primaryNavigation: readonly NavigationItem[] = [
    { label: 'Dashboard', path: '/app/dashboard', icon: 'dashboard' },
    { label: 'Links', path: '/app/links', icon: 'links' },
    { label: 'Analytics', path: '/app/analytics', icon: 'analytics' },
  ];
  protected readonly managementNavigation: readonly NavigationItem[] = [
    { label: 'API keys', path: '/app/api-keys', icon: 'key' },
    { label: 'Domains', path: '/app/domains', icon: 'domains' },
  ];

  constructor() {
    effect(() => {
      if (this.authenticationState.reason() !== 'unauthorized') {
        return;
      }

      void this.router.navigate(['/auth/sign-in'], {
        queryParams: { returnUrl: safeReturnUrl(this.router.url) },
        replaceUrl: true,
      });
    });
  }

  @HostListener('document:keydown.escape')
  protected closeNavigation(): void {
    this.navigationOpen.set(false);
  }

  protected toggleNavigation(): void {
    this.navigationOpen.update((open) => !open);
  }

  protected signOut(): void {
    if (this.signingOut()) {
      return;
    }

    this.signingOut.set(true);
    this.authenticationApi
      .signOut()
      .pipe(finalize(() => this.signingOut.set(false)))
      .subscribe({
        next: () => {
          this.toastService.show('Signed out', 'Your browser session has ended.', 'info');
          void this.router.navigate(['/auth/sign-in'], { replaceUrl: true });
        },
        error: () => {
          this.toastService.show(
            'Signed out locally',
            'The server could not confirm sign-out. Close shared browsers for safety.',
            'error',
          );
          void this.router.navigate(['/auth/sign-in'], { replaceUrl: true });
        },
      });
  }
}
