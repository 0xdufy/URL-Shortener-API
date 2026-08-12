import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type BadgeTone = 'neutral' | 'success' | 'warning' | 'danger';

@Component({
  selector: 'app-badge',
  host: { '[class]': '"tone-" + tone()' },
  template: `<span class="marker" aria-hidden="true">{{ marker() }}</span
    ><ng-content />`,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      width: fit-content;
      padding: 0.25rem 0.55rem;
      border-radius: 999px;
      background: #eef1f5;
      color: #48556b;
      font-size: 0.75rem;
      font-weight: 700;
      line-height: 1.25;
    }

    :host.tone-success {
      background: var(--color-success-soft);
      color: var(--color-success);
    }

    :host.tone-warning {
      background: var(--color-warning-soft);
      color: var(--color-warning);
    }

    :host.tone-danger {
      background: var(--color-danger-soft);
      color: var(--color-danger);
    }

    .marker {
      font-size: 0.625rem;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BadgeComponent {
  readonly tone = input<BadgeTone>('neutral');

  protected marker(): string {
    return this.tone() === 'success' ? '✓' : this.tone() === 'warning' ? '!' : '•';
  }
}
