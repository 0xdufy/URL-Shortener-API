import { Injectable, signal } from '@angular/core';

export type ToastTone = 'success' | 'error' | 'info';

export interface ToastMessage {
  readonly id: number;
  readonly title: string;
  readonly message: string;
  readonly tone: ToastTone;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly messages = signal<readonly ToastMessage[]>([]);
  private nextId = 0;

  show(title: string, message: string, tone: ToastTone = 'success'): void {
    const id = ++this.nextId;
    this.messages.update((messages) => [...messages, { id, title, message, tone }]);
    setTimeout(() => this.dismiss(id), 5000);
  }

  dismiss(id: number): void {
    this.messages.update((messages) => messages.filter((message) => message.id !== id));
  }
}
