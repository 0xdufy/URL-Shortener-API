import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  output,
  viewChild,
} from '@angular/core';

import { ButtonComponent } from '../button/button.component';
import { IconComponent } from '../icon/icon.component';

let nextDialogId = 0;

@Component({
  selector: 'app-confirmation-dialog',
  imports: [ButtonComponent, IconComponent],
  template: `
    <dialog #dialog [attr.aria-labelledby]="headingId" (close)="handleClose()">
      <div class="dialog-heading">
        <span class="warning-icon"><app-icon name="warning" /></span>
        <div>
          <h2 [id]="headingId">{{ title() }}</h2>
          <p>{{ message() }}</p>
        </div>
      </div>
      <div class="dialog-actions">
        <app-button variant="secondary" (click)="close('cancel')">Cancel</app-button>
        <app-button variant="danger" icon="trash" (click)="close('confirm')">
          {{ confirmLabel() }}
        </app-button>
      </div>
    </dialog>
  `,
  styleUrl: './confirmation-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmationDialogComponent {
  protected readonly headingId = `confirmation-dialog-title-${++nextDialogId}`;
  readonly title = input('Confirm destructive action');
  readonly message = input.required<string>();
  readonly confirmLabel = input('Delete');
  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  open(): void {
    const dialog = this.dialog().nativeElement;
    dialog.returnValue = '';
    dialog.showModal();
  }

  protected close(result: 'cancel' | 'confirm'): void {
    this.dialog().nativeElement.close(result);
  }

  protected handleClose(): void {
    if (this.dialog().nativeElement.returnValue === 'confirm') {
      this.confirmed.emit();
    } else {
      this.cancelled.emit();
    }
  }
}
