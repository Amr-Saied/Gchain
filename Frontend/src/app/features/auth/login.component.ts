import { Component, ElementRef, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { GoogleAuthService } from '../../core/services/google-auth.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="login-container">
      <div class="login-card">
        <div class="login-header">
          <div class="logo">
            <span class="logo-icon">G</span>
          </div>
          <h1 class="login-title">Welcome to Gchain</h1>
          <p class="login-subtitle">
            Choose your preferred login method to start playing
          </p>
        </div>

        <div class="login-actions">
          <button
            class="btn btn-guest"
            (click)="loginGuest()"
            [disabled]="isLoading"
          >
            <span class="btn-icon">👤</span>
            <span class="btn-text">Continue as Guest</span>
            <span class="btn-loading" *ngIf="isLoading">⏳</span>
          </button>

          <div class="divider">
            <span class="divider-text">or</span>
          </div>

          <div class="google-login">
            <span #googleBtn></span>
          </div>
        </div>

        <div class="login-footer">
          <p class="footer-text">
            By continuing, you agree to our Terms of Service and Privacy Policy
          </p>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .login-container {
        min-height: 100vh;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 2rem 1rem;
        background: linear-gradient(135deg, #0f1115 0%, #171a21 100%);
      }

      .login-card {
        background-color: #171a21;
        border: 1px solid #2d3748;
        border-radius: 16px;
        padding: 3rem 2rem;
        width: 100%;
        max-width: 400px;
        box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1),
          0 10px 10px -5px rgba(0, 0, 0, 0.04);
      }

      .login-header {
        text-align: center;
        margin-bottom: 2rem;
      }

      .logo {
        width: 64px;
        height: 64px;
        background: linear-gradient(135deg, #f2c037, #e0b22f);
        border-radius: 16px;
        display: flex;
        align-items: center;
        justify-content: center;
        margin: 0 auto 1.5rem;
      }

      .logo-icon {
        color: #0f1115;
        font-weight: 700;
        font-size: 2rem;
      }

      .login-title {
        font-size: 1.875rem;
        font-weight: 700;
        color: #e6e6e6;
        margin-bottom: 0.5rem;
      }

      .login-subtitle {
        color: #a0aec0;
        font-size: 1rem;
        line-height: 1.5;
      }

      .login-actions {
        margin-bottom: 2rem;
      }

      .btn {
        width: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.75rem;
        padding: 0.875rem 1.5rem;
        border-radius: 12px;
        font-weight: 600;
        font-size: 1rem;
        transition: all 0.2s ease;
        border: none;
        cursor: pointer;
        position: relative;
      }

      .btn:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }

      .btn-guest {
        background-color: #2d3748;
        color: #e6e6e6;
        border: 1px solid #4a5568;
      }

      .btn-guest:hover:not(:disabled) {
        background-color: #4a5568;
        transform: translateY(-1px);
      }

      .btn-icon {
        font-size: 1.25rem;
      }

      .btn-text {
        flex: 1;
      }

      .btn-loading {
        font-size: 1rem;
        animation: spin 1s linear infinite;
      }

      .divider {
        position: relative;
        margin: 1.5rem 0;
        text-align: center;
      }

      .divider::before {
        content: '';
        position: absolute;
        top: 50%;
        left: 0;
        right: 0;
        height: 1px;
        background-color: #2d3748;
      }

      .divider-text {
        background-color: #171a21;
        color: #a0aec0;
        padding: 0 1rem;
        font-size: 0.875rem;
        position: relative;
      }

      .google-login {
        display: flex;
        justify-content: center;
      }

      .login-footer {
        text-align: center;
      }

      .footer-text {
        color: #718096;
        font-size: 0.75rem;
        line-height: 1.4;
      }

      @keyframes spin {
        0% {
          transform: rotate(0deg);
        }
        100% {
          transform: rotate(360deg);
        }
      }

      @media (max-width: 480px) {
        .login-card {
          padding: 2rem 1.5rem;
        }

        .login-title {
          font-size: 1.5rem;
        }
      }
    `,
  ],
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly google = inject(GoogleAuthService);
  private readonly toast = inject(ToastService);
  @ViewChild('googleBtn', { static: true })
  googleBtnRef!: ElementRef<HTMLSpanElement>;

  isLoading = false;

  loginGuest() {
    this.isLoading = true;
    this.auth.guestLogin().subscribe({
      next: () => {
        this.toast.success('Signed in as guest');
        this.router.navigateByUrl('/');
        this.isLoading = false;
      },
      error: () => {
        this.toast.error('Login failed');
        this.isLoading = false;
      },
    });
  }

  ngOnInit() {
    this.google
      .loadScript()
      .then(() => {
        this.google.initialize(undefined, (idToken) => {
          this.auth
            .googleLogin({ token: idToken, tokenType: 'id_token' })
            .subscribe({
              next: () => {
                this.toast.success('Signed in with Google');
                this.router.navigateByUrl('/');
              },
              error: () => this.toast.error('Google login failed'),
            });
        });
        this.google.renderButton(this.googleBtnRef.nativeElement);
      })
      .catch(() => {
        // script failed; guest login remains available
      });
  }
}
