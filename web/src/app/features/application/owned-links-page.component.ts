import { DatePipe, DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
import { Subject, Subscription, debounceTime, distinctUntilChanged, finalize, map } from 'rxjs';

import { ApiError } from '../../core/api/api-error';
import {
  ExpirationFilter,
  ShortUrlListItem,
  ShortUrlListQuery,
  ShortUrlListResponse,
  ShortUrlSort,
  SortDirection,
} from '../../core/api/api.models';
import { ShortUrlsApiClient } from '../../core/api/short-urls-api-client.service';
import { BadgeComponent, BadgeTone } from '../../shared/ui/badge/badge.component';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { PageHeaderComponent } from '../../shared/ui/page-header/page-header.component';
import { StatePanelComponent } from '../../shared/ui/state-panel/state-panel.component';
import { ToastService } from '../../shared/ui/toast/toast.service';

type ActivityFilter = 'all' | 'active' | 'inactive';

interface ListErrorState {
  readonly title: string;
  readonly message: string;
  readonly actionLabel: string;
  readonly unauthorized: boolean;
}

interface LinkStatus {
  readonly label: string;
  readonly tone: BadgeTone;
}

@Component({
  selector: 'app-owned-links-page',
  imports: [
    BadgeComponent,
    DatePipe,
    DecimalPipe,
    FormsModule,
    IconComponent,
    PageHeaderComponent,
    RouterLink,
    StatePanelComponent,
  ],
  template: `
    <div class="page-stack">
      <app-page-header [eyebrow]="eyebrow" [title]="title" [description]="description">
        <a class="button-link primary" routerLink="/app/links/new">
          <app-icon name="plus" />
          <span>Create link</span>
        </a>
      </app-page-header>

      <section class="summary-grid" aria-label="Link summary">
        <article class="surface summary-card">
          <span class="summary-icon links-icon" aria-hidden="true"><app-icon name="links" /></span>
          <div>
            <p>{{ hasFilters() ? 'Matching links' : 'Owned links' }}</p>
            <strong>{{ response()?.pagination?.totalItems ?? '—' }}</strong>
            <small>{{
              hasFilters() ? 'for the current filters' : 'visible to this account'
            }}</small>
          </div>
        </article>
        <article class="surface summary-card">
          <span class="summary-icon clicks-icon" aria-hidden="true"
            ><app-icon name="analytics"
          /></span>
          <div>
            <p>Clicks on this page</p>
            <strong>{{ pageClicks() | number }}</strong>
            <small>across the links shown below</small>
          </div>
        </article>
        <article class="surface summary-card">
          <span class="summary-icon active-icon" aria-hidden="true"><app-icon name="check" /></span>
          <div>
            <p>Active on this page</p>
            <strong>{{ activeOnPage() | number }}</strong>
            <small>active, unexpired links shown</small>
          </div>
        </article>
      </section>

      <section class="surface links-panel" aria-labelledby="owned-links-title">
        <div class="panel-heading">
          <div>
            <h2 id="owned-links-title">Owned links</h2>
            <p>Searches and filters run against the server and only return your links.</p>
          </div>
          @if (hasFilters()) {
            <button class="clear-button" type="button" (click)="clearFilters()">
              Clear filters
            </button>
          }
        </div>

        <div class="filters" role="search" aria-label="Filter owned links">
          <label class="search-field">
            <span>Search links</span>
            <span class="search-control">
              <app-icon name="links" />
              <input
                type="search"
                maxlength="200"
                autocomplete="off"
                placeholder="Short code or destination"
                [ngModel]="search()"
                (ngModelChange)="searchChanged($event)"
              />
            </span>
          </label>
          <label>
            <span>Status</span>
            <select [ngModel]="activity()" (ngModelChange)="activityChanged($event)">
              <option value="all">Any status</option>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
            </select>
          </label>
          <label>
            <span>Expiry</span>
            <select [ngModel]="expiration()" (ngModelChange)="expirationChanged($event)">
              <option value="all">Any expiry</option>
              <option value="notExpired">Not expired</option>
              <option value="expired">Expired</option>
            </select>
          </label>
          <label>
            <span>Visibility</span>
            <select [ngModel]="includeDeleted()" (ngModelChange)="deletedChanged($event)">
              <option [ngValue]="false">Current links</option>
              <option [ngValue]="true">Include deleted</option>
            </select>
          </label>
          <label>
            <span>Sort by</span>
            <select [ngModel]="sortBy()" (ngModelChange)="sortChanged($event)">
              <option value="createdAt">Created</option>
              <option value="shortCode">Short code</option>
              <option value="clickCount">Clicks</option>
              <option value="expiresAt">Expiry</option>
            </select>
          </label>
          <label>
            <span>Direction</span>
            <select [ngModel]="sortDirection()" (ngModelChange)="directionChanged($event)">
              <option value="desc">Descending</option>
              <option value="asc">Ascending</option>
            </select>
          </label>
        </div>

        <div class="results-summary" aria-live="polite">
          @if (response(); as result) {
            <p>
              {{ result.pagination.totalItems | number }}
              {{ result.pagination.totalItems === 1 ? 'link' : 'links' }}
              @if (hasFilters()) {
                <span>matching your filters</span>
              }
            </p>
          } @else {
            <span></span>
          }
          @if (loading() && response()) {
            <span class="refreshing"><span class="mini-spinner"></span> Updating results</span>
          }
        </div>

        @if (loading() && !response()) {
          <app-state-panel
            kind="loading"
            title="Loading your links"
            message="Retrieving links owned by this account."
          />
        } @else if (error(); as errorState) {
          <app-state-panel
            kind="error"
            [title]="errorState.title"
            [message]="errorState.message"
            [actionLabel]="errorState.actionLabel"
            (action)="handleErrorAction(errorState)"
          />
        } @else if (response(); as result) {
          @if (result.items.length === 0) {
            <app-state-panel
              kind="empty"
              [title]="
                hasFilters() ? 'No links match these filters' : 'Create your first short link'
              "
              [message]="
                hasFilters()
                  ? 'Try a different search or clear your filters to see all current links.'
                  : 'Shorten a destination to start managing and sharing links from this workspace.'
              "
              [actionLabel]="hasFilters() ? 'Clear filters' : 'Create link'"
              (action)="emptyAction()"
            />
          } @else {
            <div class="desktop-table">
              <table>
                <thead>
                  <tr>
                    <th scope="col">Link</th>
                    <th scope="col">Status</th>
                    <th scope="col">Clicks</th>
                    <th scope="col">Created</th>
                    <th scope="col"><span class="visually-hidden">Actions</span></th>
                  </tr>
                </thead>
                <tbody>
                  @for (link of result.items; track link.id) {
                    <tr>
                      <td class="link-cell">
                        <div class="short-link-row">
                          <a [routerLink]="['/app/links', link.shortCode]">{{ link.shortUrl }}</a>
                          <button
                            type="button"
                            class="icon-button"
                            [attr.aria-label]="'Copy ' + link.shortUrl"
                            title="Copy short URL"
                            (click)="copyShortUrl(link)"
                          >
                            <span aria-hidden="true">⧉</span>
                          </button>
                        </div>
                        <a
                          class="destination"
                          [href]="link.originalUrl"
                          target="_blank"
                          rel="noopener noreferrer"
                          [title]="link.originalUrl"
                          >{{ link.originalUrl }}</a
                        >
                        <small class="expiry">{{ expiryLabel(link) }}</small>
                      </td>
                      <td>
                        <app-badge [tone]="status(link).tone">{{ status(link).label }}</app-badge>
                      </td>
                      <td class="number-cell">{{ link.clickCount | number }}</td>
                      <td>
                        <time [attr.datetime]="link.createdAtUtc">
                          {{ link.createdAtUtc | date: 'mediumDate' }}
                        </time>
                        <small>{{ link.createdAtUtc | date: 'shortTime' }}</small>
                      </td>
                      <td class="details-cell">
                        <div class="table-actions">
                          @if (!link.isDeleted) {
                            <a [routerLink]="['/app/links', link.shortCode, 'edit']">Edit</a>
                          }
                          <a [routerLink]="['/app/links', link.shortCode]">View details</a>
                        </div>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>

            <div class="mobile-list">
              @for (link of result.items; track link.id) {
                <article class="link-card">
                  <div class="card-topline">
                    <app-badge [tone]="status(link).tone">{{ status(link).label }}</app-badge>
                    <span>{{ link.clickCount | number }} clicks</span>
                  </div>
                  <a class="mobile-short-url" [routerLink]="['/app/links', link.shortCode]">
                    {{ link.shortUrl }}
                  </a>
                  <a
                    class="destination"
                    [href]="link.originalUrl"
                    target="_blank"
                    rel="noopener noreferrer"
                    [title]="link.originalUrl"
                    >{{ link.originalUrl }}</a
                  >
                  <dl>
                    <div>
                      <dt>Created</dt>
                      <dd>{{ link.createdAtUtc | date: 'mediumDate' }}</dd>
                    </div>
                    <div>
                      <dt>Expiry</dt>
                      <dd>{{ expiryLabel(link) }}</dd>
                    </div>
                  </dl>
                  <div class="card-actions">
                    <button type="button" (click)="copyShortUrl(link)">Copy short URL</button>
                    @if (!link.isDeleted) {
                      <a [routerLink]="['/app/links', link.shortCode, 'edit']">Edit</a>
                    }
                    <a [routerLink]="['/app/links', link.shortCode]">View details</a>
                  </div>
                </article>
              }
            </div>

            <nav class="pagination" aria-label="Link list pages">
              <button
                type="button"
                [disabled]="!result.pagination.hasPreviousPage || loading()"
                (click)="goToPage(result.pagination.page - 1)"
              >
                Previous
              </button>
              <p>
                Page <strong>{{ result.pagination.page }}</strong> of
                <strong>{{ result.pagination.totalPages }}</strong>
              </p>
              <button
                type="button"
                [disabled]="!result.pagination.hasNextPage || loading()"
                (click)="goToPage(result.pagination.page + 1)"
              >
                Next
              </button>
              <label>
                <span>Rows per page</span>
                <select [ngModel]="pageSize()" (ngModelChange)="pageSizeChanged($event)">
                  <option [ngValue]="10">10</option>
                  <option [ngValue]="20">20</option>
                  <option [ngValue]="50">50</option>
                </select>
              </label>
            </nav>
          }
        }
      </section>
    </div>
  `,
  styleUrl: './owned-links-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OwnedLinksPageComponent {
  private readonly api = inject(ShortUrlsApiClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchChanges = new Subject<string>();
  private activeLoad?: Subscription;

  protected readonly eyebrow = String(this.route.snapshot.data['eyebrow'] ?? 'Workspace');
  protected readonly title = String(this.route.snapshot.data['title'] ?? 'Links');
  protected readonly description = String(this.route.snapshot.data['description'] ?? '');
  protected readonly response = signal<ShortUrlListResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<ListErrorState | null>(null);
  protected readonly search = signal('');
  protected readonly activity = signal<ActivityFilter>('all');
  protected readonly expiration = signal<ExpirationFilter>('all');
  protected readonly includeDeleted = signal(false);
  protected readonly sortBy = signal<ShortUrlSort>('createdAt');
  protected readonly sortDirection = signal<SortDirection>('desc');
  protected readonly pageSize = signal(20);
  protected readonly hasFilters = computed(
    () =>
      this.search().trim().length > 0 ||
      this.activity() !== 'all' ||
      this.expiration() !== 'all' ||
      this.includeDeleted(),
  );
  protected readonly pageClicks = computed(() =>
    (this.response()?.items ?? []).reduce((total, link) => total + link.clickCount, 0),
  );
  protected readonly activeOnPage = computed(
    () =>
      (this.response()?.items ?? []).filter(
        (link) => link.isActive && !link.isExpired && !link.isDeleted,
      ).length,
  );

  constructor() {
    this.searchChanges
      .pipe(
        map((value) => value.trim()),
        debounceTime(350),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((search) => {
        const currentSearch = this.route.snapshot.queryParamMap.get('search')?.trim() ?? '';
        if (search !== currentSearch) {
          void this.updateQuery({ search: search || null, page: null });
        }
      });

    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const page = this.positiveInteger(params.get('page'), 1);
      const pageSize = this.allowedPageSize(params.get('pageSize'));
      const search = (params.get('search') ?? '').slice(0, 200);
      const activity = this.parseActivity(params.get('isActive'));
      const expiration = this.parseExpiration(params.get('expiration'));
      const includeDeleted = params.get('includeDeleted') === 'true';
      const sortBy = this.parseSort(params.get('sortBy'));
      const sortDirection = params.get('sortDirection') === 'asc' ? 'asc' : 'desc';

      this.search.set(search);
      this.activity.set(activity);
      this.expiration.set(expiration);
      this.includeDeleted.set(includeDeleted);
      this.sortBy.set(sortBy);
      this.sortDirection.set(sortDirection);
      this.pageSize.set(pageSize);
      this.load({
        page,
        pageSize,
        search: search || undefined,
        isActive: activity === 'all' ? undefined : activity === 'active',
        expiration,
        includeDeleted,
        sortBy,
        sortDirection,
      });
    });
  }

  protected searchChanged(value: string): void {
    this.search.set(value);
    this.searchChanges.next(value);
  }

  protected activityChanged(value: ActivityFilter): void {
    this.activity.set(value);
    void this.updateQuery({
      isActive: value === 'all' ? null : value === 'active',
      page: null,
    });
  }

  protected expirationChanged(value: ExpirationFilter): void {
    this.expiration.set(value);
    void this.updateQuery({ expiration: value === 'all' ? null : value, page: null });
  }

  protected deletedChanged(value: boolean): void {
    this.includeDeleted.set(value);
    void this.updateQuery({ includeDeleted: value ? true : null, page: null });
  }

  protected sortChanged(value: ShortUrlSort): void {
    this.sortBy.set(value);
    void this.updateQuery({ sortBy: value === 'createdAt' ? null : value, page: null });
  }

  protected directionChanged(value: SortDirection): void {
    this.sortDirection.set(value);
    void this.updateQuery({ sortDirection: value === 'desc' ? null : value, page: null });
  }

  protected pageSizeChanged(value: number): void {
    this.pageSize.set(value);
    void this.updateQuery({ pageSize: value === 20 ? null : value, page: null });
  }

  protected goToPage(page: number): void {
    void this.updateQuery({ page: page === 1 ? null : page });
  }

  protected clearFilters(): void {
    this.search.set('');
    void this.updateQuery({
      search: null,
      isActive: null,
      expiration: null,
      includeDeleted: null,
      page: null,
    });
  }

  protected emptyAction(): void {
    if (this.hasFilters()) {
      this.clearFilters();
      return;
    }
    void this.router.navigate(['/app/links/new']);
  }

  protected status(link: ShortUrlListItem): LinkStatus {
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

  protected expiryLabel(link: ShortUrlListItem): string {
    if (link.isDeleted && link.deletedAtUtc) {
      return `Deleted ${this.formatDate(link.deletedAtUtc)}`;
    }
    if (!link.expiresAtUtc) {
      return 'No expiry';
    }
    return `${link.isExpired ? 'Expired' : 'Expires'} ${this.formatDate(link.expiresAtUtc)}`;
  }

  protected async copyShortUrl(link: ShortUrlListItem): Promise<void> {
    try {
      if (!navigator.clipboard?.writeText) {
        throw new Error('Clipboard API unavailable');
      }
      await navigator.clipboard.writeText(link.shortUrl);
      this.toastService.show('Short URL copied', `${link.shortUrl} is ready to paste.`);
    } catch {
      this.toastService.show(
        'Could not copy the short URL',
        'Copy it manually from the link shown in the list.',
        'error',
      );
    }
  }

  protected handleErrorAction(errorState: ListErrorState): void {
    if (errorState.unauthorized) {
      void this.router.navigate(['/auth/sign-in'], {
        queryParams: { returnUrl: this.router.url },
        replaceUrl: true,
      });
      return;
    }
    this.reload();
  }

  private reload(): void {
    const params = this.route.snapshot.queryParamMap;
    this.load({
      page: this.positiveInteger(params.get('page'), 1),
      pageSize: this.allowedPageSize(params.get('pageSize')),
      search: params.get('search')?.trim() || undefined,
      isActive:
        params.get('isActive') === 'true'
          ? true
          : params.get('isActive') === 'false'
            ? false
            : undefined,
      expiration: this.parseExpiration(params.get('expiration')),
      includeDeleted: params.get('includeDeleted') === 'true',
      sortBy: this.parseSort(params.get('sortBy')),
      sortDirection: params.get('sortDirection') === 'asc' ? 'asc' : 'desc',
    });
  }

  private load(query: ShortUrlListQuery): void {
    this.activeLoad?.unsubscribe();
    this.loading.set(true);
    this.error.set(null);
    this.activeLoad = this.api
      .list(query)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          const lastValidPage = Math.max(response.pagination.totalPages, 1);
          if (response.pagination.page > lastValidPage) {
            void this.updateQuery({ page: lastValidPage === 1 ? null : lastValidPage }, true);
            return;
          }
          this.response.set(response);
        },
        error: (error: unknown) => {
          this.response.set(null);
          this.error.set(this.toErrorState(error));
        },
      });
  }

  private updateQuery(queryParams: Params, replaceUrl = false): Promise<boolean> {
    return this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl,
    });
  }

  private toErrorState(error: unknown): ListErrorState {
    if (error instanceof ApiError) {
      if (error.kind === 'authentication') {
        return {
          title: 'Your session has ended',
          message: 'Sign in again to return to your owned links.',
          actionLabel: 'Sign in',
          unauthorized: true,
        };
      }
      if (error.kind === 'rate-limited') {
        const retry = error.retryAfterSeconds
          ? ` Try again in about ${error.retryAfterSeconds} seconds.`
          : ' Wait a moment before trying again.';
        return {
          title: 'Too many requests',
          message: `The link list is temporarily rate limited.${retry}`,
          actionLabel: 'Try again',
          unauthorized: false,
        };
      }
      if (error.kind === 'authorization') {
        return {
          title: 'Links are unavailable for this account',
          message: 'This account does not have permission to view this workspace.',
          actionLabel: 'Try again',
          unauthorized: false,
        };
      }
      return {
        title:
          error.kind === 'connectivity' ? 'Unable to reach the service' : 'Links could not load',
        message: error.message,
        actionLabel: 'Try again',
        unauthorized: false,
      };
    }
    return {
      title: 'Links could not load',
      message: 'An unexpected problem occurred. Try again.',
      actionLabel: 'Try again',
      unauthorized: false,
    };
  }

  private parseActivity(value: string | null): ActivityFilter {
    return value === 'true' ? 'active' : value === 'false' ? 'inactive' : 'all';
  }

  private parseExpiration(value: string | null): ExpirationFilter {
    return value === 'expired' || value === 'notExpired' ? value : 'all';
  }

  private parseSort(value: string | null): ShortUrlSort {
    return value === 'shortCode' || value === 'clickCount' || value === 'expiresAt'
      ? value
      : 'createdAt';
  }

  private positiveInteger(value: string | null, fallback: number): number {
    const parsed = Number(value);
    return Number.isInteger(parsed) && parsed >= 1 ? parsed : fallback;
  }

  private allowedPageSize(value: string | null): number {
    const parsed = Number(value);
    return parsed === 10 || parsed === 50 ? parsed : 20;
  }

  private formatDate(value: string): string {
    return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(value));
  }
}
