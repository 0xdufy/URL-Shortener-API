import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../config/api-base-url.token';
import { ApiKeyCreationResponse, ApiKeyResource, CreateApiKeyRequest } from './api.models';
import { normalizeApiBaseUrl } from './api-url';

@Injectable({ providedIn: 'root' })
export class ApiKeysApiClient {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${normalizeApiBaseUrl(inject(API_BASE_URL))}/api-keys`;

  list(): Observable<readonly ApiKeyResource[]> {
    return this.http.get<readonly ApiKeyResource[]>(this.baseUrl);
  }

  create(request: CreateApiKeyRequest): Observable<ApiKeyCreationResponse> {
    return this.http.post<ApiKeyCreationResponse>(this.baseUrl, request);
  }

  revoke(apiKeyId: string): Observable<void> {
    return this.http.delete<void>(this.resourceUrl(apiKeyId));
  }

  rotate(apiKeyId: string): Observable<ApiKeyCreationResponse> {
    return this.http.post<ApiKeyCreationResponse>(`${this.resourceUrl(apiKeyId)}/rotate`, null);
  }

  private resourceUrl(apiKeyId: string): string {
    return `${this.baseUrl}/${encodeURIComponent(apiKeyId)}`;
  }
}
