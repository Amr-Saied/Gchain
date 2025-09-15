import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  message: string;
  type?: 'success' | 'error' | 'info' | 'warning';
  timeoutMs?: number;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private counter = 1;
  toasts = signal<Toast[]>([]);

  show(message: string, type: Toast['type'] = 'info', timeoutMs = 3000) {
    const id = this.counter++;
    const t: Toast = { id, message, type, timeoutMs };
    this.toasts.set([...this.toasts(), t]);
    if (timeoutMs > 0) setTimeout(() => this.dismiss(id), timeoutMs);
  }

  success(msg: string, ms?: number) {
    this.show(msg, 'success', ms ?? 2500);
  }
  error(msg: string, ms?: number) {
    this.show(msg, 'error', ms ?? 4000);
  }
  info(msg: string, ms?: number) {
    this.show(msg, 'info', ms ?? 3000);
  }
  warning(msg: string, ms?: number) {
    this.show(msg, 'warning', ms ?? 3000);
  }

  dismiss(id: number) {
    this.toasts.set(this.toasts().filter((t) => t.id !== id));
  }
}
