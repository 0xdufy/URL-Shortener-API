import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../config/api-base-url.token';
import {
  AnalyticsSummary,
  AnalyticsSummaryQuery,
  AnalyticsTimeSeries,
  AnalyticsTimeSeriesQuery,
  CreateShortUrlRequest,
  ShortUrlListQuery,
  ShortUrlListResponse,
  ShortUrlResource,
  ShortUrlStats,
  ShortUrlStatsQuery,
  UpdateShortUrlRequest,
  UpdateShortUrlStatusRequest,
} from './api.models';
import { normalizeApiBaseUrl } from './api-url';

@Injectable({ providedIn: 'root' })
export class ShortUrlsApiClient {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${normalizeApiBaseUrl(inject(API_BASE_URL))}/short-urls`;

  list(query: ShortUrlListQuery = {}): Observable<ShortUrlListResponse> {
    return this.http.get<ShortUrlListResponse>(this.baseUrl, {
      params: this.listParams(query),
    });
  }

  create(request: CreateShortUrlRequest): Observable<ShortUrlResource> {
    return this.http.post<ShortUrlResource>(this.baseUrl, request);
  }

  get(shortCode: string): Observable<ShortUrlResource> {
    return this.http.get<ShortUrlResource>(this.resourceUrl(shortCode));
  }

  update(shortCode: string, request: UpdateShortUrlRequest): Observable<ShortUrlResource> {
    return this.http.put<ShortUrlResource>(this.resourceUrl(shortCode), request);
  }

  updateStatus(
    shortCode: string,
    request: UpdateShortUrlStatusRequest,
  ): Observable<ShortUrlResource> {
    return this.http.patch<ShortUrlResource>(`${this.resourceUrl(shortCode)}/status`, request);
  }

  delete(shortCode: string): Observable<void> {
    return this.http.delete<void>(this.resourceUrl(shortCode));
  }

  restore(shortCode: string): Observable<ShortUrlResource> {
    return this.http.post<ShortUrlResource>(`${this.resourceUrl(shortCode)}/restore`, null);
  }

  stats(shortCode: string, query: ShortUrlStatsQuery = {}): Observable<ShortUrlStats> {
    let params = new HttpParams();
    params = this.setOptional(params, 'fromUtc', query.fromUtc);
    params = this.setOptional(params, 'toUtc', query.toUtc);

    return this.http.get<ShortUrlStats>(`${this.resourceUrl(shortCode)}/stats`, { params });
  }

  analyticsSummary(shortCode: string, query: AnalyticsSummaryQuery): Observable<AnalyticsSummary> {
    let params = new HttpParams().set('fromUtc', query.fromUtc).set('toUtc', query.toUtc);
    params = this.setOptional(params, 'topReferrers', query.topReferrers);

    return this.http.get<AnalyticsSummary>(`${this.resourceUrl(shortCode)}/analytics/summary`, {
      params,
    });
  }

  analyticsTimeSeries(
    shortCode: string,
    query: AnalyticsTimeSeriesQuery,
  ): Observable<AnalyticsTimeSeries> {
    const params = new HttpParams()
      .set('fromUtc', query.fromUtc)
      .set('toUtc', query.toUtc)
      .set('granularity', query.granularity);

    return this.http.get<AnalyticsTimeSeries>(
      `${this.resourceUrl(shortCode)}/analytics/time-series`,
      { params },
    );
  }

  private resourceUrl(shortCode: string): string {
    return `${this.baseUrl}/${encodeURIComponent(shortCode)}`;
  }

  private listParams(query: ShortUrlListQuery): HttpParams {
    let params = new HttpParams();
    params = this.setOptional(params, 'page', query.page);
    params = this.setOptional(params, 'pageSize', query.pageSize);
    params = this.setOptional(params, 'search', query.search);
    params = this.setOptional(params, 'isActive', query.isActive);
    params = this.setOptional(params, 'expiration', query.expiration);
    params = this.setOptional(params, 'includeDeleted', query.includeDeleted);
    params = this.setOptional(params, 'createdFromUtc', query.createdFromUtc);
    params = this.setOptional(params, 'createdToUtc', query.createdToUtc);
    params = this.setOptional(params, 'sortBy', query.sortBy);
    params = this.setOptional(params, 'sortDirection', query.sortDirection);
    return params;
  }

  private setOptional(
    params: HttpParams,
    key: string,
    value: string | number | boolean | undefined,
  ): HttpParams {
    return value === undefined ? params : params.set(key, String(value));
  }
}
