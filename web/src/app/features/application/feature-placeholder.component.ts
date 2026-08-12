import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { PageHeaderComponent } from '../../shared/ui/page-header/page-header.component';
import { StatePanelComponent } from '../../shared/ui/state-panel/state-panel.component';

@Component({
  selector: 'app-feature-placeholder',
  imports: [PageHeaderComponent, StatePanelComponent],
  template: `
    <div class="page-stack">
      <app-page-header
        eyebrow="Coming in a later phase"
        [title]="title"
        [description]="description"
      />
      <section class="surface panel">
        <app-state-panel
          title="This area is ready"
          [message]="
            'The ' +
            title +
            ' feature will use the shared shell and design system when implemented.'
          "
        />
      </section>
    </div>
  `,
  styles: `
    .page-stack {
      display: grid;
      width: min(100%, 76rem);
      margin-inline: auto;
      gap: var(--space-8);
    }

    .panel {
      padding: var(--space-6);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeaturePlaceholderComponent {
  private readonly route = inject(ActivatedRoute);
  protected readonly title = this.route.snapshot.data['title'] as string;
  protected readonly description = this.route.snapshot.data['description'] as string;
}
