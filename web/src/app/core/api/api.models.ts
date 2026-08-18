export interface ApiErrorEnvelope {
  readonly traceId: string;
  readonly error: {
    readonly code: string;
    readonly message: string;
    readonly details: readonly {
      readonly field: string;
      readonly message: string;
    }[];
  };
}

export interface AuthenticatedUser {
  readonly id: string;
  readonly email: string;
  readonly createdAtUtc: string;
}

export interface AuthenticationSession {
  readonly accessToken: string;
  readonly tokenType: 'Bearer';
  readonly accessTokenExpiresAtUtc: string;
  readonly refreshSessionExpiresAtUtc: string;
  readonly csrfToken: string;
  readonly user: AuthenticatedUser;
}

export interface CurrentAuthenticationSession {
  readonly sessionId: string;
  readonly refreshSessionCreatedAtUtc: string;
  readonly refreshSessionExpiresAtUtc: string;
  readonly isRefreshSessionRevoked: boolean;
  readonly user: AuthenticatedUser;
}

export interface CredentialsRequest {
  readonly email: string;
  readonly password: string;
}

export interface BrowserAuthenticationBootstrap {
  readonly csrfToken: string;
  readonly publicRegistrationEnabled: boolean;
  readonly passwordRequiredLength: number;
  readonly passwordRequiredUniqueChars: number;
}

export interface ShortUrlResource {
  readonly id: string;
  readonly originalUrl: string;
  readonly shortCode: string;
  readonly shortUrl: string;
  readonly createdAtUtc: string;
  readonly expiresAtUtc: string | null;
  readonly isActive: boolean;
  readonly isExpired: boolean;
  readonly isDeleted: boolean;
  readonly deletedAtUtc: string | null;
  readonly restoreUntilUtc: string | null;
  readonly clickCount: number;
  readonly lastAccessedAtUtc: string | null;
}

export interface ShortUrlListItem {
  readonly id: string;
  readonly originalUrl: string;
  readonly shortCode: string;
  readonly shortUrl: string;
  readonly createdAtUtc: string;
  readonly expiresAtUtc: string | null;
  readonly isActive: boolean;
  readonly isExpired: boolean;
  readonly isDeleted: boolean;
  readonly deletedAtUtc: string | null;
  readonly restoreUntilUtc: string | null;
  readonly clickCount: number;
}

export type ExpirationFilter = 'all' | 'expired' | 'notExpired';
export type ShortUrlSort = 'createdAt' | 'shortCode' | 'clickCount' | 'expiresAt';
export type SortDirection = 'asc' | 'desc';

export interface ShortUrlListQuery {
  readonly page?: number;
  readonly pageSize?: number;
  readonly search?: string;
  readonly isActive?: boolean;
  readonly expiration?: ExpirationFilter;
  readonly includeDeleted?: boolean;
  readonly createdFromUtc?: string;
  readonly createdToUtc?: string;
  readonly sortBy?: ShortUrlSort;
  readonly sortDirection?: SortDirection;
}

export interface ShortUrlListResponse {
  readonly items: readonly ShortUrlListItem[];
  readonly pagination: {
    readonly page: number;
    readonly pageSize: number;
    readonly totalItems: number;
    readonly totalPages: number;
    readonly hasPreviousPage: boolean;
    readonly hasNextPage: boolean;
  };
  readonly filters: {
    readonly search: string | null;
    readonly isActive: boolean | null;
    readonly expiration: ExpirationFilter;
    readonly includeDeleted: boolean;
    readonly createdFromUtc: string | null;
    readonly createdToUtc: string | null;
    readonly sortBy: ShortUrlSort;
    readonly sortDirection: SortDirection;
  };
}

export interface CreateShortUrlRequest {
  readonly originalUrl: string;
  readonly customAlias?: string | null;
  readonly expiresAtUtc?: string | null;
}

export interface UpdateShortUrlRequest {
  readonly originalUrl: string;
  readonly expiresAtUtc: string | null;
}

export interface UpdateShortUrlStatusRequest {
  readonly isActive: boolean;
}

export interface ShortUrlStatsQuery {
  readonly fromUtc?: string;
  readonly toUtc?: string;
}

export interface ShortUrlStats {
  readonly shortCode: string;
  readonly totalClicks: number;
  readonly fromUtc: string;
  readonly toUtc: string;
  readonly dailyClicks: readonly {
    readonly dateUtc: string;
    readonly clicks: number;
  }[];
}

export interface AnalyticsRange {
  readonly fromUtc: string;
  readonly toUtc: string;
  readonly boundarySemantics: '[fromUtc,toUtc)' | string;
  readonly timeZone: 'UTC' | string;
}

export interface AnalyticsFreshness {
  readonly consistency: 'eventual' | string;
  readonly generatedAtUtc: string;
  readonly lastAggregatedAtUtc: string | null;
  readonly includesOpenBucket: boolean;
  readonly isPartial: boolean;
}

export interface AnalyticsCategory {
  readonly value: string;
  readonly clicks: number;
}

export interface AnalyticsBreakdown {
  readonly items: readonly AnalyticsCategory[];
  readonly otherClicks: number;
  readonly isTruncated: boolean;
}

export interface AnalyticsSummary {
  readonly shortCode: string;
  readonly range: AnalyticsRange;
  readonly totalClicks: number;
  readonly uniqueVisitorsEstimate: number;
  readonly uniqueVisitorMethod: 'sumOfDailyPseudonymousVisitors' | string;
  readonly referrers: AnalyticsBreakdown;
  readonly devices: AnalyticsBreakdown;
  readonly browsers: AnalyticsBreakdown;
  readonly operatingSystems: AnalyticsBreakdown;
  readonly freshness: AnalyticsFreshness;
}

export interface AnalyticsTimeBucket {
  readonly bucketStartUtc: string;
  readonly bucketEndUtc: string;
  readonly clicks: number;
}

export interface AnalyticsTimeSeries {
  readonly shortCode: string;
  readonly range: AnalyticsRange;
  readonly granularity: 'hour' | 'day';
  readonly totalClicks: number;
  readonly buckets: readonly AnalyticsTimeBucket[];
  readonly freshness: AnalyticsFreshness;
}

export interface AnalyticsRangeQuery {
  readonly fromUtc: string;
  readonly toUtc: string;
}

export interface AnalyticsSummaryQuery extends AnalyticsRangeQuery {
  readonly topReferrers?: number;
}

export interface AnalyticsTimeSeriesQuery extends AnalyticsRangeQuery {
  readonly granularity: 'hour' | 'day';
}
