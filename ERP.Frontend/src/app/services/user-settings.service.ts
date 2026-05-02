import { Injectable, signal, inject, effect, untracked } from '@angular/core';
import { Subject, take, takeUntil } from 'rxjs';
import { AuthService } from './auth/auth.service';
import { TranslateService } from '@ngx-translate/core';
import { LanguageType, ThemeType } from '../interfaces/AuthDto';
import { toSignal } from '@angular/core/rxjs-interop';


@Injectable({ providedIn: 'root' })
export class UserSettingsService {
  private readonly SETTINGS_KEY = 'userSettings';
  private readonly translate    = inject(TranslateService);
  private readonly authService  = inject(AuthService);
  private destroy$              = new Subject<void>();

  private _theme    = signal<ThemeType>('light');
  private _language = signal<LanguageType>('en');

  readonly theme    = this._theme.asReadonly();
  readonly language = this._language.asReadonly();

 private readonly userProfile = toSignal(this.authService.userProfile$, { initialValue: null });

  constructor() {
    const cached = this.loadCachedSettings();

    const theme = cached?.theme ?? 'light';
    const language = cached?.language ?? 'en';

    if (cached) {
      this._theme.set(cached.theme);
      this._language.set(cached.language);
      this.translate.use(cached.language); // ← add this
      document.documentElement.setAttribute('data-theme', cached.theme); // ← and this
    }

    effect(() => {
      const settings = this.userProfile()?.settings;
      if (!settings) return;

      untracked(() => {
        this._theme.set(settings.theme);
        this._language.set(settings.language);
        document.documentElement.setAttribute('data-theme', settings.theme);
        this.translate.use(settings.language);
        this.cacheSettings(); // sync localStorage with server truth
      });
    });
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
    this.translate.use(next);
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
