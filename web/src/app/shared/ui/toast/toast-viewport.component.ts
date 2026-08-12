import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { IconComponent, IconName } from '../icon/icon.component';
import { ToastService, ToastTone } from './toast.service';

@Component({
  selector: 'app-toast-viewport',
  imports: [IconComponent],
  template: `
    <section class="toast-region" aria-label="Notifications" aria-live="polite">
      @for (toast of toastService.messages(); track toast.id) {
        <article class="toast" [class]="'toast tone-' + toast.tone" role="status">
          <span class="toast-icon"><app-icon [name]="iconName(toast.tone)" /></span>
          <div>
            <p class="toast-title">{{ toast.title }}</p>
            <p class="toast-message">{{ toast.message }}</p>
          </div>
          <button
            type="button"
            class="dismiss"
            [attr.aria-label]="'Dismiss ' + toast.title + ' notification'"
            (click)="toastService.dismiss(toast.id)"
          >
            <app-icon name="close" />
          </button>
        </article>
      }
    </section>
  `,
  styles: `
    .toast-region {
      position: fixed;
      z-index: 100;
      right: var(--space-5);
      bottom: var(--space-5);
      display: grid;
      width: min(calc(100% - 2rem), 24rem);
      gap: var(--space-3);
      pointer-events: none;
    }

    .toast {
      display: grid;
      grid-template-columns: auto 1fr auto;
      gap: var(--space-3);
      padding: var(--space-4);
      border: 1px solid var(--color-border);
      border-left: 4px solid var(--color-primary);
      border-radius: var(--radius-md);
      background: var(--color-surface);
      box-shadow: var(--shadow-md);
      pointer-events: auto;
      animation: enter 180ms ease-out;
    }

    .toast.tone-success {
      border-left-color: var(--color-success);
    }

    .toast.tone-error {
      border-left-color: var(--color-danger);
    }

    .toast-icon {
      color: var(--color-primary);
    }

    .tone-success .toast-icon {
      color: var(--color-success);
    }

    .tone-error .toast-icon {
      color: var(--color-danger);
    }

    .toast-title,
    .toast-message {
      margin: 0;
    }

    .toast-title {
      font-size: 0.875rem;
      font-weight: 700;
    }

    .toast-message {
      margin-top: 0.15rem;
      color: var(--color-text-muted);
      font-size: 0.8125rem;
    }

    .dismiss {
      display: grid;
      width: 2rem;
      height: 2rem;
      margin: -0.35rem -0.35rem 0 0;
      padding: 0;
      place-items: center;
      border: 0;
      border-radius: var(--radius-sm);
      background: transparent;
      color: var(--color-text-muted);
      cursor: pointer;
    }

    .dismiss:hover {
      background: var(--color-surface-subtle);
    }

    .dismiss app-icon {
      width: 1rem;
      height: 1rem;
    }

    @keyframes enter {
      from {
        opacity: 0;
        transform: translateY(0.5rem);
      }
    }

    @media (max-width: 32rem) {
      .toast-region {
        right: 1rem;
        bottom: 1rem;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastViewportComponent {
  protected readonly toastService = inject(ToastService);

  protected iconName(tone: ToastTone): IconName {
    return tone === 'success' ? 'check' : tone === 'error' ? 'error' : 'info';
  }
}
