import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class LoadingService {
  private activeRequests = signal(0);

  isLoading = signal(false);

  show() {
    this.activeRequests.update(count => count + 1);
    this.isLoading.set(this.activeRequests() > 0);
  }

  hide() {
    this.activeRequests.update(count => Math.max(0, count - 1));
    this.isLoading.set(this.activeRequests() > 0);
  }
}
