import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-page-header',
  template: `
    <header>
      <div>
        @if (eyebrow()) {
          <p class="eyebrow">{{ eyebrow() }}</p>
        }
        <h1>{{ title() }}</h1>
        @if (description()) {
          <p class="description">{{ description() }}</p>
        }
      </div>
      <div class="actions"><ng-content /></div>
    </header>
  `,
  styles: `
    header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: var(--space-6);
    }

    .eyebrow {
      margin: 0 0 var(--space-2);
      color: var(--color-primary);
      font-size: 0.75rem;
      font-weight: 800;
      letter-spacing: 0.08em;
      text-transform: uppercase;
    }

    h1 {
      margin: 0;
      font-size: clamp(1.75rem, 4vw, 2.25rem);
      line-height: 1.15;
      letter-spacing: -0.035em;
    }

    .description {
      max-width: 46rem;
      margin: var(--space-2) 0 0;
      color: var(--color-text-muted);
    }

    .actions {
      display: flex;
      flex: 0 0 auto;
      gap: var(--space-3);
    }

    @media (max-width: 38rem) {
      header {
        align-items: stretch;
        flex-direction: column;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageHeaderComponent {
  readonly title = input.required<string>();
  readonly description = input<string | undefined>(undefined);
  readonly eyebrow = input<string | undefined>(undefined);
}
