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
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  Observable,
  Subscription,
  catchError,
  combineLatest,
  distinctUntilChanged,
  finalize,
  forkJoin,
  map,
  of,
} from 'rxjs';

import { ApiError } from '../../core/api/api-error';
import {
  AnalyticsBreakdown,
  AnalyticsCategory,
  AnalyticsFreshness,
  AnalyticsSummary,
  AnalyticsTimeBucket,
  AnalyticsTimeSeries,
} from '../../core/api/api.models';
import { ShortUrlsApiClient } from '../../core/api/short-urls-api-client.service';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { PageHeaderComponent } from '../../shared/ui/page-header/page-header.component';
import { StatePanelComponent } from '../../shared/ui/state-panel/state-panel.component';

type ErrorAction = 'retry' | 'links' | 'sign-in';

interface AnalyticsErrorState {
  readonly title: string;
  readonly message: string;
  readonly actionLabel: string;
  readonly action: ErrorAction;
}

interface RequestResult<T> {
  readonly value: T | null;
  readonly error: unknown | null;
}

interface BreakdownView {
  readonly title: string;
  readonly description: string;
  readonly items: readonly AnalyticsCategory[];
}

interface ChartLabel {
  readonly x: number;
  readonly text: string;
}

@Component({
  selector: 'app-link-analytics-page',
  imports: [
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
      <app-page-header
        eyebrow="Advanced analytics"
        [title]="'Analytics for /' + (shortCode() || 'link')"
        description="Aggregate activity for this owned link, reported in UTC."
      >
        <a class="back-link" [routerLink]="['/app/links', shortCode()]">
          <span aria-hidden="true">←</span>
          Link details
        </a>
      </app-page-header>

      <section class="surface controls" aria-label="Analytics date range">
        <label>
          <span>Reporting range</span>
          <select
            [ngModel]="selectedDays()"
            [disabled]="loading()"
            (ngModelChange)="changeRange($event)"
          >
            @for (preset of rangePresets; track preset.days) {
              <option [ngValue]="preset.days">{{ preset.label }}</option>
            }
          </select>
        </label>
        <p>{{ rangeDescription() }} · The end boundary is exclusive and all buckets use UTC.</p>
        <button type="button" [disabled]="loading()" (click)="refresh()">
          {{ loading() ? 'Refreshing…' : 'Refresh data' }}
        </button>
      </section>

      @if (loading()) {
        <section class="surface state-card">
          <app-state-panel
            kind="loading"
            title="Loading link analytics"
            message="Requesting totals, trend buckets, and aggregate audience breakdowns."
          />
        </section>
      } @else if (pageError(); as error) {
        <section class="surface state-card">
          <app-state-panel
            kind="error"
            [title]="error.title"
            [message]="error.message"
            [actionLabel]="error.actionLabel"
            (action)="handleError(error)"
          />
        </section>
      } @else {
        @if (partialError(); as warning) {
          <section class="partial-alert" role="alert">
            <app-icon name="warning" />
            <div>
              <strong>{{ warning.title }}</strong>
              <p>{{ warning.message }}</p>
            </div>
            <button type="button" (click)="refresh()">Try again</button>
          </section>
        }

        @if (freshness(); as currentFreshness) {
          <section class="freshness-notice" role="status">
            <app-icon name="info" />
            <div>
              <strong>{{
                currentFreshness.isPartial
                  ? 'Current UTC day is still in progress'
                  : 'Analytics are eventually consistent'
              }}</strong>
              <p>
                Redirect events are processed asynchronously, so recent clicks may take a short time
                to appear.
                @if (currentFreshness.lastAggregatedAtUtc) {
                  Last aggregate update:
                  {{ currentFreshness.lastAggregatedAtUtc | date: 'medium' : 'UTC' }} UTC.
                } @else {
                  No aggregate has been recorded for this range yet.
                }
              </p>
            </div>
          </section>
        }

        @if (summary(); as currentSummary) {
          <section class="metric-grid" aria-label="Analytics summary">
            <article class="surface metric-card">
              <span class="metric-icon" aria-hidden="true"><app-icon name="analytics" /></span>
              <div>
                <p>Total clicks</p>
                <strong>{{ currentSummary.totalClicks | number }}</strong>
                <small>Server total for this range</small>
              </div>
            </article>
            <article class="surface metric-card">
              <span class="metric-icon visitors" aria-hidden="true"
                ><app-icon name="account"
              /></span>
              <div>
                <p>Unique visitor estimate</p>
                <strong>{{ currentSummary.uniqueVisitorsEstimate | number }}</strong>
                <small>Sum of privacy-preserving daily estimates</small>
              </div>
            </article>
          </section>
        }

        @if (isEmpty()) {
          <section class="surface empty-card">
            <app-state-panel
              kind="empty"
              title="No analytics in this range"
              message="This is a valid empty result. Share the short link or choose a wider reporting range, then return after redirects have been processed."
            />
          </section>
        }

        @if (series(); as currentSeries) {
          <section class="surface trend-card" aria-labelledby="trend-title">
            <div class="section-heading-row">
              <div>
                <p class="eyebrow">Click trend</p>
                <h2 id="trend-title">Daily activity</h2>
                <p>{{ currentSeries.totalClicks | number }} clicks across the returned buckets.</p>
              </div>
              <span class="range-chip">{{ formatRange(currentSeries) }}</span>
            </div>

            <div class="chart-wrap">
              <svg
                class="trend-chart"
                viewBox="0 0 720 240"
                role="img"
                aria-labelledby="trend-chart-title trend-chart-description"
                preserveAspectRatio="none"
              >
                <title id="trend-chart-title">Daily clicks for {{ currentSeries.shortCode }}</title>
                <desc id="trend-chart-description">
                  A line chart with {{ currentSeries.buckets.length }} UTC day buckets and a peak of
                  {{ maxBucketClicks() }} clicks.
                </desc>
                @for (guide of yGuides(); track guide.value) {
                  <line
                    class="grid-line"
                    x1="48"
                    x2="704"
                    [attr.y1]="guide.y"
                    [attr.y2]="guide.y"
                  />
                  <text class="axis-label y-label" x="40" [attr.y]="guide.y + 4">
                    {{ guide.value }}
                  </text>
                }
                @if (currentSeries.buckets.length > 0) {
                  <polyline class="trend-line" [attr.points]="chartPoints()" />
                  @for (
                    bucket of currentSeries.buckets;
                    track bucket.bucketStartUtc;
                    let index = $index
                  ) {
                    <circle
                      class="trend-point"
                      [attr.cx]="pointX(index, currentSeries.buckets.length)"
                      [attr.cy]="pointY(bucket.clicks)"
                      r="3"
                      tabindex="0"
                    >
                      <title>{{ formatBucket(bucket) }}: {{ bucket.clicks }} clicks</title>
                    </circle>
                  }
                }
                @for (label of chartLabels(); track label.x) {
                  <text class="axis-label x-label" [attr.x]="label.x" y="226">
                    {{ label.text }}
                  </text>
                }
              </svg>

              <ol class="mobile-trend" aria-label="Daily click buckets">
                @for (bucket of currentSeries.buckets; track bucket.bucketStartUtc) {
                  <li>
                    <span>{{ formatBucket(bucket) }}</span>
                    <span class="mobile-bar" aria-hidden="true">
                      <span [style.width.%]="bucketPercent(bucket.clicks)"></span>
                    </span>
                    <strong>{{ bucket.clicks | number }}</strong>
                  </li>
                }
              </ol>
            </div>
          </section>
        }

        @if (summary()) {
          <section class="breakdown-grid" aria-label="Audience breakdowns">
            @for (breakdown of breakdowns(); track breakdown.title) {
              <article class="surface breakdown-card">
                <div class="section-heading-row">
                  <div>
                    <p class="eyebrow">Breakdown</p>
                    <h2>{{ breakdown.title }}</h2>
                    <p>{{ breakdown.description }}</p>
                  </div>
                </div>
                @if (breakdown.items.length === 0) {
                  <p class="breakdown-empty">
                    No {{ breakdown.title.toLowerCase() }} data in this range.
                  </p>
                } @else {
                  <ol class="bar-list">
                    @for (item of breakdown.items; track $index) {
                      <li>
                        <div class="bar-label">
                          <span [title]="item.value">{{ item.value }}</span>
                          <strong>{{ item.clicks | number }}</strong>
                        </div>
                        <span class="bar-track" aria-hidden="true">
                          <span
                            [style.width.%]="breakdownPercent(item.clicks, breakdown.items)"
                          ></span>
                        </span>
                      </li>
                    }
                  </ol>
                }
              </article>
            }
          </section>

          <aside class="privacy-note" aria-label="Unique visitor estimate limitation">
            <app-icon name="info" />
            <p>
              <strong>About unique visitors:</strong> this estimate sums daily pseudonymous counts.
              It is not a cross-day person count and may overcount or undercount. No raw IP address,
              visitor identifier, user agent, or referrer path is displayed here.
            </p>
          </aside>
        }
      }
    </div>
  `,
  styleUrl: './link-analytics-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkAnalyticsPageComponent {
  private readonly api = inject(ShortUrlsApiClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private activeLoad?: Subscription;

  protected readonly rangePresets = [
    { days: 7, label: 'Last 7 UTC days' },
    { days: 30, label: 'Last 30 UTC days' },
    { days: 90, label: 'Last 90 UTC days' },
    { days: 365, label: 'Last 365 UTC days' },
  ] as const;
  protected readonly shortCode = signal('');
  protected readonly selectedDays = signal(30);
  protected readonly loading = signal(true);
  protected readonly summary = signal<AnalyticsSummary | null>(null);
  protected readonly series = signal<AnalyticsTimeSeries | null>(null);
  protected readonly pageError = signal<AnalyticsErrorState | null>(null);
  protected readonly partialError = signal<AnalyticsErrorState | null>(null);
  protected readonly freshness = computed<AnalyticsFreshness | null>(
    () => this.summary()?.freshness ?? this.series()?.freshness ?? null,
  );
  protected readonly isEmpty = computed(() => {
    const totals = [this.summary()?.totalClicks, this.series()?.totalClicks].filter(
      (total): total is number => total !== undefined,
    );
    return totals.length > 0 && totals.every((total) => total === 0);
  });
  protected readonly maxBucketClicks = computed(() =>
    Math.max(0, ...(this.series()?.buckets.map((bucket) => bucket.clicks) ?? [0])),
  );
  protected readonly chartPoints = computed(() => {
    const buckets = this.series()?.buckets ?? [];
    return buckets
      .map(
        (bucket, index) =>
          `${this.pointX(index, buckets.length).toFixed(2)},${this.pointY(bucket.clicks).toFixed(2)}`,
      )
      .join(' ');
  });
  protected readonly chartLabels = computed<readonly ChartLabel[]>(() => {
    const buckets = this.series()?.buckets ?? [];
    if (buckets.length === 0) {
      return [];
    }
    const indices = [...new Set([0, Math.floor((buckets.length - 1) / 2), buckets.length - 1])];
    return indices.map((index) => ({
      x: this.pointX(index, buckets.length),
      text: this.formatBucketShort(buckets[index]),
    }));
  });
  protected readonly yGuides = computed(() => {
    const maximum = this.maxBucketClicks();
    const values = maximum === 0 ? [0] : [maximum, Math.round(maximum / 2), 0];
    return [...new Set(values)].map((value) => ({ value, y: this.pointY(value) }));
  });
  protected readonly breakdowns = computed<readonly BreakdownView[]>(() => {
    const summary = this.summary();
    if (!summary) {
      return [];
    }
    return [
      {
        title: 'Top sources',
        description: 'Ranked referrer categories; long URLs and paths are never exposed.',
        items: this.referrerItems(summary.referrers),
      },
      {
        title: 'Devices',
        description: 'Broad device categories from the privacy-aware classifier.',
        items: summary.devices.items,
      },
      {
        title: 'Browsers',
        description: 'Bounded browser families, including stable unknown categories.',
        items: summary.browsers.items,
      },
      {
        title: 'Operating systems',
        description: 'Bounded operating-system families reported by the API.',
        items: summary.operatingSystems.items,
      },
    ];
  });

  constructor() {
    combineLatest([this.route.paramMap, this.route.queryParamMap])
      .pipe(
        map(([params, query]) => ({
          shortCode: params.get('shortCode')?.trim() ?? '',
          days: this.parseDays(query.get('range')),
        })),
        distinctUntilChanged(
          (previous, current) =>
            previous.shortCode === current.shortCode && previous.days === current.days,
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(({ shortCode, days }) => {
        this.shortCode.set(shortCode);
        this.selectedDays.set(days);
        this.load(shortCode, days);
      });
  }

  protected changeRange(days: number): void {
    if (!this.isAllowedDays(days) || days === this.selectedDays()) {
      return;
    }
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { range: days === 30 ? null : days },
      queryParamsHandling: 'merge',
    });
  }

  protected refresh(): void {
    this.load(this.shortCode(), this.selectedDays());
  }

  protected handleError(error: AnalyticsErrorState): void {
    if (error.action === 'sign-in') {
      void this.router.navigate(['/auth/sign-in'], {
        queryParams: { returnUrl: this.router.url },
        replaceUrl: true,
      });
      return;
    }
    if (error.action === 'links') {
      void this.router.navigate(['/app/links']);
      return;
    }
    this.refresh();
  }

  protected rangeDescription(): string {
    const precedingDays = this.selectedDays() - 1;
    return `Includes the current UTC day and the preceding ${precedingDays} days`;
  }

  protected pointX(index: number, count: number): number {
    return count <= 1 ? 376 : 48 + (index / (count - 1)) * 656;
  }

  protected pointY(clicks: number): number {
    const maximum = this.maxBucketClicks();
    return maximum === 0 ? 196 : 16 + (1 - clicks / maximum) * 180;
  }

  protected bucketPercent(clicks: number): number {
    const maximum = this.maxBucketClicks();
    return maximum === 0 ? 0 : (clicks / maximum) * 100;
  }

  protected breakdownPercent(clicks: number, items: readonly AnalyticsCategory[]): number {
    const maximum = Math.max(0, ...items.map((item) => item.clicks));
    return maximum === 0 ? 0 : (clicks / maximum) * 100;
  }

  protected formatBucket(bucket: AnalyticsTimeBucket): string {
    return new Intl.DateTimeFormat(undefined, {
      dateStyle: 'medium',
      timeZone: 'UTC',
    }).format(new Date(bucket.bucketStartUtc));
  }

  protected formatRange(series: AnalyticsTimeSeries): string {
    const formatter = new Intl.DateTimeFormat(undefined, {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      timeZone: 'UTC',
    });
    const inclusiveEnd = new Date(new Date(series.range.toUtc).getTime() - 1);
    return `${formatter.format(new Date(series.range.fromUtc))} – ${formatter.format(inclusiveEnd)} UTC`;
  }

  private load(shortCode: string, days: number): void {
    this.activeLoad?.unsubscribe();
    this.summary.set(null);
    this.series.set(null);
    this.pageError.set(null);
    this.partialError.set(null);

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

    const range = this.utcRange(days);
    this.loading.set(true);
    this.activeLoad = forkJoin({
      summary: this.capture(this.api.analyticsSummary(shortCode, { ...range, topReferrers: 10 })),
      series: this.capture(
        this.api.analyticsTimeSeries(shortCode, { ...range, granularity: 'day' }),
      ),
    })
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(({ summary, series }) => {
        if (summary.error && series.error) {
          const accessError = [summary.error, series.error].find((error) =>
            this.isAccessError(error),
          );
          this.pageError.set(this.describeError(accessError ?? summary.error));
          return;
        }
        const partialFailure = summary.error ?? series.error;
        if (partialFailure && this.isAccessError(partialFailure)) {
          this.pageError.set(this.describeError(partialFailure));
          return;
        }

        this.summary.set(summary.value);
        this.series.set(series.value);

        if (partialFailure) {
          const failedSection = summary.error ? 'summary and breakdowns' : 'trend chart';
          const details = this.describeError(partialFailure);
          this.partialError.set({
            ...details,
            title: `Some analytics could not load`,
            message: `The ${failedSection} is unavailable. ${details.message}`,
            actionLabel: 'Try again',
            action: 'retry',
          });
        }
      });
  }

  private capture<T>(request: Observable<T>): Observable<RequestResult<T>> {
    return request.pipe(
      map((value) => ({ value, error: null })),
      catchError((error: unknown) => of({ value: null, error })),
    );
  }

  private utcRange(days: number): { fromUtc: string; toUtc: string } {
    const now = new Date();
    const toUtc = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() + 1));
    const fromUtc = new Date(toUtc.getTime() - days * 24 * 60 * 60 * 1000);
    return { fromUtc: fromUtc.toISOString(), toUtc: toUtc.toISOString() };
  }

  private parseDays(value: string | null): number {
    const parsed = Number(value);
    return this.isAllowedDays(parsed) ? parsed : 30;
  }

  private isAllowedDays(value: number): boolean {
    return this.rangePresets.some((preset) => preset.days === value);
  }

  private referrerItems(breakdown: AnalyticsBreakdown): readonly AnalyticsCategory[] {
    return breakdown.otherClicks > 0
      ? [
          ...breakdown.items,
          { value: 'Other sources outside the top results', clicks: breakdown.otherClicks },
        ]
      : breakdown.items;
  }

  private formatBucketShort(bucket: AnalyticsTimeBucket): string {
    return new Intl.DateTimeFormat(undefined, {
      month: 'short',
      day: 'numeric',
      timeZone: 'UTC',
    }).format(new Date(bucket.bucketStartUtc));
  }

  private isAccessError(error: unknown): boolean {
    return (
      error instanceof ApiError &&
      (error.kind === 'authentication' ||
        error.kind === 'authorization' ||
        error.kind === 'not-found')
    );
  }

  private describeError(error: unknown): AnalyticsErrorState {
    if (!(error instanceof ApiError)) {
      return {
        title: 'Analytics could not load',
        message: 'An unexpected problem occurred while loading this report.',
        actionLabel: 'Try again',
        action: 'retry',
      };
    }
    if (error.kind === 'not-found' || error.kind === 'authorization') {
      return {
        title: 'Analytics are not available',
        message:
          'This link does not exist, was deleted, or is not available to this account. Link ownership is never disclosed.',
        actionLabel: 'Back to links',
        action: 'links',
      };
    }
    if (error.kind === 'authentication') {
      return {
        title: 'Sign in to view analytics',
        message: 'Your session is no longer available. Sign in again to return to this report.',
        actionLabel: 'Sign in',
        action: 'sign-in',
      };
    }
    if (error.kind === 'rate-limited') {
      const retry = error.retryAfterSeconds
        ? ` Try again in about ${error.retryAfterSeconds} seconds.`
        : ' Wait a moment before trying again.';
      return {
        title: 'Too many analytics requests',
        message: `Analytics are temporarily rate limited.${retry}`,
        actionLabel: 'Try again',
        action: 'retry',
      };
    }
    return {
      title:
        error.kind === 'connectivity' ? 'Could not reach the service' : 'Analytics could not load',
      message: error.message,
      actionLabel: 'Try again',
      action: 'retry',
    };
  }
}
