import { Injectable, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

function detectTheme(): Theme {
  if (typeof localStorage !== 'undefined') {
    const saved = localStorage.getItem('utilix.theme') as Theme | null;
    if (saved === 'light' || saved === 'dark') return saved;
  }
  if (typeof window !== 'undefined' && window.matchMedia) {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
  return 'light';
}

function apply(theme: Theme): void {
  if (typeof document !== 'undefined') {
    document.documentElement.dataset['theme'] = theme;
  }
}

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly _theme = signal<Theme>(detectTheme());
  readonly theme = this._theme.asReadonly();

  constructor() {
    apply(this._theme());
  }

  toggle(): void {
    const next: Theme = this._theme() === 'light' ? 'dark' : 'light';
    this._theme.set(next);
    apply(next);
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('utilix.theme', next);
    }
  }
}
