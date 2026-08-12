import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type IconName =
  | 'account'
  | 'analytics'
  | 'check'
  | 'chevron-down'
  | 'close'
  | 'dashboard'
  | 'domains'
  | 'error'
  | 'info'
  | 'key'
  | 'links'
  | 'menu'
  | 'plus'
  | 'sparkles'
  | 'trash'
  | 'warning';

@Component({
  selector: 'app-icon',
  template: `
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="1.8"
      stroke-linecap="round"
      stroke-linejoin="round"
    >
      @switch (name()) {
        @case ('dashboard') {
          <rect x="3" y="3" width="7" height="7" rx="1" />
          <rect x="14" y="3" width="7" height="7" rx="1" />
          <rect x="3" y="14" width="7" height="7" rx="1" />
          <rect x="14" y="14" width="7" height="7" rx="1" />
        }
        @case ('links') {
          <path d="M10 13a5 5 0 0 0 7.5.5l2-2a5 5 0 0 0-7-7l-1.1 1.1" />
          <path d="M14 11a5 5 0 0 0-7.5-.5l-2 2a5 5 0 0 0 7 7l1.1-1.1" />
        }
        @case ('analytics') {
          <path d="M4 20V10m6 10V4m6 16v-7m4 7H2" />
        }
        @case ('key') {
          <circle cx="8" cy="15" r="4" />
          <path d="m11 12 8-8m-2 2 2 2m-5 1 2 2" />
        }
        @case ('domains') {
          <circle cx="12" cy="12" r="9" />
          <path d="M3 12h18M12 3a15 15 0 0 1 0 18M12 3a15 15 0 0 0 0 18" />
        }
        @case ('account') {
          <circle cx="12" cy="8" r="4" />
          <path d="M4.5 21a7.5 7.5 0 0 1 15 0" />
        }
        @case ('menu') {
          <path d="M4 7h16M4 12h16M4 17h16" />
        }
        @case ('close') {
          <path d="m6 6 12 12M18 6 6 18" />
        }
        @case ('plus') {
          <path d="M12 5v14M5 12h14" />
        }
        @case ('trash') {
          <path d="M4 7h16m-10 4v6m4-6v6M9 7l1-3h4l1 3m3 0-1 14H7L6 7" />
        }
        @case ('check') {
          <path d="m5 12 4 4L19 6" />
        }
        @case ('warning') {
          <path
            d="M10.3 4.1 2.4 18a2 2 0 0 0 1.7 3h15.8a2 2 0 0 0 1.7-3L13.7 4.1a2 2 0 0 0-3.4 0Z"
          />
          <path d="M12 9v4m0 4h.01" />
        }
        @case ('error') {
          <circle cx="12" cy="12" r="9" />
          <path d="m9 9 6 6m0-6-6 6" />
        }
        @case ('info') {
          <circle cx="12" cy="12" r="9" />
          <path d="M12 11v5m0-8h.01" />
        }
        @case ('chevron-down') {
          <path d="m7 10 5 5 5-5" />
        }
        @case ('sparkles') {
          <path d="m12 3 1.1 3.2L16 8l-2.9 1.8L12 13l-1.1-3.2L8 8l2.9-1.8L12 3Z" />
          <path
            d="m18.5 14 .7 2.1 1.8 1.1-1.8 1.1-.7 2.1-.7-2.1-1.8-1.1 1.8-1.1.7-2.1ZM5.5 12l.7 2.1L8 15.2l-1.8 1.1-.7 2.1-.7-2.1L3 15.2l1.8-1.1.7-2.1Z"
          />
        }
      }
    </svg>
  `,
  styles: `
    :host {
      display: inline-flex;
      width: 1.25rem;
      height: 1.25rem;
      flex: 0 0 auto;
    }

    svg {
      width: 100%;
      height: 100%;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IconComponent {
  readonly name = input.required<IconName>();
}
