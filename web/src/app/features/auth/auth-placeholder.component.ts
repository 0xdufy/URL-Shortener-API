import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-auth-placeholder',
  imports: [RouterLink],
  template: `
    <main class="placeholder" aria-labelledby="auth-heading">
      <p class="eyebrow">URL Shortener</p>
      <h1 id="auth-heading">Authentication area</h1>
      <p>Sign-in and registration experiences will be implemented in the authentication UI task.</p>
      <a routerLink="/app">Return to the application shell</a>
    </main>
  `,
  styles: `
    :host {
      display: grid;
      min-height: 100vh;
      place-items: center;
      padding: 1.5rem;
    }

    .placeholder {
      width: min(100%, 36rem);
      padding: 2rem;
      border: 1px solid #dfe3ec;
      border-radius: 1rem;
      background: #fff;
      box-shadow: 0 1rem 3rem rgb(23 32 51 / 8%);
    }

    .eyebrow {
      margin: 0;
      color: #3558c7;
      font-weight: 700;
      letter-spacing: 0.08em;
      text-transform: uppercase;
    }

    h1 {
      margin-block: 0.5rem;
    }

    a {
      color: #294bb8;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthPlaceholderComponent {}
