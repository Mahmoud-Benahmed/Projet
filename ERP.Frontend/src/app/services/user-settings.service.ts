import { Injectable, signal, inject, effect, untracked } from '@angular/core';
import { Subject, take, takeUntil } from 'rxjs';
import { AuthService } from './auth/auth.service';
import { TranslateService } from '@ngx-translate/core';


@Injectable({ providedIn: 'root' })
export class UserSettingsService {
  private readonly SETTINGS_KEY = 'userSettings';
  private readonly translate    = inject(TranslateService);
  private readonly authService  = inject(AuthService);
  private destroy$              = new Subject<void>();

  private _theme    = signal<'light' | 'dark'>('light');
  private _language = signal<'en' | 'fr'>('en');

  readonly theme    = this._theme.asReadonly();
  readonly language = this._language.asReadonly();

  private _initialized = false;

  constructor() {
    effect(() => {
      const lang = this._language();
      untracked(() => {
        this.translate.use(lang).pipe(take(1)).subscribe();
      });
    });

    this.authService.onLogout$.pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.persistToServer();
      localStorage.removeItem(this.SETTINGS_KEY);  // ← clear on logout
    });
  }

  init() {
    if (this._initialized) return;
    this._initialized = true;

    // Priority: localStorage cache → JWT claims → defaults
    // localStorage is updated on every toggle so it always reflects the latest choice,
    // even though the JWT still carries the old values until the next login.
    const cached = this.loadCachedSettings();
    const theme    = cached?.theme    ?? this.authService.Theme    ?? 'light';
    const language = cached?.language ?? this.authService.Language ?? 'en';

    this._theme.set(theme);
    this._language.set(language);
    document.documentElement.setAttribute('data-theme', theme);
  }

  toggleTheme() {
    const next = this._theme() === 'dark' ? 'light' : 'dark';
    this._theme.set(next);
    document.documentElement.setAttribute('data-theme', next);
    this.cacheSettings();     // ← persist locally first so reload works immediately
    this.persistToServer();
  }

  toggleLanguage() {
    const next = this._language() === 'en' ? 'fr' : 'en';
    this._language.set(next);
    this.cacheSettings();
    this.persistToServer();
  }

  get isDark() { return this._theme() === 'dark'; }
  get isEn()   { return this._language() === 'en'; }

  private cacheSettings(): void {
    localStorage.setItem(this.SETTINGS_KEY, JSON.stringify({
      theme:    this._theme(),
      language: this._language(),
    }));
  }

  private loadCachedSettings(): { theme: 'light' | 'dark'; language: 'en' | 'fr' } | null {
    try {
      const raw = localStorage.getItem(this.SETTINGS_KEY);
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }

  persistToServer() {
    const userId = this.authService.UserId;
    if (!userId) return;

    this.authService.updateSettings(userId, {
      theme:    this._theme(),
      language: this._language(),
    }).pipe(take(1)).subscribe({
      error: () => console.error('Failed to persist settings'),
    });
  }
}
