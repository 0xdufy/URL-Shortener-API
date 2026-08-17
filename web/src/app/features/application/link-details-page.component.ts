import { DatePipe, DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, Subscription, catchError, finalize, of, switchMap, throwError } from 'rxjs';

import { ApiError } from '../../core/api/api-error';
import { ShortUrlResource } from '../../core/api/api.models';
import { ShortUrlsApiClient } from '../../core/api/short-urls-api-client.service';
import { BadgeComponent, BadgeTone } from '../../shared/ui/badge/badge.component';
import { ButtonComponent } from '../../shared/ui/button/button.component';
import { ConfirmationDialogComponent } from '../../shared/ui/confirmation-dialog/confirmation-dialog.component';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { PageHeaderComponent } from '../../shared/ui/page-header/page-header.component';
import { StatePanelComponent } from '../../shared/ui/state-panel/state-panel.component';
import { ToastService } from '../../shared/ui/toast/toast.service';

type LifecycleOperation = 'status' | 'delete' | 'restore';

interface LinkStatus {
  readonly label: string;
  readonly tone: BadgeTone;
}

interface PageError {
  readonly title: string;
  readonly message: string;
  readonly actionLabel: string;
  readonly action: 'retry' | 'links' | 'sign-in';
}

interface ActionError {
  readonly title: string;
  readonly message: string;
  readonly actionLabel?: string;
  readonly action?: 'reload' | 'sign-in';
}

@Component({
  selector: 'app-link-details-page',
  imports: [
    BadgeComponent,
    ButtonComponent,
    ConfirmationDialogComponent,
    DatePipe,
    DecimalPipe,
    IconComponent,
    PageHeaderComponent,
    RouterLink,
    StatePanelComponent,
  ],
  template: `
    <div class="page-stack">
      <app-page-header
        eyebrow="Link management"
        title="Link details"
        description="Inspect this owned link, its current reachability, and every supported lifecycle action."
      >
        <a class="back-link" routerLink="/app/links">
          <span aria-hidden="true">←</span>
          Back to links
        </a>
      </app-page-header>

      @if (loading()) {
        <section class="surface state-card">
          <app-state-panel
            kind="loading"
            title="Loading link details"
            message="Retrieving the latest link state and usage total."
          />
        </section>
      } @else if (pageError(); as error) {
        <section class="surface state-card">
          <app-state-panel
            kind="error"
            [title]="error.title"
            [message]="error.message"
            [actionLabel]="error.actionLabel"
            (action)="handlePageError(error)"
          />
        </section>
      } @else if (link(); as currentLink) {
        @if (currentLink.isDeleted || currentLink.isExpired || !currentLink.isActive) {
          <section
            class="lifecycle-notice"
            [class.deleted]="currentLink.isDeleted"
            [class.expired]="currentLink.isExpired && !currentLink.isDeleted"
            role="status"
          >
            <app-icon [name]="currentLink.isDeleted ? 'trash' : 'warning'" />
            <div>
              <strong>{{ noticeTitle(currentLink) }}</strong>
              <p>{{ noticeMessage(currentLink) }}</p>
            </div>
          </section>
        }

        <section class="surface link-hero" aria-labelledby="short-link-title">
          <div class="hero-topline">
            <div>
              <p class="eyebrow">Canonical short URL</p>
              <h2 id="short-link-title">/{{ currentLink.shortCode }}</h2>
            </div>
            <app-badge [tone]="status(currentLink).tone">
              {{ status(currentLink).label }}
            </app-badge>
          </div>

          @if (currentLink.isDeleted) {
            <p class="short-url unavailable">{{ currentLink.shortUrl }}</p>
          } @else if (externalUrl(currentLink.shortUrl); as shortUrl) {
            <a class="short-url" [href]="shortUrl" target="_blank" rel="noopener noreferrer">
              {{ currentLink.shortUrl }}
            </a>
          } @else {
            <p class="short-url invalid-url">The API returned an invalid short URL.</p>
          }

          <div class="hero-actions">
            <app-button variant="secondary" (click)="copyShortUrl(currentLink)">
              Copy short URL
            </app-button>
            @if (!currentLink.isDeleted && externalUrl(currentLink.shortUrl); as shortUrl) {
              <a
                class="button-link secondary"
                [href]="shortUrl"
                target="_blank"
                rel="noopener noreferrer"
              >
                Open short link
              </a>
            }
          </div>
        </section>

        @if (actionError(); as error) {
          <section class="action-alert" role="alert">
            <app-icon name="error" />
            <div>
              <strong>{{ error.title }}</strong>
              <p>{{ error.message }}</p>
            </div>
            @if (error.actionLabel) {
              <button type="button" (click)="handleActionError(error)">
                {{ error.actionLabel }}
              </button>
            }
          </section>
        }

        <div class="content-grid">
          <div class="details-stack">
            <section class="surface destination-card" aria-labelledby="destination-title">
              <div class="section-heading-row">
                <div>
                  <p class="eyebrow">Redirect destination</p>
                  <h2 id="destination-title">Where this link leads</h2>
                </div>
                @if (externalUrl(currentLink.originalUrl); as destination) {
                  <a
                    class="open-destination"
                    [href]="destination"
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    Open destination
                  </a>
                }
              </div>
              @if (externalUrl(currentLink.originalUrl); as destination) {
                <a
                  class="destination-url"
                  [href]="destination"
                  target="_blank"
                  rel="noopener noreferrer"
                  [title]="currentLink.originalUrl"
                >
                  {{ currentLink.originalUrl }}
                </a>
              } @else {
                <p class="destination-url invalid-url">The API returned an invalid destination.</p>
              }
            </section>

            <section class="metric-grid" aria-label="Basic link statistics">
              <article class="surface metric-card">
                <span class="metric-icon clicks"><app-icon name="analytics" /></span>
                <div>
                  <p>Total clicks</p>
                  <strong>{{ currentLink.clickCount | number }}</strong>
                  <small>may take a few seconds to update</small>
                </div>
              </article>
              <article class="surface metric-card">
                <span class="metric-icon"><app-icon name="check" /></span>
                <div>
                  <p>Last opened</p>
                  <strong class="date-value">{{ lastOpenedLabel(currentLink) }}</strong>
                  <small>{{ lastOpenedContext(currentLink) }} · may update shortly</small>
                </div>
              </article>
              <article class="surface metric-card">
                <span class="metric-icon"><app-icon name="links" /></span>
                <div>
                  <p>Created</p>
                  <strong class="date-value">{{
                    currentLink.createdAtUtc | date: 'medium'
                  }}</strong>
                  <small>shown in your local time</small>
                </div>
              </article>
              <article class="surface metric-card">
                <span class="metric-icon expiry-icon"><app-icon name="warning" /></span>
                <div>
                  <p>Expiry</p>
                  <strong class="date-value">{{ expiryLabel(currentLink) }}</strong>
                  <small>{{
                    currentLink.expiresAtUtc ? 'shown in your local time' : 'no automatic expiry'
                  }}</small>
                </div>
              </article>
            </section>

            <section class="surface metadata-card" aria-labelledby="metadata-title">
              <div class="section-heading-row">
                <div>
                  <p class="eyebrow">Resource metadata</p>
                  <h2 id="metadata-title">Lifecycle record</h2>
                </div>
              </div>
              <dl>
                <div>
                  <dt>Short code</dt>
                  <dd class="monospace">{{ currentLink.shortCode }}</dd>
                </div>
                <div>
                  <dt>Resource ID</dt>
                  <dd class="monospace">{{ currentLink.id }}</dd>
                </div>
                <div>
                  <dt>Configured state</dt>
                  <dd>{{ currentLink.isActive ? 'Active' : 'Inactive' }}</dd>
                </div>
                <div>
                  <dt>Redirect state</dt>
                  <dd>{{ redirectState(currentLink) }}</dd>
                </div>
                <div>
                  <dt>Created</dt>
                  <dd>{{ currentLink.createdAtUtc | date: 'medium' }}</dd>
                </div>
                <div>
                  <dt>Expires</dt>
                  <dd>
                    {{
                      currentLink.expiresAtUtc
                        ? (currentLink.expiresAtUtc | date: 'medium')
                        : 'Never'
                    }}
                  </dd>
                </div>
                @if (currentLink.isDeleted) {
                  <div>
                    <dt>Deleted</dt>
                    <dd>
                      {{
                        currentLink.deletedAtUtc
                          ? (currentLink.deletedAtUtc | date: 'medium')
                          : 'Deletion time unavailable'
                      }}
                    </dd>
                  </div>
                  <div>
                    <dt>Restore deadline</dt>
                    <dd>{{ restoreDeadlineLabel(currentLink) }}</dd>
                  </div>
                }
              </dl>
            </section>
          </div>

          <aside class="side-stack">
            <section class="surface lifecycle-card" aria-labelledby="lifecycle-title">
              <p class="eyebrow">Lifecycle actions</p>
              <h2 id="lifecycle-title">Manage this link</h2>

              @if (currentLink.isDeleted) {
                <p class="lifecycle-copy">
                  Deleted links cannot redirect or be edited. Restore is available only before the
                  server-provided deadline.
                </p>
                @if (canRestore(currentLink)) {
                  <app-button
                    icon="check"
                    [loading]="pending() === 'restore'"
                    [disabled]="pending() !== null"
                    (click)="restoreLink()"
                  >
                    Restore link
                  </app-button>
                  <small class="restore-window">
                    Available until {{ currentLink.restoreUntilUtc | date: 'medium' }}
                  </small>
                } @else {
                  <div class="restore-unavailable">
                    <app-icon name="warning" />
                    <span>This link is outside its restore window.</span>
                  </div>
                }
              } @else {
                <p class="lifecycle-copy">
                  {{
                    currentLink.isActive
                      ? 'Deactivate to pause redirects without changing or deleting the link.'
                      : 'Activate to allow redirects again. Expiry rules still apply.'
                  }}
                </p>
                <app-button
                  [variant]="currentLink.isActive ? 'secondary' : 'primary'"
                  [loading]="pending() === 'status'"
                  [disabled]="pending() !== null"
                  (click)="toggleStatus()"
                >
                  {{ currentLink.isActive ? 'Deactivate link' : 'Activate link' }}
                </app-button>
                <a
                  class="button-link secondary full-width"
                  [class.disabled]="pending() !== null"
                  [attr.aria-disabled]="pending() !== null || null"
                  [routerLink]="
                    pending() === null ? ['/app/links', currentLink.shortCode, 'edit'] : null
                  "
                >
                  Edit destination or expiry
                </a>
                <button
                  class="delete-button"
                  type="button"
                  [disabled]="pending() !== null"
                  (click)="requestDelete()"
                >
                  <app-icon name="trash" />
                  Delete link
                </button>
              }
            </section>

            @if (!currentLink.isDeleted) {
              <section class="surface analytics-card" aria-labelledby="analytics-title">
                <span class="analytics-icon"><app-icon name="analytics" /></span>
                <p class="eyebrow">Analytics</p>
                <h2 id="analytics-title">Explore link performance</h2>
                <p>
                  The basic click total is shown here. Detailed trends and breakdowns arrive in the
                  advanced analytics workspace.
                </p>
                <a [routerLink]="['/app/links', currentLink.shortCode, 'analytics']">
                  Go to analytics
                  <span aria-hidden="true">→</span>
                </a>
              </section>
            }
          </aside>
        </div>

        <app-confirmation-dialog
          #deleteDialog
          title="Delete this short link?"
          [message]="deleteConfirmationMessage(currentLink)"
          confirmLabel="Delete link"
          (confirmed)="deleteLink()"
        />
      }
    </div>
  `,
  styleUrl: './link-details-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkDetailsPageComponent {
  private readonly api = inject(ShortUrlsApiClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly deleteDialog = viewChild.required<ConfirmationDialogComponent>('deleteDialog');
  private activeLoad?: Subscription;

  protected readonly shortCode = signal('');
  protected readonly link = signal<ShortUrlResource | null>(null);
  protected readonly loading = signal(true);
  protected readonly pageError = signal<PageError | null>(null);
  protected readonly actionError = signal<ActionError | null>(null);
  protected readonly pending = signal<LifecycleOperation | null>(null);

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const shortCode = params.get('shortCode')?.trim() ?? '';
      this.shortCode.set(shortCode);
      this.loadLink(shortCode);
    });
  }

  protected status(link: ShortUrlResource): LinkStatus {
    if (link.isDeleted) {
      return { label: 'Deleted', tone: 'danger' };
    }
    if (link.isExpired) {
      return { label: 'Expired', tone: 'warning' };
    }
    if (!link.isActive) {
      return { label: 'Inactive', tone: 'neutral' };
    }
    return { label: 'Active', tone: 'success' };
  }

  protected noticeTitle(link: ShortUrlResource): string {
    if (link.isDeleted) {
      return 'This link is deleted';
    }
    if (link.isExpired) {
      return 'This link has expired';
    }
    return 'Redirects are paused';
  }

  protected noticeMessage(link: ShortUrlResource): string {
    if (link.isDeleted) {
      return this.canRestore(link)
        ? 'It no longer redirects, but you can restore it before the deadline shown below.'
        : 'It no longer redirects and is not currently eligible for restoration.';
    }
    if (link.isExpired) {
      return 'The short URL returns an expired response until you replace or clear its expiry.';
    }
    return 'The short URL does not redirect while its configured state is inactive.';
  }

  protected redirectState(link: ShortUrlResource): string {
    if (link.isDeleted) {
      return 'Unavailable — deleted';
    }
    if (link.isExpired) {
      return 'Unavailable — expired';
    }
    if (!link.isActive) {
      return 'Unavailable — inactive';
    }
    return 'Available';
  }

  protected expiryLabel(link: ShortUrlResource): string {
    if (!link.expiresAtUtc) {
      return 'Never';
    }
    const formatted = this.formatDate(link.expiresAtUtc);
    return link.isExpired ? `Expired ${formatted}` : formatted;
  }

  protected lastOpenedLabel(link: ShortUrlResource): string {
    if (link.isDeleted) {
      return 'Unavailable';
    }
    return link.lastAccessedAtUtc ? this.formatDate(link.lastAccessedAtUtc) : 'No clicks yet';
  }

  protected lastOpenedContext(link: ShortUrlResource): string {
    if (link.isDeleted) {
      return 'detail is hidden after deletion';
    }
    return link.lastAccessedAtUtc ? 'shown in your local time' : 'waiting for the first visit';
  }

  protected restoreDeadlineLabel(link: ShortUrlResource): string {
    if (!link.restoreUntilUtc) {
      return 'Unavailable';
    }
    const deadline = this.formatDate(link.restoreUntilUtc);
    return this.canRestore(link) ? deadline : `Ended ${deadline}`;
  }

  protected canRestore(link: ShortUrlResource): boolean {
    if (!link.isDeleted || !link.restoreUntilUtc) {
      return false;
    }
    const deadline = new Date(link.restoreUntilUtc).getTime();
    return Number.isFinite(deadline) && Date.now() < deadline;
  }

  protected externalUrl(value: string): string | null {
    try {
      const url = new URL(value);
      return (url.protocol === 'http:' || url.protocol === 'https:') && Boolean(url.hostname)
        ? value
        : null;
    } catch {
      return null;
    }
  }

  protected async copyShortUrl(link: ShortUrlResource): Promise<void> {
    if (!this.externalUrl(link.shortUrl)) {
      this.toastService.show(
        'Could not copy the short URL',
        'The service returned an invalid URL. Reload the link before trying again.',
        'error',
      );
      return;
    }

    try {
      if (!navigator.clipboard?.writeText) {
        throw new Error('Clipboard API unavailable');
      }
      await navigator.clipboard.writeText(link.shortUrl);
      this.toastService.show('Short URL copied', `${link.shortUrl} is ready to paste.`);
    } catch {
      this.toastService.show(
        'Could not copy the short URL',
        'Copy it manually from the canonical short URL shown on this page.',
        'error',
      );
    }
  }

  protected toggleStatus(): void {
    const link = this.link();
    if (!link || link.isDeleted || this.pending()) {
      return;
    }

    const nextActive = !link.isActive;
    this.startOperation('status');
    this.api
      .updateStatus(link.shortCode, { isActive: nextActive })
      .pipe(
        finalize(() => this.finishOperation('status')),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updated) => {
          this.link.set(updated);
          this.toastService.show(
            nextActive ? 'Link activated' : 'Link deactivated',
            nextActive
              ? 'The active setting is on. Expiry rules still determine redirect availability.'
              : 'Redirects are paused until you activate the link again.',
          );
        },
        error: (error: unknown) => this.handleMutationError(error, 'status'),
      });
  }

  protected requestDelete(): void {
    if (!this.pending()) {
      this.deleteDialog().open();
    }
  }

  protected deleteConfirmationMessage(link: ShortUrlResource): string {
    return `/${link.shortCode} will stop redirecting immediately. Its alias and click history stay reserved, and restoration is available only during the server-defined retention window.`;
  }

  protected deleteLink(): void {
    const link = this.link();
    if (!link || link.isDeleted || this.pending()) {
      return;
    }

    this.startOperation('delete');
    this.api
      .delete(link.shortCode)
      .pipe(
        finalize(() => this.finishOperation('delete')),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.toastService.show(
            'Link deleted',
            'Redirects have stopped. Reloading the server-provided restore window now.',
          );
          this.loadLink(link.shortCode);
        },
        error: (error: unknown) => this.handleMutationError(error, 'delete'),
      });
  }

  protected restoreLink(): void {
    const link = this.link();
    if (!link || !this.canRestore(link) || this.pending()) {
      return;
    }

    this.startOperation('restore');
    this.api
      .restore(link.shortCode)
      .pipe(
        finalize(() => this.finishOperation('restore')),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (restored) => {
          this.link.set(restored);
          this.toastService.show(
            'Link restored',
            restored.isActive
              ? 'The link is restored. Expiry rules still determine redirect availability.'
              : 'The link is restored but remains inactive until you activate it.',
          );
        },
        error: (error: unknown) => this.handleMutationError(error, 'restore'),
      });
  }

  protected handlePageError(error: PageError): void {
    if (error.action === 'retry') {
      this.loadLink(this.shortCode());
      return;
    }
    if (error.action === 'sign-in') {
      this.goToSignIn();
      return;
    }
    void this.router.navigate(['/app/links']);
  }

  protected handleActionError(error: ActionError): void {
    if (error.action === 'sign-in') {
      this.goToSignIn();
      return;
    }
    if (error.action === 'reload') {
      this.loadLink(this.shortCode());
    }
  }

  private loadLink(shortCode: string): void {
    this.activeLoad?.unsubscribe();
    this.actionError.set(null);
    this.pageError.set(null);
    this.link.set(null);

    if (!shortCode) {
      this.loading.set(false);
      this.pageError.set({
        title: 'Link not available',
        message: 'This address does not identify a short link.',
        actionLabel: 'Back to links',
        action: 'links',
      });
      return;
    }

    this.loading.set(true);
    this.activeLoad = this.detailsRequest(shortCode)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (resource) => this.link.set(resource),
        error: (error: unknown) => this.pageError.set(this.describePageError(error)),
      });
  }

  private detailsRequest(shortCode: string): Observable<ShortUrlResource> {
    return this.api.get(shortCode).pipe(
      catchError((detailError: unknown) => {
        if (!(detailError instanceof ApiError) || detailError.kind !== 'not-found') {
          return throwError(() => detailError);
        }

        return this.deletedDetailsRequest(shortCode, detailError);
      }),
    );
  }

  private deletedDetailsRequest(
    shortCode: string,
    detailError: unknown,
    page = 1,
  ): Observable<ShortUrlResource> {
    return this.api
      .list({
        page,
        pageSize: 100,
        search: shortCode.slice(0, 200),
        includeDeleted: true,
      })
      .pipe(
        switchMap((response) => {
          const deleted = response.items.find(
            (item) => item.isDeleted && item.shortCode === shortCode,
          );
          if (deleted) {
            return of({ ...deleted, lastAccessedAtUtc: null });
          }
          if (response.pagination.hasNextPage) {
            return this.deletedDetailsRequest(shortCode, detailError, page + 1);
          }
          return throwError(() => detailError);
        }),
      );
  }

  private startOperation(operation: LifecycleOperation): void {
    this.actionError.set(null);
    this.pending.set(operation);
  }

  private finishOperation(operation: LifecycleOperation): void {
    if (this.pending() === operation) {
      this.pending.set(null);
    }
  }

  private handleMutationError(error: unknown, operation: LifecycleOperation): void {
    if (!(error instanceof ApiError)) {
      this.actionError.set({
        title: 'Action could not be completed',
        message: 'Something unexpected happened. Check your connection and try again.',
      });
      return;
    }

    if (error.kind === 'not-found' || (operation === 'restore' && error.kind === 'conflict')) {
      this.toastService.show(
        'Link state changed',
        'Reloading the latest lifecycle state before you continue.',
        'info',
      );
      this.loadLink(this.shortCode());
      return;
    }

    if (error.kind === 'authentication') {
      this.actionError.set({
        title: 'Your session has ended',
        message: 'Sign in again to continue managing this link.',
        actionLabel: 'Sign in',
        action: 'sign-in',
      });
      return;
    }

    if (error.kind === 'authorization') {
      this.actionError.set({
        title: 'Action not permitted',
        message: 'This account does not have permission to change this link.',
        actionLabel: 'Reload link',
        action: 'reload',
      });
      return;
    }

    if (operation === 'restore' && error.kind === 'gone') {
      this.actionError.set({
        title: 'The restore window has ended',
        message: 'The server no longer permits this deleted link to be restored.',
        actionLabel: 'Reload link',
        action: 'reload',
      });
      return;
    }

    if (error.kind === 'rate-limited') {
      const retry = error.retryAfterSeconds
        ? ` Try again in about ${error.retryAfterSeconds} seconds.`
        : ' Wait a moment before trying again.';
      this.actionError.set({
        title: 'Too many requests',
        message: `The lifecycle action was rate limited.${retry}`,
      });
      return;
    }

    this.actionError.set({
      title:
        error.kind === 'connectivity'
          ? 'Could not reach the service'
          : 'Action could not be completed',
      message: error.message,
    });
  }

  private describePageError(error: unknown): PageError {
    if (!(error instanceof ApiError)) {
      return {
        title: 'Link could not be loaded',
        message: 'Something unexpected happened while loading this link.',
        actionLabel: 'Try again',
        action: 'retry',
      };
    }

    if (error.kind === 'not-found') {
      return {
        title: 'Link not available',
        message:
          'This link does not exist, is no longer retained, or is not available to this account.',
        actionLabel: 'Back to links',
        action: 'links',
      };
    }
    if (error.kind === 'authentication') {
      return {
        title: 'Sign in to view this link',
        message: 'Your session is no longer available. Sign in again to return here.',
        actionLabel: 'Sign in',
        action: 'sign-in',
      };
    }
    if (error.kind === 'authorization') {
      return {
        title: 'Link unavailable for this account',
        message: 'This account does not have permission to view link-management resources.',
        actionLabel: 'Back to links',
        action: 'links',
      };
    }
    if (error.kind === 'rate-limited') {
      const retry = error.retryAfterSeconds
        ? ` Try again in about ${error.retryAfterSeconds} seconds.`
        : ' Wait a moment before trying again.';
      return {
        title: 'Too many requests',
        message: `Link details are temporarily rate limited.${retry}`,
        actionLabel: 'Try again',
        action: 'retry',
      };
    }
    return {
      title: error.kind === 'connectivity' ? 'Could not reach the service' : 'Link could not load',
      message: error.message,
      actionLabel: 'Try again',
      action: 'retry',
    };
  }

  private goToSignIn(): void {
    void this.router.navigate(['/auth/sign-in'], {
      queryParams: { returnUrl: this.router.url },
      replaceUrl: true,
    });
  }

  private formatDate(value: string): string {
    return new Intl.DateTimeFormat(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
  }
}
