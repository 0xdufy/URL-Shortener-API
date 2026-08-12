import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { IconComponent, IconName } from '../icon/icon.component';

export type ButtonVariant = 'primary' | 'secondary' | 'quiet' | 'danger';

@Component({
  selector: 'app-button',
  imports: [IconComponent],
  host: { '[class]': '"variant-" + variant()' },
  template: `
    <button
      [attr.aria-busy]="loading() || null"
      [disabled]="disabled() || loading()"
      [type]="type()"
    >
      @if (loading()) {
        <span class="spinner" aria-hidden="true"></span>
      } @else if (icon(); as iconName) {
        <app-icon [name]="iconName" />
      }
      <span><ng-content /></span>
    </button>
  `,
  styleUrl: './button.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ButtonComponent {
  readonly variant = input<ButtonVariant>('primary');
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly icon = input<IconName | undefined>(undefined);
}
