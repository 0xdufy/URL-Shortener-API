export function normalizeApiBaseUrl(apiBaseUrl: string): string {
  return apiBaseUrl.replace(/\/+$/, '');
}

export function isApiUrl(url: string, apiBaseUrl: string): boolean {
  const baseUrl = normalizeApiBaseUrl(apiBaseUrl);
  return url === baseUrl || url.startsWith(`${baseUrl}/`);
}
