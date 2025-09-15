import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private _isLoading = signal<boolean>(false);
  private _loadingCount = 0;

  get isLoading() {
    return this._isLoading.asReadonly();
  }

  setLoading(loading: boolean) {
    if (loading) {
      this._loadingCount++;
    } else {
      this._loadingCount = Math.max(0, this._loadingCount - 1);
    }

    this._isLoading.set(this._loadingCount > 0);
  }

  forceLoading(loading: boolean) {
    this._loadingCount = loading ? 1 : 0;
    this._isLoading.set(loading);
  }

  reset() {
    this._loadingCount = 0;
    this._isLoading.set(false);
  }
}
