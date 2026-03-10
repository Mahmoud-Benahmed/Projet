import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private _dark = signal<boolean>(
    localStorage.getItem('theme') !== 'light'
  );

  readonly isDark = this._dark.asReadonly();

  toggle(): void {
    const next = !this._dark();
    this._dark.set(next);
    localStorage.setItem('theme', next ? 'dark' : 'light');
    document.documentElement.setAttribute('data-theme', next ? 'dark' : 'light');
  }

  init(): void {
    const saved = localStorage.getItem('theme') ?? 'dark';
    document.documentElement.setAttribute('data-theme', saved);
    this._dark.set(saved !== 'light');
  }
}
