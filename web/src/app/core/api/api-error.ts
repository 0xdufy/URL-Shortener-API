export type ApiFailureKind =
  | 'authentication'
  | 'authorization'
  | 'validation'
  | 'not-found'
  | 'conflict'
  | 'gone'
  | 'rate-limited'
  | 'connectivity'
  | 'service'
  | 'unexpected';

export interface ApiErrorDetail {
  readonly field: string;
  readonly message: string;
}

export interface ApiErrorOptions {
  readonly status: number;
  readonly code: string;
  readonly message: string;
  readonly details?: readonly ApiErrorDetail[];
  readonly traceId?: string;
  readonly clientRequestId?: string;
  readonly retryAfterSeconds?: number;
  readonly kind: ApiFailureKind;
  readonly isUserActionable: boolean;
}

export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly details: readonly ApiErrorDetail[];
  readonly traceId?: string;
  readonly clientRequestId?: string;
  readonly retryAfterSeconds?: number;
  readonly kind: ApiFailureKind;
  readonly isUserActionable: boolean;

  constructor(options: ApiErrorOptions) {
    super(options.message);
    this.name = 'ApiError';
    this.status = options.status;
    this.code = options.code;
    this.details = options.details ?? [];
    this.traceId = options.traceId;
    this.clientRequestId = options.clientRequestId;
    this.retryAfterSeconds = options.retryAfterSeconds;
    this.kind = options.kind;
    this.isUserActionable = options.isUserActionable;
  }

  validationMessages(field: string): readonly string[] {
    return this.details.filter((detail) => detail.field === field).map((detail) => detail.message);
  }

  validationErrors(): Readonly<Record<string, readonly string[]>> {
    const errors: Record<string, string[]> = {};

    for (const detail of this.details) {
      errors[detail.field] = [...(errors[detail.field] ?? []), detail.message];
    }

    return errors;
  }
}
