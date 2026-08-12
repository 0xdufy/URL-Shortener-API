import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { ToastViewportComponent } from '../../shared/ui/toast/toast-viewport.component';

@Component({
  selector: 'app-authentication-layout',
  imports: [RouterOutlet, ToastViewportComponent],
  template: `
    <main class="auth-page">
      <section class="auth-introduction" aria-labelledby="auth-product-name">
        <a class="brand" href="/" aria-label="Shortly home">
          <span class="brand-mark" aria-hidden="true">S</span>
          <span id="auth-product-name">Shortly</span>
        </a>
        <div class="introduction-copy">
          <p class="eyebrow">Simple links. Useful insight.</p>
          <h1>Make every link easier to share and understand.</h1>
          <p>
            Create memorable short links, keep them organized, and see how they perform from one
            focused workspace.
          </p>
        </div>
        <p class="security-note">Secure sessions · Privacy-minded by design</p>
      </section>

      <section class="auth-workspace" aria-label="Account access">
        <div class="mobile-brand" aria-hidden="true">
          <span class="brand-mark">S</span>
          <strong>Shortly</strong>
        </div>
        <router-outlet />
      </section>
    </main>
    <app-toast-viewport />
  `,
  styleUrl: './authentication-layout.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthenticationLayoutComponent {}
