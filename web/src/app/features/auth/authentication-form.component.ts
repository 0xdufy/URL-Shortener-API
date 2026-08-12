import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { ApiError } from '../../core/api/api-error';
import { AuthenticationApiClient } from '../../core/api/authentication-api-client.service';
import { BrowserAuthenticationBootstrap } from '../../core/api/api.models';
import { AuthenticationStateService } from '../../core/auth/authentication-state.service';
import { safeReturnUrl } from '../../core/auth/safe-return-url';
import { ButtonComponent } from '../../shared/ui/button/button.component';
import { FieldComponent } from '../../shared/ui/field/field.component';
import { ToastService } from '../../shared/ui/toast/toast.service';

type AuthenticationMode = 'sign-in' | 'register';

@Component({
  selector: 'app-authentication-form',
  imports: [ButtonComponent, FieldComponent, ReactiveFormsModule, RouterLink],
  template: `
    <div class="form-card">
      @if (configurationLoading()) {
        <div class="form-heading unavailable" role="status">
          <p class="eyebrow">Preparing registration</p>
          <h2>Loading account options…</h2>
          <p>Please wait while Shortly checks the current registration policy.</p>
        </div>
      } @else if (configurationError()) {
        <div class="form-heading unavailable" role="alert">
          <p class="eyebrow">Service unavailable</p>
          <h2>Registration options could not be loaded</h2>
          <p>Try again later, or sign in with an existing account.</p>
          <a
            class="primary-link"
            routerLink="/auth/sign-in"
            [queryParams]="{ returnUrl: returnUrl }"
          >
            Return to sign in
          </a>
        </div>
      } @else if (registrationAvailable()) {
        <div class="form-heading">
          <p class="eyebrow">{{ mode === 'register' ? 'Get started' : 'Welcome back' }}</p>
          <h2>{{ mode === 'register' ? 'Create your account' : 'Sign in to Shortly' }}</h2>
          <p>
            {{
              mode === 'register'
                ? 'Start creating and managing better short links.'
                : 'Enter your details to continue to your workspace.'
            }}
          </p>
        </div>

        @if (formError()) {
          <div class="form-error" role="alert" tabindex="-1">
            <strong>{{ errorTitle() }}</strong>
            <span>{{ formError() }}</span>
          </div>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <app-field controlId="email" label="Email address" [error]="fieldError('email')">
            <input
              id="email"
              class="form-control"
              type="email"
              formControlName="email"
              autocomplete="email"
              inputmode="email"
              placeholder="you@example.com"
              [attr.aria-describedby]="fieldError('email') ? 'email-error' : null"
              [attr.aria-invalid]="fieldError('email') ? true : null"
            />
          </app-field>

          <app-field
            controlId="password"
            label="Password"
            [hint]="passwordHint()"
            [error]="fieldError('password')"
          >
            <input
              id="password"
              class="form-control"
              type="password"
              formControlName="password"
              [autocomplete]="mode === 'register' ? 'new-password' : 'current-password'"
              [attr.aria-describedby]="
                fieldError('password') ? 'password-error' : passwordHint() ? 'password-hint' : null
              "
              [attr.aria-invalid]="fieldError('password') ? true : null"
            />
          </app-field>

          <app-button type="submit" [loading]="submitting()" [disabled]="form.disabled">
            {{ mode === 'register' ? 'Create account' : 'Sign in' }}
          </app-button>
        </form>

        @if (mode === 'register' || publicRegistrationEnabled()) {
          <p class="alternate-action">
            {{ mode === 'register' ? 'Already have an account?' : 'New to Shortly?' }}
            <a
              [routerLink]="mode === 'register' ? '/auth/sign-in' : '/auth/register'"
              [queryParams]="{ returnUrl: returnUrl }"
            >
              {{ mode === 'register' ? 'Sign in' : 'Create an account' }}
            </a>
          </p>
        }
      } @else {
        <div class="form-heading unavailable" role="status">
          <p class="eyebrow">Registration unavailable</p>
          <h2>Account creation is currently closed</h2>
          <p>Ask your workspace administrator for access, or sign in with an existing account.</p>
          <a
            class="primary-link"
            routerLink="/auth/sign-in"
            [queryParams]="{ returnUrl: returnUrl }"
          >
            Return to sign in
          </a>
        </div>
      }
    </div>
  `,
  styleUrl: './authentication-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthenticationFormComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authenticationApi = inject(AuthenticationApiClient);
  private readonly authenticationState = inject(AuthenticationStateService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);

  protected readonly mode = this.route.snapshot.data['mode'] as AuthenticationMode;
  protected readonly returnUrl = safeReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl'));
  protected readonly submitting = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly serverFieldErrors = signal<Readonly<Record<string, string>>>({});
  protected readonly registrationAvailable = signal(this.mode === 'sign-in');
  protected readonly publicRegistrationEnabled = signal(false);
  protected readonly configurationLoading = signal(this.mode === 'register');
  protected readonly configurationError = signal(false);
  protected readonly passwordPolicy = signal<BrowserAuthenticationBootstrap | null>(null);
  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', [Validators.required, Validators.maxLength(128)]],
  });

  ngOnInit(): void {
    if (this.authenticationState.isAuthenticated()) {
      void this.router.navigateByUrl(this.returnUrl);
      return;
    }

    this.authenticationApi.bootstrap().subscribe({
      next: (configuration) => {
        this.configurationLoading.set(false);
        this.passwordPolicy.set(configuration);
        this.publicRegistrationEnabled.set(configuration.publicRegistrationEnabled);
        this.registrationAvailable.set(
          this.mode === 'sign-in' || configuration.publicRegistrationEnabled,
        );
        if (this.mode === 'register') {
          this.form.controls.password.addValidators(
            Validators.minLength(configuration.passwordRequiredLength),
          );
          this.form.controls.password.updateValueAndValidity();
        }
      },
      error: () => {
        if (this.mode === 'register') {
          this.configurationLoading.set(false);
          this.configurationError.set(true);
        }
      },
    });
  }

  protected submit(): void {
    this.formError.set(null);
    this.serverFieldErrors.set({});
    this.form.markAllAsTouched();

    if (this.form.invalid || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.form.disable();
    const request = this.form.getRawValue();
    const operation =
      this.mode === 'register'
        ? this.authenticationApi.register(request)
        : this.authenticationApi.signIn(request);

    operation.pipe(finalize(() => this.finishSubmission())).subscribe({
      next: () => {
        if (this.mode === 'register') {
          this.toastService.show('Account created', 'Your secure session is ready.');
        }
        void this.router.navigateByUrl(this.returnUrl);
      },
      error: (error: unknown) => this.handleError(error),
    });
  }

  protected fieldError(field: 'email' | 'password'): string | undefined {
    const serverError = this.serverFieldErrors()[field];
    if (serverError) {
      return serverError;
    }

    const control = this.form.controls[field];
    if (!control.touched) {
      return undefined;
    }

    if (control.hasError('required')) {
      return field === 'email' ? 'Enter your email address.' : 'Enter your password.';
    }
    if (control.hasError('email')) {
      return 'Enter a valid email address.';
    }
    if (control.hasError('minlength')) {
      return `Use at least ${this.passwordPolicy()?.passwordRequiredLength ?? 12} characters.`;
    }
    if (control.hasError('maxlength')) {
      return field === 'email'
        ? 'Email must be 256 characters or fewer.'
        : 'Password must be 128 characters or fewer.';
    }
    return undefined;
  }

  protected passwordHint(): string | undefined {
    if (this.mode !== 'register') {
      return undefined;
    }

    const policy = this.passwordPolicy();
    return policy
      ? `Use ${policy.passwordRequiredLength}–128 characters with uppercase, lowercase, a number, a symbol, and at least ${policy.passwordRequiredUniqueChars} unique characters.`
      : 'Use 12–128 characters with uppercase, lowercase, a number, and a symbol.';
  }

  protected errorTitle(): string {
    return this.mode === 'sign-in' ? 'Unable to sign in' : 'Unable to create account';
  }

  private finishSubmission(): void {
    this.submitting.set(false);
    this.form.enable();
  }

  private handleError(error: unknown): void {
    if (!(error instanceof ApiError)) {
      this.formError.set('Something went wrong. Try again.');
      return;
    }

    this.serverFieldErrors.set(
      Object.fromEntries(
        error.details.map((detail) => [detail.field.toLowerCase(), detail.message]),
      ),
    );

    const hasVisibleFieldError = error.details.some((detail) =>
      ['email', 'password'].includes(detail.field.toLowerCase()),
    );

    if (error.code === 'AUTHENTICATION_FAILED') {
      this.formError.set('Invalid credentials.');
    } else if (error.code === 'ACCOUNT_UNAVAILABLE') {
      this.formError.set('An account cannot be created with those details.');
    } else if (error.code === 'RATE_LIMITED') {
      const retry = error.retryAfterSeconds;
      this.formError.set(
        retry === undefined
          ? 'Too many attempts. Wait a moment and try again.'
          : `Too many attempts. Try again in ${retry} seconds.`,
      );
    } else if (!hasVisibleFieldError) {
      this.formError.set(error.message);
    }
  }
}
