import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../config/api-base-url.token';
import { CustomDomainResource, RegisterCustomDomainRequest } from './api.models';
import { normalizeApiBaseUrl } from './api-url';

@Injectable({ providedIn: 'root' })
export class CustomDomainsApiClient {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${normalizeApiBaseUrl(inject(API_BASE_URL))}/custom-domains`;

  list(): Observable<readonly CustomDomainResource[]> {
    return this.http.get<readonly CustomDomainResource[]>(this.baseUrl);
  }

  register(request: RegisterCustomDomainRequest): Observable<CustomDomainResource> {
    return this.http.post<CustomDomainResource>(this.baseUrl, request);
  }

  requestVerification(customDomainId: string): Observable<CustomDomainResource> {
    return this.http.post<CustomDomainResource>(
      `${this.resourceUrl(customDomainId)}/verification/request`,
      null,
    );
  }

  checkVerification(customDomainId: string): Observable<CustomDomainResource> {
    return this.http.post<CustomDomainResource>(
      `${this.resourceUrl(customDomainId)}/verification/check`,
      null,
    );
  }

  disable(customDomainId: string): Observable<CustomDomainResource> {
    return this.http.post<CustomDomainResource>(
      `${this.resourceUrl(customDomainId)}/disable`,
      null,
    );
  }

  private resourceUrl(customDomainId: string): string {
    return `${this.baseUrl}/${encodeURIComponent(customDomainId)}`;
  }
}
