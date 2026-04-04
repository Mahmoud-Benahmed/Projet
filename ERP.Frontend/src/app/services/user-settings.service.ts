import { Injectable, signal, inject, effect } from '@angular/core';
import { take } from 'rxjs';
import { AuthService } from './auth/auth.service';
import { TranslateService } from '@ngx-translate/core';

@Injectable({ providedIn: 'root' })
export class UserSettingsService {
  private readonly translate = inject(TranslateService);
  private readonly authService = inject(AuthService);

  // Initialize from JWT — DB is the source of truth
  private _theme = signal<'light' | 'dark'>(
    this.authService.Theme ?? 'light'
  );

  private _language = signal<'en' | 'fr'>(
    this.authService.Language ?? 'en'
  );

  readonly theme = this._theme.asReadonly();
  readonly language = this._language.asReadonly();

  private _initialized = false;

  constructor() {
    effect(() => {
      this.translate.use(this._language());
    });
  }

  init() {
    if (this._initialized) return;
    this._initialized = true;

    document.documentElement.setAttribute('data-theme', this._theme());
    this.translate.use(this._language());
  }

  toggleTheme() {
    const next = this._theme() === 'dark' ? 'light' : 'dark';
    this._theme.set(next);
    document.documentElement.setAttribute('data-theme', next);
    this.persistToServer();
  }

  toggleLanguage() {
    const next = this._language() === 'en' ? 'fr' : 'en';
    this._language.set(next);
    this.persistToServer();
  }

  get isDark() { return this._theme() === 'dark'; }
  get isEn()   { return this._language() === 'en'; }

  setLanguage(lang: 'en' | 'fr') {
    if (this._language() === lang) return;
    this._language.set(lang);
    this.persistToServer();
  }

  private persistToServer() {
    const userId = this.authService.UserId;
    if (!userId) return;

    this.authService.updateSettings(userId, {
      theme: this._theme(),
      language: this._language()
    }).pipe(take(1)).subscribe({
      error: () => {
        console.error('Failed to persist settings');
      }
    });
  }
}
