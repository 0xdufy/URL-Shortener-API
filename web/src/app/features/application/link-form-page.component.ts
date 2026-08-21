import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { ApiError } from '../../core/api/api-error';
import { CustomDomainResource, ShortUrlResource } from '../../core/api/api.models';
import { CustomDomainsApiClient } from '../../core/api/custom-domains-api-client.service';
import { ShortUrlsApiClient } from '../../core/api/short-urls-api-client.service';
import { ButtonComponent } from '../../shared/ui/button/button.component';
import { FieldComponent } from '../../shared/ui/field/field.component';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { PageHeaderComponent } from '../../shared/ui/page-header/page-header.component';
import { StatePanelComponent } from '../../shared/ui/state-panel/state-panel.component';
import { ToastService } from '../../shared/ui/toast/toast.service';

type LinkFormMode = 'create' | 'edit';
type AliasMode = 'generated' | 'custom';
type LinkField = 'originalUrl' | 'customAlias' | 'customDomainId' | 'expiresAtUtc';

const UNAVAILABLE_DOMAIN = '__unavailable__';

interface FormAlert {
  readonly title: string;
  readonly message: string;
}

interface LoadError {
  readonly title: string;
  readonly message: string;
  readonly actionLabel: string;
  readonly action: 'retry' | 'links' | 'sign-in';
}

@Component({
  selector: 'app-link-form-page',
  imports: [
    ButtonComponent,
    FieldComponent,
    IconComponent,
    PageHeaderComponent,
    ReactiveFormsModule,
    RouterLink,
    StatePanelComponent,
  ],
  template: `
    <div class="page-stack">
      <app-page-header
        eyebrow="Link management"
        [title]="mode === 'create' ? 'Create a link' : 'Edit link'"
        [description]="
          mode === 'create'
            ? 'Choose a destination, then let Shortly generate a code or reserve a custom alias.'
            : 'Update the destination or expiry. The short code remains unchanged.'
        "
      >
        <a class="back-link" routerLink="/app/links">
          <span aria-hidden="true">←</span>
          Back to links
        </a>
      </app-page-header>

      @if (loading()) {
        <section class="surface state-card">
          <app-state-panel
            kind="loading"
            title="Loading link"
            message="Retrieving the current destination and expiry."
          />
        </section>
      } @else if (loadError(); as error) {
        <section class="surface state-card">
          <app-state-panel
            kind="error"
            [title]="error.title"
            [message]="error.message"
            [actionLabel]="error.actionLabel"
            (action)="handleLoadErrorAction(error)"
          />
        </section>
      } @else if (createdLink(); as created) {
        <section class="surface success-card" aria-labelledby="created-link-title">
          <span class="success-icon" aria-hidden="true"><app-icon name="check" /></span>
          <p class="eyebrow">Ready to share</p>
          <h2 id="created-link-title">Your short link is live</h2>
          <p class="success-description">
            Copy it now or open it in a new tab to verify the redirect.
          </p>

          <div class="created-url">
            <a [href]="created.shortUrl" target="_blank" rel="noopener noreferrer">
              {{ created.shortUrl }}
            </a>
            <button type="button" (click)="copyShortUrl(created)">Copy short URL</button>
          </div>

          <dl class="created-summary">
            <div>
              <dt>Destination</dt>
              <dd>
                <a
                  [href]="created.originalUrl"
                  target="_blank"
                  rel="noopener noreferrer"
                  [title]="created.originalUrl"
                >
                  {{ created.originalUrl }}
                </a>
              </dd>
            </div>
            <div>
              <dt>Expiry</dt>
              <dd>{{ created.expiresAtUtc ? 'Scheduled' : 'No expiry' }}</dd>
            </div>
          </dl>

          <div class="success-actions">
            <a class="button-link primary" [routerLink]="['/app/links', created.shortCode]">
              View link details
            </a>
            <a
              class="button-link secondary"
              [href]="created.shortUrl"
              target="_blank"
              rel="noopener noreferrer"
            >
              Open short link
            </a>
            <button class="text-button" type="button" (click)="createAnother()">
              Create another
            </button>
          </div>
        </section>
      } @else {
        <div class="content-grid">
          <section class="surface form-card" aria-labelledby="link-form-title">
            <div class="section-heading-row">
              <div>
                <h2 id="link-form-title">
                  {{ mode === 'create' ? 'Link details' : 'Editable details' }}
                </h2>
                <p>
                  {{
                    mode === 'create'
                      ? 'All fields except the destination are optional.'
                      : 'Only destination and expiry can be changed.'
                  }}
                </p>
              </div>
              @if (mode === 'edit' && link(); as currentLink) {
                <span class="short-code-chip">/{{ currentLink.shortCode }}</span>
              }
            </div>

            @if (formAlert(); as alert) {
              <div class="form-alert" role="alert" tabindex="-1">
                <strong>{{ alert.title }}</strong>
                <span>{{ alert.message }}</span>
              </div>
            }

            @if (saveConfirmation()) {
              <div class="save-confirmation" role="status">
                <app-icon name="check" />
                <span>Your changes were saved. Redirects now use the updated details.</span>
              </div>
            }

            <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
              <app-field
                controlId="original-url"
                label="Destination URL"
                hint="Use a complete http:// or https:// address."
                [error]="fieldError('originalUrl')"
              >
                <input
                  id="original-url"
                  class="form-control"
                  type="url"
                  formControlName="originalUrl"
                  inputmode="url"
                  autocomplete="url"
                  maxlength="2048"
                  placeholder="https://example.com/a-useful-page"
                  [attr.aria-describedby]="fieldDescription('originalUrl', 'original-url')"
                  [attr.aria-invalid]="fieldError('originalUrl') ? true : null"
                />
              </app-field>

              @if (mode === 'create') {
                <fieldset class="alias-options">
                  <legend>Short code</legend>
                  <div class="choice-grid">
                    <label [class.selected]="form.controls.aliasMode.value === 'generated'">
                      <input type="radio" formControlName="aliasMode" value="generated" />
                      <span>
                        <strong>Generate for me</strong>
                        <small>Shortly creates a unique code automatically.</small>
                      </span>
                    </label>
                    <label [class.selected]="form.controls.aliasMode.value === 'custom'">
                      <input type="radio" formControlName="aliasMode" value="custom" />
                      <span>
                        <strong>Use a custom alias</strong>
                        <small>Choose a memorable code that is not already claimed.</small>
                      </span>
                    </label>
                  </div>
                </fieldset>

                @if (form.controls.aliasMode.value === 'custom') {
                  <app-field
                    controlId="custom-alias"
                    label="Custom alias"
                    hint="Use 4–20 letters, numbers, hyphens, or underscores."
                    [error]="fieldError('customAlias')"
                  >
                    <div class="alias-input">
                      <span aria-hidden="true">/</span>
                      <input
                        id="custom-alias"
                        class="form-control"
                        type="text"
                        formControlName="customAlias"
                        autocomplete="off"
                        minlength="4"
                        maxlength="20"
                        pattern="[A-Za-z0-9_-]+"
                        placeholder="launch-2026"
                        [attr.aria-describedby]="fieldDescription('customAlias', 'custom-alias')"
                        [attr.aria-invalid]="fieldError('customAlias') ? true : null"
                      />
                    </div>
                  </app-field>
                }
              }

              <app-field
                controlId="custom-domain"
                label="Link domain"
                hint="Use Shortly's platform domain or one of your verified custom domains."
                [error]="fieldError('customDomainId')"
              >
                <select
                  id="custom-domain"
                  class="form-control"
                  formControlName="customDomainId"
                  [attr.aria-describedby]="fieldDescription('customDomainId', 'custom-domain')"
                  [attr.aria-invalid]="fieldError('customDomainId') ? true : null"
                  [attr.aria-busy]="domainsLoading() || null"
                >
                  <option value="">Shortly platform domain</option>
                  @if (form.controls.customDomainId.value === unavailableDomainValue) {
                    <option [value]="unavailableDomainValue" disabled>
                      {{ unavailableDomainHost() ?? 'Selected custom domain' }} — unavailable
                    </option>
                  }
                  @for (domain of verifiedDomains(); track domain.id) {
                    <option [value]="domain.id">{{ domain.host }}</option>
                  }
                </select>
                @if (domainsLoading()) {
                  <p class="domain-selector-note" role="status">Loading verified domains…</p>
                } @else if (domainsLoadError()) {
                  <p class="domain-selector-note error" role="alert">
                    Verified domains could not load.
                    <button type="button" (click)="loadDomains()">Try again</button>
                  </p>
                } @else if (verifiedDomains().length === 0) {
                  <p class="domain-selector-note">
                    No verified custom domains are available.
                    <a routerLink="/app/domains">Manage domains</a>
                  </p>
                } @else {
                  <p class="domain-selector-note">
                    Only backend-verified, enabled domains appear here.
                    <a routerLink="/app/domains">Manage domains</a>
                  </p>
                }
              </app-field>

              <app-field
                controlId="expires-at"
                label="Expiry date and time"
                hint="Entered in your local time and sent to the API as UTC. Leave blank for no expiry."
                [optional]="true"
                [error]="fieldError('expiresAtUtc')"
              >
                <input
                  id="expires-at"
                  class="form-control"
                  type="datetime-local"
                  formControlName="expiresAtLocal"
                  step="60"
                  [min]="minimumExpiry"
                  [attr.aria-describedby]="fieldDescription('expiresAtUtc', 'expires-at')"
                  [attr.aria-invalid]="fieldError('expiresAtUtc') ? true : null"
                />
              </app-field>

              <div class="form-actions">
                <app-button type="submit" [loading]="submitting()" [disabled]="form.disabled">
                  {{ mode === 'create' ? 'Create short link' : 'Save changes' }}
                </app-button>
                <a
                  class="cancel-link"
                  [routerLink]="
                    mode === 'edit' && link() ? ['/app/links', link()!.shortCode] : ['/app/links']
                  "
                >
                  Cancel
                </a>
              </div>
            </form>
          </section>

          <aside class="surface guidance-card" aria-labelledby="guidance-title">
            @if (mode === 'edit' && link(); as currentLink) {
              <p class="eyebrow">Permanent short URL</p>
              <h2 id="guidance-title">The alias stays the same</h2>
              <a
                class="current-short-url"
                [href]="currentLink.shortUrl"
                target="_blank"
                rel="noopener noreferrer"
              >
                {{ currentLink.shortUrl }}
              </a>
              <button type="button" class="aside-action" (click)="copyShortUrl(currentLink)">
                Copy short URL
              </button>
              <p>
                Editing changes where this link redirects and when it expires. It cannot change the
                short code, owner, activity, or usage totals.
              </p>
            } @else {
              <p class="eyebrow">Before you create</p>
              <h2 id="guidance-title">A stable link, your way</h2>
              <ul>
                <li>
                  <app-icon name="sparkles" />
                  <span><strong>Generated codes</strong> are the quickest option.</span>
                </li>
                <li>
                  <app-icon name="links" />
                  <span><strong>Custom aliases</strong> are permanent once created.</span>
                </li>
                <li>
                  <app-icon name="check" />
                  <span
                    ><strong>Optional expiry</strong> stops future redirects automatically.</span
                  >
                </li>
              </ul>
              <p>
                The service validates every value again when you submit, even when this form accepts
                it locally.
              </p>
            }
          </aside>
        </div>
      }
    </div>
  `,
  styleUrl: './link-form-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkFormPageComponent implements OnInit {
  private readonly api = inject(ShortUrlsApiClient);
  private readonly domainsApi = inject(CustomDomainsApiClient);
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);

  protected readonly mode = this.route.snapshot.data['mode'] as LinkFormMode;
  protected readonly minimumExpiry = toLocalDateTimeInput(new Date(Date.now() + 60_000));
  protected readonly loading = signal(this.mode === 'edit');
  protected readonly submitting = signal(false);
  protected readonly link = signal<ShortUrlResource | null>(null);
  protected readonly createdLink = signal<ShortUrlResource | null>(null);
  protected readonly loadError = signal<LoadError | null>(null);
  protected readonly formAlert = signal<FormAlert | null>(null);
  protected readonly saveConfirmation = signal(false);
  protected readonly customDomains = signal<readonly CustomDomainResource[]>([]);
  protected readonly domainsLoading = signal(true);
  protected readonly domainsLoadError = signal(false);
  protected readonly unavailableDomainValue = UNAVAILABLE_DOMAIN;
  protected readonly unavailableDomainHost = signal<string | null>(null);
  protected readonly verifiedDomains = computed(() =>
    this.customDomains().filter(
      (domain) => domain.status === 'verified' && domain.canServeBrandedLinks,
    ),
  );
  protected readonly serverFieldErrors = signal<Readonly<Partial<Record<LinkField, string>>>>({});
  protected readonly form = this.formBuilder.nonNullable.group({
    originalUrl: ['', [Validators.required, Validators.maxLength(2048), absoluteHttpUrl]],
    aliasMode: ['generated' as AliasMode],
    customAlias: [
      '',
      [Validators.minLength(4), Validators.maxLength(20), Validators.pattern(/^[A-Za-z0-9_-]+$/)],
    ],
    customDomainId: ['', [availableDomainSelection]],
    expiresAtLocal: ['', [futureLocalDateTime]],
  });

  constructor() {
    this.form.controls.originalUrl.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.clearServerError('originalUrl'));
    this.form.controls.customAlias.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.clearServerError('customAlias'));
    this.form.controls.customDomainId.valueChanges.pipe(takeUntilDestroyed()).subscribe((value) => {
      if (!value) {
        this.unavailableDomainHost.set(null);
      } else if (value !== UNAVAILABLE_DOMAIN) {
        this.unavailableDomainHost.set(
          this.customDomains().find((domain) => domain.id === value)?.host ??
            (this.link()?.customDomainId === value
              ? (this.link()?.customDomainHost ?? null)
              : null),
        );
      }
      this.clearServerError('customDomainId');
    });
    this.form.controls.expiresAtLocal.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.clearServerError('expiresAtUtc'));
    this.form.controls.aliasMode.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.clearServerError('customAlias');
      this.formAlert.set(null);
      if (this.form.controls.aliasMode.value === 'custom') {
        this.form.controls.customAlias.addValidators(Validators.required);
      } else {
        this.form.controls.customAlias.removeValidators(Validators.required);
      }
      this.form.controls.customAlias.updateValueAndValidity({ emitEvent: false });
    });
  }

  ngOnInit(): void {
    this.loadDomains();
    if (this.mode === 'edit') {
      this.loadLink();
    }
  }

  protected submit(): void {
    this.formAlert.set(null);
    this.saveConfirmation.set(false);
    this.serverFieldErrors.set({});
    this.form.controls.expiresAtLocal.updateValueAndValidity();
    this.form.markAllAsTouched();

    if (this.form.invalid || this.submitting()) {
      this.focusFirstInvalidControl();
      return;
    }

    this.submitting.set(true);
    this.form.disable();
    const value = this.form.getRawValue();
    const expiresAtUtc = localDateTimeToUtc(value.expiresAtLocal);
    const operation =
      this.mode === 'create'
        ? this.api.create({
            originalUrl: value.originalUrl.trim(),
            customAlias: value.aliasMode === 'custom' ? value.customAlias.trim() : null,
            customDomainId: value.customDomainId || null,
            expiresAtUtc,
          })
        : this.api.update(this.route.snapshot.paramMap.get('shortCode') ?? '', {
            originalUrl: value.originalUrl.trim(),
            customDomainId: value.customDomainId || null,
            expiresAtUtc,
          });

    operation.pipe(finalize(() => this.finishSubmission())).subscribe({
      next: (savedLink) => this.handleSuccess(savedLink),
      error: (error: unknown) => this.handleSubmissionError(error),
    });
  }

  protected fieldError(field: LinkField): string | undefined {
    const serverError = this.serverFieldErrors()[field];
    if (serverError) {
      return serverError;
    }

    const control =
      field === 'expiresAtUtc' ? this.form.controls.expiresAtLocal : this.form.controls[field];
    if (!control.touched) {
      return undefined;
    }

    if (control.hasError('required')) {
      return field === 'customAlias' ? 'Enter a custom alias.' : 'Enter the destination URL.';
    }
    if (control.hasError('domainUnavailable')) {
      return 'Choose the platform domain or a currently verified custom domain.';
    }
    if (control.hasError('httpUrl')) {
      return 'Enter a complete URL beginning with http:// or https://.';
    }
    if (control.hasError('maxlength')) {
      return field === 'originalUrl'
        ? 'Destination URL must be 2,048 characters or fewer.'
        : 'Custom alias must be 20 characters or fewer.';
    }
    if (control.hasError('minlength')) {
      return 'Custom alias must be at least 4 characters.';
    }
    if (control.hasError('pattern')) {
      return 'Use only letters, numbers, hyphens, and underscores.';
    }
    if (control.hasError('invalidDateTime')) {
      return 'Enter a valid expiry date and time.';
    }
    if (control.hasError('futureDateTime')) {
      return 'Choose an expiry date and time in the future.';
    }
    return undefined;
  }

  protected fieldDescription(field: LinkField, controlId: string): string {
    return `${controlId}-${this.fieldError(field) ? 'error' : 'hint'}`;
  }

  protected async copyShortUrl(resource: ShortUrlResource): Promise<void> {
    try {
      if (!navigator.clipboard?.writeText) {
        throw new Error('Clipboard API unavailable');
      }
      await navigator.clipboard.writeText(resource.shortUrl);
      this.toastService.show('Short URL copied', `${resource.shortUrl} is ready to paste.`);
    } catch {
      this.toastService.show(
        'Could not copy the short URL',
        'Select and copy the displayed address manually.',
        'error',
      );
    }
  }

  protected createAnother(): void {
    this.createdLink.set(null);
    this.form.reset({
      originalUrl: '',
      aliasMode: 'generated',
      customAlias: '',
      customDomainId: '',
      expiresAtLocal: '',
    });
    this.form.markAsPristine();
    this.form.markAsUntouched();
    this.formAlert.set(null);
    this.serverFieldErrors.set({});
    this.focusElement('#original-url');
  }

  protected handleLoadErrorAction(error: LoadError): void {
    if (error.action === 'retry') {
      this.loadLink();
      return;
    }
    if (error.action === 'sign-in') {
      void this.router.navigate(['/auth/sign-in'], {
        queryParams: { returnUrl: this.router.url },
        replaceUrl: true,
      });
      return;
    }
    void this.router.navigate(['/app/links']);
  }

  private loadLink(): void {
    const shortCode = this.route.snapshot.paramMap.get('shortCode');
    if (!shortCode) {
      this.loading.set(false);
      this.loadError.set({
        title: 'Link not available',
        message: 'This edit address does not identify a link.',
        actionLabel: 'Back to links',
        action: 'links',
      });
      return;
    }

    this.loading.set(true);
    this.loadError.set(null);
    this.api
      .get(shortCode)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (resource) => {
          this.link.set(resource);
          this.form.patchValue({
            originalUrl: resource.originalUrl,
            customDomainId: resource.customDomainId ?? '',
            expiresAtLocal: resource.expiresAtUtc
              ? toLocalDateTimeInput(new Date(resource.expiresAtUtc))
              : '',
          });
          this.form.markAsPristine();
          this.form.markAsUntouched();
          this.reconcileDomainSelection();
        },
        error: (error: unknown) => this.loadError.set(this.describeLoadError(error)),
      });
  }

  private handleSuccess(savedLink: ShortUrlResource): void {
    if (this.mode === 'create') {
      this.createdLink.set(savedLink);
      this.toastService.show('Short link created', `${savedLink.shortUrl} is ready to share.`);
      return;
    }

    this.link.set(savedLink);
    this.form.patchValue({
      originalUrl: savedLink.originalUrl,
      customDomainId: savedLink.customDomainId ?? '',
      expiresAtLocal: savedLink.expiresAtUtc
        ? toLocalDateTimeInput(new Date(savedLink.expiresAtUtc))
        : '',
    });
    this.reconcileDomainSelection();
    this.form.markAsPristine();
    this.form.markAsUntouched();
    this.saveConfirmation.set(true);
    this.toastService.show('Link updated', 'The new destination and expiry are now in effect.');
  }

  private finishSubmission(): void {
    this.submitting.set(false);
    this.form.enable({ emitEvent: false });
  }

  private handleSubmissionError(error: unknown): void {
    if (!(error instanceof ApiError)) {
      this.formAlert.set({
        title: 'Link could not be saved',
        message: 'Something unexpected happened. Check your connection and try again.',
      });
      this.focusAlert();
      return;
    }

    const fieldErrors: Partial<Record<LinkField, string>> = {};
    for (const detail of error.details) {
      const field = normalizeApiField(detail.field);
      if (field && !fieldErrors[field]) {
        fieldErrors[field] = detail.message;
      }
    }

    if (error.code === 'ALIAS_CONFLICT') {
      fieldErrors.customAlias = 'This custom alias is already in use. Choose another one.';
      this.formAlert.set({
        title: 'That alias is already taken',
        message: 'Choose a different custom alias, or let Shortly generate a unique code.',
      });
    } else if (error.code === 'CUSTOM_DOMAIN_UNAVAILABLE') {
      fieldErrors.customDomainId =
        'This domain is no longer verified and enabled. Choose another domain.';
      this.formAlert.set({
        title: 'Custom domain unavailable',
        message:
          'Domain status changed before the link was saved. Refresh the available domains and choose again.',
      });
      this.loadDomains();
    } else if (error.kind === 'rate-limited' || error.code === 'RATE_LIMITED') {
      this.formAlert.set({
        title: 'Creation limit reached',
        message:
          error.retryAfterSeconds === undefined
            ? 'Wait a moment before trying to create another link.'
            : `Try again in ${error.retryAfterSeconds} seconds. Your form values are still here.`,
      });
    } else if (error.kind === 'validation' || Object.keys(fieldErrors).length > 0) {
      this.formAlert.set({
        title: 'Check the highlighted fields',
        message:
          Object.keys(fieldErrors).length > 0
            ? 'The service rejected one or more values. Review its feedback below.'
            : 'The service rejected these values. Review the form and try again.',
      });
    } else if (error.kind === 'not-found' && this.mode === 'edit') {
      this.formAlert.set({
        title: 'This link is no longer editable',
        message: 'It may have been deleted or is no longer available to this account.',
      });
    } else if (error.kind === 'connectivity') {
      this.formAlert.set({
        title: 'Could not reach the service',
        message: 'Check your connection. Your form values are still here, so you can try again.',
      });
    } else if (error.kind === 'service') {
      this.formAlert.set({
        title: 'The service could not save this link',
        message: 'Your form values are still here. Wait a moment and try again.',
      });
    } else {
      this.formAlert.set({
        title: 'Link could not be saved',
        message: error.message || 'The request could not be completed. Try again.',
      });
    }

    this.serverFieldErrors.set(fieldErrors);
    if (Object.keys(fieldErrors).length > 0) {
      this.focusFirstServerError(fieldErrors);
    } else {
      this.focusAlert();
    }
  }

  protected loadDomains(): void {
    this.domainsLoading.set(true);
    this.domainsLoadError.set(false);
    this.domainsApi
      .list()
      .pipe(
        finalize(() => {
          this.domainsLoading.set(false);
          this.reconcileDomainSelection();
        }),
      )
      .subscribe({
        next: (domains) => {
          this.customDomains.set(domains);
          this.reconcileDomainSelection();
        },
        error: () => {
          this.domainsLoadError.set(true);
          this.reconcileDomainSelection();
        },
      });
  }

  private reconcileDomainSelection(): void {
    const selectedId = this.form.controls.customDomainId.value;
    if (!selectedId || selectedId === UNAVAILABLE_DOMAIN || this.domainsLoading()) {
      return;
    }
    const available = this.verifiedDomains().some((domain) => domain.id === selectedId);
    if (!available) {
      this.form.controls.customDomainId.setValue(UNAVAILABLE_DOMAIN);
      this.form.controls.customDomainId.markAsTouched();
    }
  }

  private describeLoadError(error: unknown): LoadError {
    if (!(error instanceof ApiError)) {
      return {
        title: 'Link could not be loaded',
        message: 'Something unexpected happened while loading this link.',
        actionLabel: 'Try again',
        action: 'retry',
      };
    }
    if (error.kind === 'not-found' || error.kind === 'authorization') {
      return {
        title: 'Link not available',
        message: 'This link may have been deleted or is not available to this account.',
        actionLabel: 'Back to links',
        action: 'links',
      };
    }
    if (error.kind === 'authentication') {
      return {
        title: 'Sign in to edit this link',
        message: 'Your session is no longer available.',
        actionLabel: 'Sign in',
        action: 'sign-in',
      };
    }
    if (error.kind === 'connectivity') {
      return {
        title: 'Could not reach the service',
        message: 'Check your connection, then try loading the link again.',
        actionLabel: 'Try again',
        action: 'retry',
      };
    }
    return {
      title: 'Link could not be loaded',
      message: 'The service could not return this link. Wait a moment and try again.',
      actionLabel: 'Try again',
      action: 'retry',
    };
  }

  private clearServerError(field: LinkField): void {
    if (!this.serverFieldErrors()[field]) {
      return;
    }
    const next = { ...this.serverFieldErrors() };
    delete next[field];
    this.serverFieldErrors.set(next);
    this.formAlert.set(null);
  }

  private focusFirstInvalidControl(): void {
    queueMicrotask(() =>
      this.elementRef.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
    );
  }

  private focusFirstServerError(errors: Partial<Record<LinkField, string>>): void {
    const selector = errors.originalUrl
      ? '#original-url'
      : errors.customAlias
        ? '#custom-alias'
        : errors.customDomainId
          ? '#custom-domain'
          : '#expires-at';
    this.focusElement(selector);
  }

  private focusAlert(): void {
    this.focusElement('.form-alert');
  }

  private focusElement(selector: string): void {
    queueMicrotask(() =>
      this.elementRef.nativeElement.querySelector<HTMLElement>(selector)?.focus(),
    );
  }
}

function absoluteHttpUrl(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value.trim();
  if (!value) {
    return null;
  }
  try {
    const url = new URL(value);
    return (url.protocol === 'http:' || url.protocol === 'https:') && url.hostname
      ? null
      : { httpUrl: true };
  } catch {
    return { httpUrl: true };
  }
}

function futureLocalDateTime(control: AbstractControl<string>): ValidationErrors | null {
  if (!control.value) {
    return null;
  }
  const timestamp = new Date(control.value).getTime();
  if (Number.isNaN(timestamp)) {
    return { invalidDateTime: true };
  }
  return timestamp > Date.now() ? null : { futureDateTime: true };
}

function availableDomainSelection(control: AbstractControl<string>): ValidationErrors | null {
  return control.value === UNAVAILABLE_DOMAIN ? { domainUnavailable: true } : null;
}

function localDateTimeToUtc(value: string): string | null {
  return value ? new Date(value).toISOString() : null;
}

function toLocalDateTimeInput(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(
    date.getHours(),
  )}:${pad(date.getMinutes())}`;
}

function normalizeApiField(field: string): LinkField | undefined {
  const normalized = field.replaceAll('$', '').replaceAll('.', '').toLowerCase();
  if (normalized.endsWith('originalurl')) {
    return 'originalUrl';
  }
  if (normalized.endsWith('customalias')) {
    return 'customAlias';
  }
  if (normalized.endsWith('customdomainid')) {
    return 'customDomainId';
  }
  if (normalized.endsWith('expiresatutc')) {
    return 'expiresAtUtc';
  }
  return undefined;
}
