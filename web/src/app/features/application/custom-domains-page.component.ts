import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
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
import { CustomDomainResource, CustomDomainStatus } from '../../core/api/api.models';
import { CustomDomainsApiClient } from '../../core/api/custom-domains-api-client.service';
import { BadgeComponent, BadgeTone } from '../../shared/ui/badge/badge.component';
import { ButtonComponent } from '../../shared/ui/button/button.component';
import { ConfirmationDialogComponent } from '../../shared/ui/confirmation-dialog/confirmation-dialog.component';
import { FieldComponent } from '../../shared/ui/field/field.component';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { PageHeaderComponent } from '../../shared/ui/page-header/page-header.component';
import { StatePanelComponent } from '../../shared/ui/state-panel/state-panel.component';
import { ToastService } from '../../shared/ui/toast/toast.service';

interface PageError {
  readonly title: string;
  readonly message: string;
  readonly signIn: boolean;
}

type DomainOperation = 'check' | 'request' | 'disable';

@Component({
  selector: 'app-custom-domains-page',
  imports: [
    BadgeComponent,
    ButtonComponent,
    ConfirmationDialogComponent,
    FieldComponent,
    IconComponent,
    PageHeaderComponent,
    ReactiveFormsModule,
    StatePanelComponent,
  ],
  template: `
    <div class="page-stack">
      <app-page-header
        eyebrow="Branded links"
        title="Custom domains"
        description="Register a hostname, prove control with DNS, and use verified domains for your links."
      />

      <div class="content-grid">
        <main class="main-stack">
          <section class="surface register-card" aria-labelledby="register-domain-title">
            <div class="section-heading-row">
              <div>
                <h2 id="register-domain-title">Register a domain</h2>
                <p>Enter a hostname only—without a scheme, port, path, or wildcard.</p>
              </div>
              <span class="domain-count">{{ domains().length }}</span>
            </div>

            @if (formError()) {
              <div class="form-alert" role="alert" tabindex="-1">{{ formError() }}</div>
            }

            <form [formGroup]="form" (ngSubmit)="register()" novalidate>
              <app-field
                controlId="domain-host"
                label="Domain hostname"
                hint="For example: go.example.com"
                [error]="hostError()"
              >
                <input
                  id="domain-host"
                  class="form-control"
                  type="text"
                  formControlName="host"
                  inputmode="url"
                  autocomplete="url"
                  maxlength="253"
                  placeholder="go.example.com"
                  [attr.aria-describedby]="hostError() ? 'domain-host-error' : 'domain-host-hint'"
                  [attr.aria-invalid]="hostError() ? true : null"
                />
              </app-field>

              @if (normalizedPreview(); as host) {
                <p class="normalization-preview" role="status">
                  <app-icon name="info" />
                  This claim will be registered as <strong>{{ host }}</strong
                  >.
                </p>
              }

              <div class="form-actions">
                <app-button type="submit" icon="plus" [loading]="registering()">
                  Register domain
                </app-button>
              </div>
            </form>
          </section>

          <section class="surface domains-card" aria-labelledby="registered-domains-title">
            <div class="section-heading-row">
              <div>
                <h2 id="registered-domains-title">Registered domains</h2>
                <p>Verification status only changes after the service checks the DNS record.</p>
              </div>
              <button class="refresh-button" type="button" (click)="load()" [disabled]="loading()">
                Refresh list
              </button>
            </div>

            @if (loading()) {
              <app-state-panel
                kind="loading"
                title="Loading domains"
                message="Retrieving your domain claims and current verification states."
              />
            } @else if (pageError(); as error) {
              <app-state-panel
                kind="error"
                [title]="error.title"
                [message]="error.message"
                [actionLabel]="error.signIn ? 'Sign in' : 'Try again'"
                (action)="handlePageError(error)"
              />
            } @else if (domains().length === 0) {
              <app-state-panel
                kind="empty"
                title="No custom domains yet"
                message="Register your first hostname above to receive its DNS verification record."
              />
            } @else {
              <div class="domain-list">
                @for (domain of domains(); track domain.id) {
                  <article
                    class="domain-item"
                    [class]="'domain-item state-' + domain.status"
                    [attr.data-domain-id]="domain.id"
                    tabindex="-1"
                  >
                    <div class="domain-heading">
                      <div class="domain-title">
                        <span class="domain-icon" aria-hidden="true"
                          ><app-icon name="domains"
                        /></span>
                        <div>
                          <h3>{{ domain.host }}</h3>
                          <p>Registered {{ formatDate(domain.createdAtUtc) }}</p>
                        </div>
                      </div>
                      <app-badge [tone]="statusTone(domain.status)">
                        {{ statusLabel(domain.status) }}
                      </app-badge>
                    </div>

                    <p class="status-message">{{ statusMessage(domain) }}</p>

                    @if (domain.verificationFailure; as failure) {
                      <div class="verification-failure" role="status">
                        <app-icon name="warning" />
                        <div>
                          <strong>{{ failureTitle(failure.code) }}</strong>
                          <p>{{ failureGuidance(failure.code, failure.message) }}</p>
                        </div>
                      </div>
                    }

                    @if (domain.status !== 'disabled') {
                      <div class="dns-record" aria-label="DNS verification record">
                        <div class="record-heading">
                          <div>
                            <p class="record-eyebrow">DNS verification record</p>
                            <h4>Add this exact TXT record</h4>
                          </div>
                          <button
                            type="button"
                            (click)="copyRecord(domain)"
                            [attr.aria-label]="'Copy DNS record for ' + domain.host"
                          >
                            Copy record
                          </button>
                        </div>
                        <dl>
                          <div>
                            <dt>Type</dt>
                            <dd>
                              <code>{{ domain.verificationRecord.type }}</code>
                            </dd>
                          </div>
                          <div>
                            <dt>Name</dt>
                            <dd>
                              <code>{{ domain.verificationRecord.name }}</code>
                              <button
                                type="button"
                                (click)="
                                  copyText(domain.verificationRecord.name, 'Record name copied')
                                "
                              >
                                Copy
                              </button>
                            </dd>
                          </div>
                          <div>
                            <dt>Value</dt>
                            <dd>
                              <code>{{ domain.verificationRecord.value }}</code>
                              <button
                                type="button"
                                (click)="
                                  copyText(domain.verificationRecord.value, 'Record value copied')
                                "
                              >
                                Copy
                              </button>
                            </dd>
                          </div>
                        </dl>
                      </div>
                    } @else {
                      <div class="disabled-note">
                        <app-icon name="info" />
                        <p>
                          Start verification to rotate the token and create a new DNS record. The
                          previous value can no longer re-enable this domain.
                        </p>
                      </div>
                    }

                    <div class="domain-footer">
                      <p>{{ attemptLabel(domain) }}</p>
                      <div class="domain-actions">
                        @if (domain.status === 'pending' || domain.status === 'failed') {
                          <app-button
                            variant="secondary"
                            [loading]="isBusy(domain.id, 'check')"
                            [disabled]="busyDomainId() !== null"
                            (click)="runOperation('check', domain)"
                          >
                            {{
                              domain.lastVerificationAttemptAtUtc
                                ? 'Check again'
                                : 'Check verification'
                            }}
                          </app-button>
                          <app-button
                            variant="quiet"
                            [loading]="isBusy(domain.id, 'request')"
                            [disabled]="busyDomainId() !== null"
                            (click)="runOperation('request', domain)"
                          >
                            Replace DNS record
                          </app-button>
                        } @else if (domain.status === 'disabled') {
                          <app-button
                            variant="secondary"
                            [loading]="isBusy(domain.id, 'request')"
                            [disabled]="busyDomainId() !== null"
                            (click)="runOperation('request', domain)"
                          >
                            Start verification
                          </app-button>
                        }

                        @if (domain.status !== 'disabled') {
                          <app-button
                            variant="danger"
                            [disabled]="busyDomainId() !== null"
                            (click)="confirmDisable(domain)"
                          >
                            Disable
                          </app-button>
                        }
                      </div>
                    </div>
                  </article>
                }
              </div>
            }
          </section>
        </main>

        <aside class="surface guidance-card" aria-labelledby="dns-guidance-title">
          <p class="eyebrow">DNS setup</p>
          <h2 id="dns-guidance-title">Verification proves ownership</h2>
          <ol>
            <li>Register the exact hostname you want to use for branded links.</li>
            <li>Add the returned TXT name and value with your DNS provider.</li>
            <li>Return here and ask Shortly to check the published record.</li>
          </ol>
          <div class="guidance-note">
            <app-icon name="info" />
            <p>
              DNS propagation can delay when the record becomes visible. If a check fails, keep the
              record published and try again later.
            </p>
          </div>
          <p>
            Verification does not configure traffic or TLS. Your DNS routing and certificate must
            also be ready before branded links are publicly reachable.
          </p>
        </aside>
      </div>

      <app-confirmation-dialog
        #disableDialog
        title="Disable this domain?"
        [message]="disableMessage()"
        confirmLabel="Disable domain"
        (confirmed)="disableConfirmed()"
        (cancelled)="pendingDisable.set(null)"
      />
    </div>
  `,
  styleUrl: './custom-domains-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CustomDomainsPageComponent implements OnInit {
  private readonly api = inject(CustomDomainsApiClient);
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly disableDialog = viewChild.required<ConfirmationDialogComponent>('disableDialog');

  protected readonly domains = signal<readonly CustomDomainResource[]>([]);
  protected readonly loading = signal(true);
  protected readonly registering = signal(false);
  protected readonly pageError = signal<PageError | null>(null);
  protected readonly formError = signal<string | null>(null);
  protected readonly serverHostError = signal<string | null>(null);
  protected readonly hostValue = signal('');
  protected readonly busyDomainId = signal<string | null>(null);
  protected readonly busyOperation = signal<DomainOperation | null>(null);
  protected readonly pendingDisable = signal<CustomDomainResource | null>(null);
  protected readonly form = this.formBuilder.nonNullable.group({
    host: ['', [Validators.required, Validators.maxLength(253), domainHostname]],
  });
  protected readonly normalizedPreview = computed(() => normalizeHostname(this.hostValue()));
  protected readonly disableMessage = computed(() => {
    const domain = this.pendingDisable();
    return domain
      ? `Disabling ${domain.host} immediately stops every assigned branded link from resolving. The claim is retained and must be verified again before it can serve links.`
      : '';
  });

  constructor() {
    this.form.controls.host.valueChanges.pipe(takeUntilDestroyed()).subscribe((value) => {
      this.hostValue.set(value);
      this.serverHostError.set(null);
      this.formError.set(null);
    });
  }

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.pageError.set(null);
    this.api
      .list()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (domains) => this.domains.set(domains),
        error: (error: unknown) => this.pageError.set(this.describePageError(error)),
      });
  }

  protected register(): void {
    this.form.markAllAsTouched();
    this.formError.set(null);
    this.serverHostError.set(null);
    if (this.form.invalid || this.registering()) {
      this.focus('#domain-host');
      return;
    }

    this.registering.set(true);
    this.api
      .register({ host: this.form.controls.host.value.trim() })
      .pipe(finalize(() => this.registering.set(false)))
      .subscribe({
        next: (domain) => {
          this.domains.update((domains) => [domain, ...domains]);
          this.form.reset({ host: '' });
          this.form.markAsPristine();
          this.form.markAsUntouched();
          this.toast.show(
            'Domain registered',
            `Add the TXT record for ${domain.host}, then check verification.`,
          );
          this.focus(`[data-domain-id="${domain.id}"]`);
        },
        error: (error: unknown) => this.handleRegisterError(error),
      });
  }

  protected runOperation(operation: 'check' | 'request', domain: CustomDomainResource): void {
    if (this.busyDomainId()) {
      return;
    }
    this.busyDomainId.set(domain.id);
    this.busyOperation.set(operation);
    const request =
      operation === 'check'
        ? this.api.checkVerification(domain.id)
        : this.api.requestVerification(domain.id);
    request.pipe(finalize(() => this.finishOperation())).subscribe({
      next: (updated) => {
        this.replaceDomain(updated);
        if (operation === 'request') {
          this.toast.show(
            'New DNS record ready',
            `The verification token for ${updated.host} was replaced.`,
          );
        } else if (updated.status === 'verified') {
          this.toast.show(
            'Domain verified',
            `${updated.host} can now be selected for branded links.`,
          );
        } else {
          this.toast.show('Verification not complete', this.statusMessage(updated), 'info');
        }
      },
      error: (error: unknown) => this.handleOperationError(error),
    });
  }

  protected confirmDisable(domain: CustomDomainResource): void {
    this.pendingDisable.set(domain);
    queueMicrotask(() => this.disableDialog().open());
  }

  protected disableConfirmed(): void {
    const domain = this.pendingDisable();
    this.pendingDisable.set(null);
    if (!domain || this.busyDomainId()) {
      return;
    }
    this.busyDomainId.set(domain.id);
    this.busyOperation.set('disable');
    this.api
      .disable(domain.id)
      .pipe(finalize(() => this.finishOperation()))
      .subscribe({
        next: (updated) => {
          this.replaceDomain(updated);
          this.toast.show('Domain disabled', `${updated.host} can no longer serve branded links.`);
        },
        error: (error: unknown) => this.handleOperationError(error),
      });
  }

  protected isBusy(domainId: string, operation: DomainOperation): boolean {
    return this.busyDomainId() === domainId && this.busyOperation() === operation;
  }

  protected hostError(): string | undefined {
    if (this.serverHostError()) {
      return this.serverHostError() ?? undefined;
    }
    const control = this.form.controls.host;
    if (!control.touched) {
      return undefined;
    }
    if (control.hasError('required')) {
      return 'Enter a domain hostname.';
    }
    if (control.hasError('maxlength')) {
      return 'Use a hostname no longer than 253 characters.';
    }
    if (control.hasError('domainHostname')) {
      return 'Enter a multi-label hostname without a scheme, port, path, wildcard, or IP address.';
    }
    return undefined;
  }

  protected statusTone(status: CustomDomainStatus): BadgeTone {
    return status === 'verified'
      ? 'success'
      : status === 'pending'
        ? 'warning'
        : status === 'failed'
          ? 'danger'
          : 'neutral';
  }

  protected statusLabel(status: CustomDomainStatus): string {
    return status === 'failed'
      ? 'Verification failed'
      : status.charAt(0).toUpperCase() + status.slice(1);
  }

  protected statusMessage(domain: CustomDomainResource): string {
    switch (domain.status) {
      case 'verified':
        return 'Backend verification succeeded. This domain is available in link forms.';
      case 'failed':
        return 'The last DNS check did not find the exact current verification value.';
      case 'disabled':
        return 'This retained claim cannot serve links until a new token is verified.';
      default:
        return 'Waiting for a backend DNS check to confirm the exact TXT record.';
    }
  }

  protected failureTitle(code: string): string {
    if (code === 'DNS_TXT_RECORD_NOT_FOUND') {
      return 'TXT record not found';
    }
    if (code === 'DNS_TXT_RECORD_MISMATCH') {
      return 'TXT record does not match';
    }
    if (code === 'DNS_LOOKUP_UNAVAILABLE') {
      return 'DNS lookup unavailable';
    }
    return 'Verification failed';
  }

  protected failureGuidance(code: string, fallback: string): string {
    if (code === 'DNS_TXT_RECORD_NOT_FOUND') {
      return 'Publish the record shown below. DNS propagation can delay when it becomes visible.';
    }
    if (code === 'DNS_TXT_RECORD_MISMATCH') {
      return 'Replace stale or incorrect TXT content with the exact current value shown below.';
    }
    if (code === 'DNS_LOOKUP_UNAVAILABLE') {
      return 'The resolver could not complete the lookup. Keep the record published and try again later.';
    }
    return fallback;
  }

  protected attemptLabel(domain: CustomDomainResource): string {
    if (domain.verifiedAtUtc && domain.status === 'verified') {
      return `Verified ${this.formatDate(domain.verifiedAtUtc)}`;
    }
    if (domain.lastVerificationAttemptAtUtc) {
      return `Last checked ${this.formatDate(domain.lastVerificationAttemptAtUtc)}`;
    }
    if (domain.disabledAtUtc) {
      return `Disabled ${this.formatDate(domain.disabledAtUtc)}`;
    }
    return 'Not checked yet';
  }

  protected formatDate(value: string): string {
    return new Intl.DateTimeFormat(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
  }

  protected async copyText(value: string, title: string): Promise<void> {
    try {
      if (!navigator.clipboard?.writeText) {
        throw new Error('Clipboard API unavailable');
      }
      await navigator.clipboard.writeText(value);
      this.toast.show(title, 'Copied to your clipboard.');
    } catch {
      this.toast.show('Copy failed', 'Select the text and copy it manually.', 'error');
    }
  }

  protected copyRecord(domain: CustomDomainResource): void {
    void this.copyText(
      `Type: ${domain.verificationRecord.type}\nName: ${domain.verificationRecord.name}\nValue: ${domain.verificationRecord.value}`,
      'DNS record copied',
    );
  }

  protected handlePageError(error: PageError): void {
    if (error.signIn) {
      void this.router.navigate(['/auth/sign-in'], {
        queryParams: { returnUrl: this.router.url },
        replaceUrl: true,
      });
      return;
    }
    this.load();
  }

  private handleRegisterError(error: unknown): void {
    if (error instanceof ApiError) {
      const hostMessage = error.validationMessages('host')[0];
      if (hostMessage) {
        this.serverHostError.set(hostMessage);
      }
      this.formError.set(this.registrationMessage(error));
    } else {
      this.formError.set('The domain could not be registered. Try again.');
    }
    this.focus(this.serverHostError() ? '#domain-host' : '.form-alert');
  }

  private registrationMessage(error: ApiError): string {
    if (error.code === 'CUSTOM_DOMAIN_ALREADY_CLAIMED') {
      return 'This normalized hostname is already registered and cannot be claimed again.';
    }
    if (error.code === 'CUSTOM_DOMAIN_RESERVED') {
      return 'This hostname belongs to a protected platform namespace and cannot be registered.';
    }
    return error.message || 'The domain could not be registered.';
  }

  private handleOperationError(error: unknown): void {
    const message =
      error instanceof ApiError ? error.message : 'The domain operation could not be completed.';
    this.toast.show('Request failed', message, 'error');
    if (
      error instanceof ApiError &&
      (error.code === 'CUSTOM_DOMAIN_STATE_CONFLICT' || error.kind === 'not-found')
    ) {
      this.load();
    }
  }

  private describePageError(error: unknown): PageError {
    if (error instanceof ApiError && error.kind === 'authentication') {
      return {
        title: 'Your session has ended',
        message: 'Sign in again to manage custom domains.',
        signIn: true,
      };
    }
    return {
      title: 'Domains could not load',
      message:
        error instanceof ApiError ? error.message : 'An unexpected problem occurred. Try again.',
      signIn: false,
    };
  }

  private replaceDomain(updated: CustomDomainResource): void {
    this.domains.update((domains) =>
      domains.map((domain) => (domain.id === updated.id ? updated : domain)),
    );
  }

  private finishOperation(): void {
    this.busyDomainId.set(null);
    this.busyOperation.set(null);
  }

  private focus(selector: string): void {
    queueMicrotask(() =>
      this.elementRef.nativeElement.querySelector<HTMLElement>(selector)?.focus(),
    );
  }
}

function domainHostname(control: AbstractControl<string>): ValidationErrors | null {
  return control.value && !normalizeHostname(control.value) ? { domainHostname: true } : null;
}

function normalizeHostname(value: string): string | null {
  const candidate = value.trim().replace(/\.$/, '');
  if (!candidate || /[:/\\?#*@\s]/.test(candidate) || candidate.length > 253) {
    return null;
  }
  try {
    const hostname = new URL(`http://${candidate}`).hostname.toLowerCase();
    const labels = hostname.split('.');
    if (
      labels.length < 2 ||
      /^\d+(?:\.\d+){3}$/.test(hostname) ||
      labels.some(
        (label) => !label || label.length > 63 || !/^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/.test(label),
      )
    ) {
      return null;
    }
    return hostname;
  } catch {
    return null;
  }
}
