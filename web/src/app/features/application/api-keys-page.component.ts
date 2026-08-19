import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  OnInit,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { ApiError } from '../../core/api/api-error';
import { ApiKeysApiClient } from '../../core/api/api-keys-api-client.service';
import {
  ApiKeyCreationResponse,
  ApiKeyResource,
  ApiKeyScope,
  ApiKeyState,
} from '../../core/api/api.models';
import { API_BASE_URL } from '../../core/config/api-base-url.token';
import { normalizeApiBaseUrl } from '../../core/api/api-url';
import { BadgeComponent, BadgeTone } from '../../shared/ui/badge/badge.component';
import { ButtonComponent } from '../../shared/ui/button/button.component';
import { ConfirmationDialogComponent } from '../../shared/ui/confirmation-dialog/confirmation-dialog.component';
import { FieldComponent } from '../../shared/ui/field/field.component';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { PageHeaderComponent } from '../../shared/ui/page-header/page-header.component';
import { StatePanelComponent } from '../../shared/ui/state-panel/state-panel.component';
import { ToastService } from '../../shared/ui/toast/toast.service';

type ScopeControl = 'createShortUrls' | 'readShortUrls' | 'writeShortUrls' | 'readAnalytics';
type ConfirmAction = 'revoke' | 'rotate';

interface ScopeOption {
  readonly value: ApiKeyScope;
  readonly control: ScopeControl;
  readonly label: string;
  readonly description: string;
}

interface PendingAction {
  readonly action: ConfirmAction;
  readonly apiKey: ApiKeyResource;
}

interface PageError {
  readonly title: string;
  readonly message: string;
  readonly signIn: boolean;
}

const SCOPE_OPTIONS: readonly ScopeOption[] = [
  {
    value: 'shorturls:create',
    control: 'createShortUrls',
    label: 'Create short links',
    description: 'Create new short URLs for this account.',
  },
  {
    value: 'shorturls:read',
    control: 'readShortUrls',
    label: 'Read short links',
    description: 'List and inspect short URLs owned by this account.',
  },
  {
    value: 'shorturls:write',
    control: 'writeShortUrls',
    label: 'Manage short links',
    description: 'Update, enable, disable, delete, and restore owned links.',
  },
  {
    value: 'analytics:read',
    control: 'readAnalytics',
    label: 'Read analytics',
    description: 'Read aggregate analytics for owned short links.',
  },
];

@Component({
  selector: 'app-api-keys-page',
  imports: [
    BadgeComponent,
    ButtonComponent,
    ConfirmationDialogComponent,
    DatePipe,
    FieldComponent,
    IconComponent,
    PageHeaderComponent,
    ReactiveFormsModule,
    StatePanelComponent,
  ],
  template: `
    <div class="page-stack">
      @if (oneTimeCredential(); as reveal) {
        <section class="surface secret-card" aria-labelledby="secret-title">
          <span class="secret-icon" aria-hidden="true"><app-icon name="key" /></span>
          <p class="eyebrow">One-time secret</p>
          <h1 id="secret-title">
            {{
              revealKind() === 'rotation'
                ? 'Your replacement key is ready'
                : 'Your API key is ready'
            }}
          </h1>
          <p class="secret-warning" role="alert">
            Copy this key now. For your security, it cannot be viewed or recovered after you leave
            this screen.
          </p>

          <div class="secret-value">
            <code>{{ reveal.key }}</code>
            <button type="button" (click)="copyText(reveal.key, 'API key copied')">Copy key</button>
          </div>

          <div class="transient-example">
            <div class="example-heading">
              <div>
                <h2>Try it with cURL</h2>
                <p>This example contains the new key and disappears with this screen.</p>
              </div>
              <button
                type="button"
                (click)="copyText(transientUsageExample(), 'API request copied')"
              >
                Copy example
              </button>
            </div>
            <pre><code>{{ transientUsageExample() }}</code></pre>
          </div>

          <label class="acknowledgement">
            <input
              type="checkbox"
              [checked]="secretAcknowledged()"
              (change)="setSecretAcknowledged($event)"
            />
            <span>
              <strong>I saved this key securely</strong>
              <small>I understand that Shortly cannot show it to me again.</small>
            </span>
          </label>

          <app-button [disabled]="!secretAcknowledged()" icon="check" (click)="dismissSecret()">
            I’ve saved my key
          </app-button>
        </section>
      } @else {
        <app-page-header
          eyebrow="Developer access"
          title="API keys"
          description="Create scoped credentials for programmatic access without sharing your account password."
        >
          <app-button icon="plus" (click)="openCreateForm()">Create API key</app-button>
        </app-page-header>

        @if (showCreateForm()) {
          <section class="surface create-card" aria-labelledby="create-key-title">
            <div class="section-heading-row">
              <div>
                <h2 id="create-key-title">Create an API key</h2>
                <p>Choose only the capabilities this integration needs.</p>
              </div>
              <button
                class="close-button"
                type="button"
                aria-label="Close create form"
                (click)="closeCreateForm()"
              >
                <app-icon name="close" />
              </button>
            </div>

            @if (formError(); as alert) {
              <div class="form-alert" role="alert" tabindex="-1">
                <strong>{{ alert }}</strong>
              </div>
            }

            <form [formGroup]="form" (ngSubmit)="create()" novalidate>
              <div class="form-row">
                <app-field
                  controlId="api-key-name"
                  label="Key name"
                  hint="Use a name that identifies the app or environment."
                  [error]="nameError()"
                >
                  <input
                    id="api-key-name"
                    class="form-control"
                    type="text"
                    formControlName="name"
                    autocomplete="off"
                    maxlength="64"
                    placeholder="Production deployment"
                    [attr.aria-invalid]="nameError() ? true : null"
                  />
                </app-field>

                <app-field
                  controlId="api-key-expiry"
                  label="Expiration (optional)"
                  hint="Date and time are interpreted in your local time zone."
                  [error]="expiryError()"
                >
                  <input
                    id="api-key-expiry"
                    class="form-control"
                    type="datetime-local"
                    formControlName="expiresAtUtc"
                    [min]="minimumExpiry"
                    [attr.aria-invalid]="expiryError() ? true : null"
                  />
                </app-field>
              </div>

              <fieldset
                class="scope-fieldset"
                [attr.aria-describedby]="scopeError() ? 'scope-error' : null"
              >
                <legend>Scopes</legend>
                <p>Select at least one. Scopes apply only to resources owned by this account.</p>
                <div class="scope-grid">
                  @for (scope of scopeOptions; track scope.value) {
                    <label [class.selected]="form.controls[scope.control].value">
                      <input type="checkbox" [formControlName]="scope.control" />
                      <span>
                        <strong>{{ scope.label }}</strong>
                        <code>{{ scope.value }}</code>
                        <small>{{ scope.description }}</small>
                      </span>
                    </label>
                  }
                </div>
                @if (scopeError()) {
                  <p id="scope-error" class="field-error" role="alert">
                    Select at least one scope.
                  </p>
                }
              </fieldset>

              <div class="form-actions">
                <app-button type="submit" [loading]="creating()" icon="key">Create key</app-button>
                <app-button variant="quiet" [disabled]="creating()" (click)="closeCreateForm()">
                  Cancel
                </app-button>
              </div>
            </form>
          </section>
        }

        <div class="content-grid">
          <section
            class="surface keys-card"
            aria-labelledby="owned-keys-title"
            [attr.aria-busy]="loading()"
          >
            <div class="section-heading-row">
              <div>
                <h2 id="owned-keys-title">Your keys</h2>
                <p>Only public prefixes and non-secret metadata are stored and shown here.</p>
              </div>
              @if (!loading()) {
                <span class="key-count">{{ activeCount() }} active</span>
              }
            </div>

            @if (loading()) {
              <app-state-panel
                kind="loading"
                title="Loading API keys"
                message="Retrieving safe key metadata."
              />
            } @else if (pageError(); as error) {
              <app-state-panel
                kind="error"
                [title]="error.title"
                [message]="error.message"
                [actionLabel]="error.signIn ? 'Sign in' : 'Try again'"
                (action)="handleErrorAction(error)"
              />
            } @else if (apiKeys().length === 0) {
              <app-state-panel
                kind="empty"
                title="No API keys yet"
                message="Create a scoped key when you are ready to connect an integration."
                actionLabel="Create API key"
                (action)="openCreateForm()"
              />
            } @else {
              <div class="key-list">
                @for (apiKey of apiKeys(); track apiKey.id) {
                  <article
                    class="key-item"
                    [class.state-active]="apiKey.state === 'active'"
                    [class.state-expired]="apiKey.state === 'expired'"
                    [class.state-revoked]="apiKey.state === 'revoked'"
                  >
                    <div class="key-main">
                      <div class="key-title-row">
                        <h3>{{ apiKey.name }}</h3>
                        <app-badge [tone]="stateTone(apiKey.state)">{{
                          stateLabel(apiKey.state)
                        }}</app-badge>
                      </div>
                      <code class="key-prefix">{{ apiKey.prefix }}</code>
                      <div class="scope-list" aria-label="Granted scopes">
                        @for (scope of apiKey.scopes; track scope) {
                          <span>{{ scope }}</span>
                        }
                      </div>
                    </div>

                    <dl class="metadata">
                      <div>
                        <dt>Created</dt>
                        <dd>{{ apiKey.createdAtUtc | date: 'medium' }}</dd>
                      </div>
                      <div>
                        <dt>Expires</dt>
                        <dd>
                          {{
                            apiKey.expiresAtUtc ? (apiKey.expiresAtUtc | date: 'medium') : 'Never'
                          }}
                        </dd>
                      </div>
                      <div>
                        <dt>Last used</dt>
                        <dd>
                          {{
                            apiKey.lastUsedAtUtc ? (apiKey.lastUsedAtUtc | date: 'medium') : 'Never'
                          }}
                        </dd>
                      </div>
                      @if (apiKey.revokedAtUtc) {
                        <div>
                          <dt>Revoked</dt>
                          <dd>{{ apiKey.revokedAtUtc | date: 'medium' }}</dd>
                        </div>
                      }
                    </dl>

                    @if (apiKey.state === 'active') {
                      <div class="key-actions">
                        <app-button
                          variant="secondary"
                          [disabled]="busyKeyId() !== null"
                          [loading]="busyKeyId() === apiKey.id && pendingOperation() === 'rotate'"
                          (click)="confirm('rotate', apiKey)"
                        >
                          Rotate
                        </app-button>
                        <app-button
                          variant="danger"
                          [disabled]="busyKeyId() !== null"
                          [loading]="busyKeyId() === apiKey.id && pendingOperation() === 'revoke'"
                          (click)="confirm('revoke', apiKey)"
                        >
                          Revoke
                        </app-button>
                      </div>
                    }
                  </article>
                }
              </div>
            }
          </section>

          <aside class="surface guidance-card" aria-labelledby="use-api-key-title">
            <p class="eyebrow">Quick start</p>
            <h2 id="use-api-key-title">Use an API key</h2>
            <p>Keep keys in a secret manager or environment variable, never in source control.</p>
            <ol>
              <li>Save the one-time key as <code>SHORTLY_API_KEY</code>.</li>
              <li>
                Send it in the <code>Authorization</code> header using the
                <code>ApiKey</code> scheme.
              </li>
              <li>Rotate a key immediately if it may have been exposed.</li>
            </ol>
            <div class="generic-example">
              <pre><code>{{ genericUsageExample }}</code></pre>
              <button type="button" (click)="copyText(genericUsageExample, 'API example copied')">
                Copy example
              </button>
            </div>
            <p class="placeholder-note">
              This saved example uses an environment-variable placeholder; it never contains an
              existing key.
            </p>
          </aside>
        </div>

        <app-confirmation-dialog
          #confirmationDialog
          [title]="confirmationTitle()"
          [message]="confirmationMessage()"
          [confirmLabel]="pendingAction()?.action === 'rotate' ? 'Rotate key' : 'Revoke key'"
          (confirmed)="runConfirmedAction()"
          (cancelled)="pendingAction.set(null)"
        />
      }
    </div>
  `,
  styleUrl: './api-keys-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApiKeysPageComponent implements OnInit {
  private readonly api = inject(ApiKeysApiClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly formBuilder = inject(FormBuilder);
  private readonly apiBaseUrl = normalizeApiBaseUrl(inject(API_BASE_URL));

  protected readonly scopeOptions = SCOPE_OPTIONS;
  protected readonly minimumExpiry = toLocalDateTimeInput(new Date(Date.now() + 60_000));
  protected readonly genericUsageExample = `curl --request GET "${this.apiBaseUrl}/short-urls" --header "Authorization: ApiKey $SHORTLY_API_KEY"`;
  protected readonly apiKeys = signal<readonly ApiKeyResource[]>([]);
  protected readonly loading = signal(true);
  protected readonly pageError = signal<PageError | null>(null);
  protected readonly showCreateForm = signal(false);
  protected readonly creating = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly oneTimeCredential = signal<ApiKeyCreationResponse | null>(null);
  protected readonly revealKind = signal<'creation' | 'rotation'>('creation');
  protected readonly secretAcknowledged = signal(false);
  protected readonly pendingAction = signal<PendingAction | null>(null);
  protected readonly busyKeyId = signal<string | null>(null);
  protected readonly pendingOperation = signal<ConfirmAction | null>(null);
  protected readonly activeCount = computed(
    () => this.apiKeys().filter((apiKey) => apiKey.state === 'active').length,
  );
  protected readonly transientUsageExample = computed(() => {
    const key = this.oneTimeCredential()?.key ?? '';
    return `curl --request GET "${this.apiBaseUrl}/short-urls" --header "Authorization: ApiKey ${key}"`;
  });
  protected readonly confirmationTitle = computed(() =>
    this.pendingAction()?.action === 'rotate' ? 'Rotate this API key?' : 'Revoke this API key?',
  );
  protected readonly confirmationMessage = computed(() => {
    const pending = this.pendingAction();
    if (!pending) {
      return '';
    }
    return pending.action === 'rotate'
      ? `“${pending.apiKey.name}” will stop working immediately. You must save and deploy the replacement key.`
      : `“${pending.apiKey.name}” will stop working immediately. This action cannot be undone.`;
  });

  protected readonly form = this.formBuilder.nonNullable.group({
    name: [
      '',
      [
        Validators.required,
        Validators.maxLength(64),
        Validators.pattern(/^[A-Za-z0-9][A-Za-z0-9 ._-]*$/),
        noSurroundingWhitespace,
      ],
    ],
    expiresAtUtc: ['', futureLocalDateTime],
    createShortUrls: false,
    readShortUrls: false,
    writeShortUrls: false,
    readAnalytics: false,
  });

  private readonly confirmationDialog =
    viewChild<ConfirmationDialogComponent>('confirmationDialog');

  ngOnInit(): void {
    this.load();
  }

  @HostListener('window:beforeunload', ['$event'])
  protected warnBeforeLeaving(event: BeforeUnloadEvent): void {
    if (this.oneTimeCredential()) {
      event.preventDefault();
    }
  }

  protected openCreateForm(): void {
    this.showCreateForm.set(true);
    this.formError.set(null);
    queueMicrotask(() =>
      this.elementRef.nativeElement.querySelector<HTMLElement>('#api-key-name')?.focus(),
    );
  }

  protected closeCreateForm(): void {
    if (this.creating()) {
      return;
    }
    this.showCreateForm.set(false);
    this.formError.set(null);
    this.form.reset();
  }

  protected nameError(): string | undefined {
    const control = this.form.controls.name;
    if (!(control.touched || control.dirty) || !control.errors) {
      return undefined;
    }
    if (control.hasError('required')) {
      return 'Enter a name for this key.';
    }
    if (control.hasError('maxlength')) {
      return 'Use 64 characters or fewer.';
    }
    return 'Begin with a letter or digit and use only letters, digits, spaces, dots, underscores, or hyphens.';
  }

  protected expiryError(): string | undefined {
    const control = this.form.controls.expiresAtUtc;
    return (control.touched || control.dirty) && control.invalid
      ? 'Choose a date and time in the future.'
      : undefined;
  }

  protected scopeError(): boolean {
    return this.form.touched && this.selectedScopes().length === 0;
  }

  protected create(): void {
    this.form.markAllAsTouched();
    const scopes = this.selectedScopes();
    if (this.form.invalid || scopes.length === 0 || this.creating()) {
      this.formError.set('Check the highlighted fields and select at least one scope.');
      this.focusFirstInvalid();
      return;
    }

    this.creating.set(true);
    this.formError.set(null);
    const value = this.form.getRawValue();
    this.api
      .create({
        name: value.name,
        scopes,
        expiresAtUtc: value.expiresAtUtc ? new Date(value.expiresAtUtc).toISOString() : null,
      })
      .pipe(
        finalize(() => this.creating.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.apiKeys.update((apiKeys) => [response.apiKey, ...apiKeys]);
          this.creating.set(false);
          this.closeCreateForm();
          this.revealCredential(response, 'creation');
        },
        error: (error: unknown) => this.handleMutationError(error, 'API key could not be created.'),
      });
  }

  protected confirm(action: ConfirmAction, apiKey: ApiKeyResource): void {
    this.pendingAction.set({ action, apiKey });
    queueMicrotask(() => this.confirmationDialog()?.open());
  }

  protected runConfirmedAction(): void {
    const pending = this.pendingAction();
    this.pendingAction.set(null);
    if (!pending || this.busyKeyId()) {
      return;
    }

    this.busyKeyId.set(pending.apiKey.id);
    this.pendingOperation.set(pending.action);
    if (pending.action === 'revoke') {
      this.api
        .revoke(pending.apiKey.id)
        .pipe(
          finalize(() => this.finishOperation()),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe({
          next: () => {
            this.markRevoked(pending.apiKey.id);
            this.toast.show('API key revoked', 'The key can no longer authenticate requests.');
          },
          error: (error: unknown) =>
            this.handleMutationError(error, 'API key could not be revoked.'),
        });
      return;
    }

    this.api
      .rotate(pending.apiKey.id)
      .pipe(
        finalize(() => this.finishOperation()),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.markRevoked(pending.apiKey.id, response.apiKey.id);
          this.apiKeys.update((apiKeys) => [response.apiKey, ...apiKeys]);
          this.revealCredential(response, 'rotation');
        },
        error: (error: unknown) => this.handleMutationError(error, 'API key could not be rotated.'),
      });
  }

  protected setSecretAcknowledged(event: Event): void {
    this.secretAcknowledged.set((event.target as HTMLInputElement).checked);
  }

  protected dismissSecret(): void {
    if (!this.secretAcknowledged()) {
      return;
    }
    this.oneTimeCredential.set(null);
    this.secretAcknowledged.set(false);
    this.toast.show('Key saved', 'Only its safe metadata will be shown from now on.');
  }

  protected async copyText(value: string, title: string): Promise<void> {
    try {
      if (!navigator.clipboard?.writeText) {
        throw new Error('Clipboard API unavailable.');
      }
      await navigator.clipboard.writeText(value);
      this.toast.show(title, 'Copied to your clipboard.');
    } catch {
      this.toast.show('Copy failed', 'Select the text and copy it manually.', 'error');
    }
  }

  protected stateTone(state: ApiKeyState): BadgeTone {
    return state === 'active' ? 'success' : state === 'expired' ? 'warning' : 'danger';
  }

  protected stateLabel(state: ApiKeyState): string {
    return state.charAt(0).toUpperCase() + state.slice(1);
  }

  protected handleErrorAction(error: PageError): void {
    if (error.signIn) {
      void this.router.navigate(['/auth/sign-in'], {
        queryParams: { returnUrl: this.router.url },
        replaceUrl: true,
      });
      return;
    }
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.pageError.set(null);
    this.api
      .list()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (apiKeys) => this.apiKeys.set(apiKeys),
        error: (error: unknown) => this.pageError.set(this.describePageError(error)),
      });
  }

  private selectedScopes(): ApiKeyScope[] {
    return SCOPE_OPTIONS.filter(({ control }) => this.form.controls[control].value).map(
      ({ value }) => value,
    );
  }

  private revealCredential(response: ApiKeyCreationResponse, kind: 'creation' | 'rotation'): void {
    this.revealKind.set(kind);
    this.secretAcknowledged.set(false);
    this.oneTimeCredential.set(response);
    queueMicrotask(() =>
      this.elementRef.nativeElement.querySelector<HTMLElement>('.secret-value button')?.focus(),
    );
  }

  private markRevoked(apiKeyId: string, replacedByApiKeyId: string | null = null): void {
    const revokedAtUtc = new Date().toISOString();
    this.apiKeys.update((apiKeys) =>
      apiKeys.map((apiKey) =>
        apiKey.id === apiKeyId
          ? { ...apiKey, state: 'revoked', revokedAtUtc, replacedByApiKeyId }
          : apiKey,
      ),
    );
  }

  private finishOperation(): void {
    this.busyKeyId.set(null);
    this.pendingOperation.set(null);
  }

  private handleMutationError(error: unknown, fallback: string): void {
    const message = error instanceof ApiError ? error.message : fallback;
    this.formError.set(message);
    this.toast.show('Request failed', message, 'error');
    if (error instanceof ApiError && error.code === 'API_KEY_STATE_CONFLICT') {
      this.load();
    }
  }

  private describePageError(error: unknown): PageError {
    if (error instanceof ApiError && error.kind === 'authentication') {
      return {
        title: 'Your session has ended',
        message: 'Sign in again to manage API keys.',
        signIn: true,
      };
    }
    if (error instanceof ApiError && error.kind === 'rate-limited') {
      return {
        title: 'Too many requests',
        message: error.retryAfterSeconds
          ? `Try again in about ${error.retryAfterSeconds} seconds.`
          : 'Wait a moment, then try again.',
        signIn: false,
      };
    }
    return {
      title: 'API keys could not load',
      message:
        error instanceof ApiError ? error.message : 'An unexpected problem occurred. Try again.',
      signIn: false,
    };
  }

  private focusFirstInvalid(): void {
    queueMicrotask(() => {
      const selector = this.form.controls.name.invalid
        ? '#api-key-name'
        : this.form.controls.expiresAtUtc.invalid
          ? '#api-key-expiry'
          : '.scope-fieldset input';
      this.elementRef.nativeElement.querySelector<HTMLElement>(selector)?.focus();
    });
  }
}

function futureLocalDateTime(control: AbstractControl<string>): ValidationErrors | null {
  if (!control.value) {
    return null;
  }
  const timestamp = new Date(control.value).getTime();
  return Number.isFinite(timestamp) && timestamp > Date.now() ? null : { futureDateTime: true };
}

function noSurroundingWhitespace(control: AbstractControl<string>): ValidationErrors | null {
  return control.value === control.value.trim() ? null : { surroundingWhitespace: true };
}

function toLocalDateTimeInput(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(
    date.getHours(),
  )}:${pad(date.getMinutes())}`;
}
