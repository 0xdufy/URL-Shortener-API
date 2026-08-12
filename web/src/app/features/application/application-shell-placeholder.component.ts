import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-application-shell-placeholder',
  imports: [RouterLink],
  template: `
    <header class="top-bar">
      <a class="brand" routerLink="/app">URL Shortener</a>
      <a routerLink="/auth">Authentication</a>
    </header>

    <main class="content" aria-labelledby="shell-heading">
      <p class="eyebrow">Application foundation</p>
      <h1 id="shell-heading">Your short-link workspace starts here.</h1>
      <p>
        The Angular workspace, route boundaries, and environment-aware API configuration are ready.
        The reusable product shell and design system arrive in the next task.
      </p>
    </main>
  `,
  styles: `
    :host {
      display: block;
      min-height: 100vh;
    }

    .top-bar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      min-height: 4rem;
      padding-inline: clamp(1rem, 4vw, 3rem);
      border-bottom: 1px solid #dfe3ec;
      background: #fff;
    }

    .top-bar a {
      color: #294bb8;
    }

    .brand {
      color: #172033 !important;
      font-weight: 750;
      text-decoration: none;
    }

    .content {
      width: min(100% - 2rem, 64rem);
      margin-inline: auto;
      padding-block: clamp(4rem, 12vw, 9rem);
    }

    .eyebrow {
      margin: 0;
      color: #3558c7;
      font-weight: 700;
      letter-spacing: 0.08em;
      text-transform: uppercase;
    }

    h1 {
      max-width: 18ch;
      margin-block: 0.75rem 1rem;
      font-size: clamp(2.25rem, 7vw, 4.75rem);
      line-height: 1.02;
    }

    .content > p:last-child {
      max-width: 58ch;
      color: #556078;
      font-size: 1.125rem;
      line-height: 1.7;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApplicationShellPlaceholderComponent {}
