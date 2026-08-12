import { ChangeDetectionStrategy, Component, inject, viewChild } from '@angular/core';

import { BadgeComponent } from '../../shared/ui/badge/badge.component';
import { ButtonComponent } from '../../shared/ui/button/button.component';
import { ConfirmationDialogComponent } from '../../shared/ui/confirmation-dialog/confirmation-dialog.component';
import { FieldComponent } from '../../shared/ui/field/field.component';
import { PageHeaderComponent } from '../../shared/ui/page-header/page-header.component';
import { StatePanelComponent } from '../../shared/ui/state-panel/state-panel.component';
import { ToastService } from '../../shared/ui/toast/toast.service';

@Component({
  selector: 'app-foundation-overview',
  imports: [
    BadgeComponent,
    ButtonComponent,
    ConfirmationDialogComponent,
    FieldComponent,
    PageHeaderComponent,
    StatePanelComponent,
  ],
  template: `
    <div class="page-stack">
      <app-page-header
        eyebrow="Application foundation"
        title="Welcome to your workspace"
        description="The shared shell and interface patterns are ready for product features. No workspace data is loaded yet."
      >
        <app-button icon="plus" (click)="showFeedback()">Test feedback</app-button>
      </app-page-header>

      <section class="status-grid" aria-label="Foundation status">
        <article class="surface status-card">
          <span class="status-icon" aria-hidden="true">01</span>
          <div>
            <p>Navigation</p>
            <app-badge tone="success">Ready</app-badge>
          </div>
        </article>
        <article class="surface status-card">
          <span class="status-icon" aria-hidden="true">02</span>
          <div>
            <p>Reusable states</p>
            <app-badge tone="success">Ready</app-badge>
          </div>
        </article>
        <article class="surface status-card">
          <span class="status-icon" aria-hidden="true">03</span>
          <div>
            <p>Feature data</p>
            <app-badge tone="warning">Not loaded</app-badge>
          </div>
        </article>
      </section>

      <div class="content-grid">
        <section class="surface panel" aria-labelledby="empty-state-title">
          <div class="panel-heading">
            <div>
              <h2 class="section-heading" id="empty-state-title">Empty-state pattern</h2>
              <p class="section-description">A consistent starting point for every feature area.</p>
            </div>
          </div>
          <app-state-panel
            title="Nothing here yet"
            message="This workspace is ready for data when the product features are implemented."
            actionLabel="Test notification"
            (action)="showFeedback()"
          />
        </section>

        <section class="surface panel" aria-labelledby="form-pattern-title">
          <div class="panel-heading">
            <div>
              <h2 class="section-heading" id="form-pattern-title">Form pattern</h2>
              <p class="section-description">
                Labels, help, errors, and interaction states stay predictable.
              </p>
            </div>
          </div>
          <form class="form-grid" (submit)="$event.preventDefault(); showFeedback()">
            <app-field
              controlId="example-name"
              label="Display name"
              hint="Use a clear name that others will recognize."
            >
              <input
                id="example-name"
                class="form-control"
                type="text"
                placeholder="Example workspace"
                aria-describedby="example-name-hint"
              />
            </app-field>
            <app-field
              controlId="example-alias"
              label="Custom alias"
              error="Choose an alias before continuing."
              [optional]="true"
            >
              <input
                id="example-alias"
                class="form-control"
                type="text"
                aria-invalid="true"
                aria-describedby="example-alias-error"
              />
            </app-field>
            <div class="button-row">
              <app-button type="submit">Save example</app-button>
              <app-button variant="secondary" [disabled]="true">Disabled</app-button>
              <app-button variant="quiet" [loading]="true">Saving</app-button>
            </div>
          </form>
        </section>
      </div>

      <section class="surface panel" aria-labelledby="table-pattern-title">
        <div class="panel-heading table-heading">
          <div>
            <h2 class="section-heading" id="table-pattern-title">Table and destructive action</h2>
            <p class="section-description">
              Responsive data structure with explicit status language.
            </p>
          </div>
          <app-button variant="danger" icon="trash" (click)="dialogComponent().open()">
            Test confirmation
          </app-button>
        </div>
        <div class="table-wrap">
          <table class="data-table">
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col">Status</th>
                <th scope="col">Last updated</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td class="table-empty" colspan="3">No records to display.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </div>

    <app-confirmation-dialog
      #confirmationDialog
      title="Delete this example?"
      message="This demonstrates the shared confirmation pattern. No application data will be changed."
      confirmLabel="Confirm deletion"
      (confirmed)="confirmExample()"
    />
  `,
  styleUrl: './foundation-overview.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FoundationOverviewComponent {
  protected readonly dialogComponent = viewChild.required(ConfirmationDialogComponent);
  private readonly toastService = inject(ToastService);

  protected showFeedback(): void {
    this.toastService.show(
      'Feedback pattern is ready',
      'Feature screens can use this shared notification service.',
    );
  }

  protected confirmExample(): void {
    this.toastService.show(
      'Example confirmed',
      'The destructive flow completed without changing application data.',
      'info',
    );
  }
}
