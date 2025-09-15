import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toasts">
      <div
        class="toast"
        *ngFor="let t of toasts()"
        [class.err]="t.type === 'error'"
        [class.ok]="t.type === 'success'"
        [class.warn]="t.type === 'warning'"
      >
        <span>{{ t.message }}</span>
        <button (click)="dismiss(t.id)">×</button>
      </div>
    </div>
  `,
  styles: [
    `
      .toasts {
        position: fixed;
        right: 16px;
        bottom: 16px;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        z-index: 9999;
      }
    `,
    `
      .toast {
        background: var(--color-surface);
        border: 1px solid #303644;
        color: var(--color-text);
        padding: 0.6rem 1rem;
        border-radius: 10px;
        display: flex;
        gap: 0.75rem;
        align-items: center;
        box-shadow: 0 6px 20px rgba(0, 0, 0, 0.35);
      }
    `,
    `
      .toast.ok {
        border-color: #1ec8a5;
      }
    `,
    `
      .toast.err {
        border-color: #ef476f;
      }
    `,
    `
      .toast.warn {
        border-color: #f2c037;
      }
    `,
    `
      .toast button {
        background: transparent;
        border: none;
        color: var(--color-muted);
        cursor: pointer;
        font-size: 18px;
      }
    `,
  ],
})
export class ToastContainerComponent {
  private readonly toast = inject(ToastService);
  toasts = computed(() => this.toast.toasts());
  dismiss(id: number) {
    this.toast.dismiss(id);
  }
}
