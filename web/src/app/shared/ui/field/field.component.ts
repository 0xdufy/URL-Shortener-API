import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-field',
  template: `
    <div class="label-row">
      <label [for]="controlId()">{{ label() }}</label>
      @if (optional()) {
        <span>Optional</span>
      }
    </div>
    <ng-content />
    @if (error()) {
      <p class="message error" [id]="controlId() + '-error'" role="alert">{{ error() }}</p>
    } @else if (hint()) {
      <p class="message" [id]="controlId() + '-hint'">{{ hint() }}</p>
    }
  `,
  styles: `
    :host {
      display: grid;
      gap: 0.4rem;
    }

    .label-row {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      gap: var(--space-3);
    }

    label {
      color: var(--color-text);
      font-size: 0.875rem;
      font-weight: 700;
    }

    .label-row span,
    .message {
      color: var(--color-text-muted);
      font-size: 0.75rem;
    }

    .message {
      margin: 0;
    }

    .message.error {
      color: var(--color-danger);
      font-weight: 600;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FieldComponent {
  readonly controlId = input.required<string>();
  readonly label = input.required<string>();
  readonly hint = input<string | undefined>(undefined);
  readonly error = input<string | undefined>(undefined);
  readonly optional = input(false);
}
