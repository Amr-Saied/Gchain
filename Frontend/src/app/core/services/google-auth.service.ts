import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

declare global {
  interface Window {
    google?: any;
  }
}

@Injectable({ providedIn: 'root' })
export class GoogleAuthService {
  private scriptLoaded = false;

  loadScript(): Promise<void> {
    if (this.scriptLoaded || window.google?.accounts?.id)
      return Promise.resolve();
    return new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.onload = () => {
        this.scriptLoaded = true;
        resolve();
      };
      script.onerror = () =>
        reject(new Error('Failed to load Google Identity script'));
      document.head.appendChild(script);
    });
  }

  initialize(
    clientId = environment.googleClientId,
    callback: (idToken: string) => void
  ) {
    if (!clientId || clientId.startsWith('<ADD_')) {
      throw new Error('Google Client ID is not set in environment');
    }
    window.google.accounts.id.initialize({
      client_id: clientId,
      callback: (response: any) => {
        const idToken = response.credential as string;
        callback(idToken);
      },
    });
  }

  renderButton(target: HTMLElement) {
    window.google.accounts.id.renderButton(target, {
      theme: 'outline',
      size: 'large',
      shape: 'pill',
      text: 'signin_with',
      logo_alignment: 'left',
    });
  }
}
