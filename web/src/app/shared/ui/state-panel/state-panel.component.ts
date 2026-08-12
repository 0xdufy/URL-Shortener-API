import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { ButtonComponent } from '../button/button.component';
import { IconComponent, IconName } from '../icon/icon.component';

export type StateKind = 'loading' | 'empty' | 'error';

@Component({
  selector: 'app-state-panel',
  imports: [ButtonComponent, IconComponent],
  host: { '[class]': '"state-" + kind()' },
  template: `
    @if (kind() === 'loading') {
      <span class="loader" aria-hidden="true"></span>
      <span class="visually-hidden">Loading</span>
    } @else {
      <span class="icon-wrap" aria-hidden="true"><app-icon [name]="iconName()" /></span>
    }
    <h2>{{ title() }}</h2>
    <p>{{ message() }}</p>
    @if (actionLabel()) {
      <app-button [variant]="kind() === 'error' ? 'secondary' : 'primary'" (click)="action.emit()">
        {{ actionLabel() }}
      </app-button>
    }
  `,
  styles: `
    :host {
      display: flex;
      min-height: 13rem;
      align-items: center;
      justify-content: center;
      flex-direction: column;
      padding: var(--space-8);
      border: 1px dashed var(--color-border-strong);
      border-radius: var(--radius-md);
      background: var(--color-surface-subtle);
      text-align: center;
    }

    .icon-wrap {
      display: grid;
      width: 2.75rem;
      height: 2.75rem;
      margin-bottom: var(--space-4);
      place-items: center;
      border-radius: 50%;
      background: var(--color-primary-soft);
      color: var(--color-primary);
    }

    :host.state-error .icon-wrap {
      background: var(--color-danger-soft);
      color: var(--color-danger);
    }

    h2 {
      margin: 0;
      font-size: 1rem;
    }

    p {
      max-width: 30rem;
      margin: var(--space-2) 0 var(--space-5);
      color: var(--color-text-muted);
      font-size: 0.875rem;
    }

    .loader {
      width: 2rem;
      height: 2rem;
      margin-bottom: var(--space-4);
      border: 3px solid var(--color-border);
      border-top-color: var(--color-primary);
      border-radius: 50%;
      animation: spin 700ms linear infinite;
    }

    @keyframes spin {
      to {
        transform: rotate(360deg);
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatePanelComponent {
  readonly kind = input<StateKind>('empty');
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly actionLabel = input<string | undefined>(undefined);
  readonly action = output<void>();

  protected iconName(): IconName {
    return this.kind() === 'error' ? 'error' : 'sparkles';
  }
}
