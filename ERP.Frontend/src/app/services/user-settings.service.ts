import { Injectable, signal, inject, effect } from '@angular/core';
import { take } from 'rxjs';
import { AuthService } from './auth/auth.service';
import { TranslateService } from '@ngx-translate/core';

@Injectable({ providedIn: 'root' })
export class UserSettingsService {
  private readonly translate = inject(TranslateService);
  private readonly authService = inject(AuthService);

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
    // ✅ Load initial language once, then react to changes
    this.translate.use(this._language()).pipe(take(1)).subscribe();

    effect(() => {
      const lang = this._language();
      this.translate.use(lang).pipe(take(1)).subscribe();
    });
  }
  init() {
    if (this._initialized) return;
    this._initialized = true;
    document.documentElement.setAttribute('data-theme', this._theme());
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

  setLanguage(lang: 'en' | 'fr') {
    if (this._language() === lang) return;
    this._language.set(lang);
    if(this.authService.JwtPayload) this.persistToServer();
  }

  get isDark() { return this._theme() === 'dark'; }
  get isEn()   { return this._language() === 'en'; }

  private persistToServer() {
    const userId = this.authService.UserId;
    if (!userId) return;

    this.authService.updateSettings(userId, {
      theme: this._theme(),
      language: this._language()
    }).pipe(take(1)).subscribe({
      error: () => console.error('Failed to persist settings')
    });
  }
}
