import { Injectable, Inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { API_BASE_URL } from '../app.config';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private apiUrl: string;
  readonly currentTheme = signal<string>('default');

  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) baseUrl: string
  ) {
    this.apiUrl = `${baseUrl}/api/theme`;
  }

  loadTheme(): Observable<{ theme: string }> {
    return this.http.get<{ theme: string }>(`${this.apiUrl}/current`).pipe(
      tap(res => {
        this.applyTheme(res.theme);
      })
    );
  }

  applyTheme(themeName: string): void {
    this.currentTheme.set(themeName);
    const body = document.body;
    // Remove existing theme classes
    body.classList.remove('theme-default', 'theme-emerald', 'theme-sunset', 'theme-ocean', 'theme-purple');
    // Add new theme class
    body.classList.add(`theme-${themeName}`);
  }
}
