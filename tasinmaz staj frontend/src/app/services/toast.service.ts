import { Injectable, signal } from '@angular/core';

export interface ToastMessage {
  id: number;
  message: string;
  type: 'success' | 'error' | 'warning' | 'info';
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private nextId = 0;
  toasts = signal<ToastMessage[]>([]);

  show(message: string, type: 'success' | 'error' | 'warning' | 'info' = 'info'): void {
    const id = this.nextId++;
    this.toasts.update(current => [...current, { id, message, type }]);
    setTimeout(() => this.remove(id), 5000);
  }

  success(message: string): void {
    this.show(message, 'success');
  }

  error(message: string): void {
    this.show(message, 'error');
  }

  warning(message: string): void {
    this.show(message, 'warning');
  }

  info(message: string): void {
    this.show(message, 'info');
  }

  remove(id: number): void {
    this.toasts.update(current => current.filter(t => t.id !== id));
  }
}
