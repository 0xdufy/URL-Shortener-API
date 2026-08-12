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
