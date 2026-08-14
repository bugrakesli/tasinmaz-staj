import { Injectable, signal, effect } from '@angular/core';

export type Theme = 'light' | 'dark';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly THEME_KEY = 'tys-theme';
  
  // Varsayılan temayı localStorage'dan veya sistem tercihinden al
  public currentTheme = signal<Theme>(this.getInitialTheme());

  constructor() {
    // Tema değiştiğinde HTML'e data-theme özelliğini ekle ve localStorage'a kaydet
    effect(() => {
      const theme = this.currentTheme();
      if (theme === 'dark') {
        document.documentElement.setAttribute('data-theme', 'dark');
      } else {
        document.documentElement.removeAttribute('data-theme');
      }
      localStorage.setItem(this.THEME_KEY, theme);
    });
  }

  public toggleTheme() {
    this.currentTheme.update(theme => theme === 'light' ? 'dark' : 'light');
  }

  private getInitialTheme(): Theme {
    const savedTheme = localStorage.getItem(this.THEME_KEY) as Theme;
    if (savedTheme) {
      return savedTheme;
    }
    
    // İşletim sistemi tercihini kontrol et
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
      return 'dark';
    }
    
    return 'light';
  }
}
