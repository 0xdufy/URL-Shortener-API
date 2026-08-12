const protectedApplicationPrefix = '/app';

export function safeReturnUrl(value: string | null | undefined): string {
  if (
    value === protectedApplicationPrefix ||
    value?.startsWith(`${protectedApplicationPrefix}/`) ||
    value?.startsWith(`${protectedApplicationPrefix}?`)
  ) {
    return value;
  }

  return '/app/dashboard';
}
