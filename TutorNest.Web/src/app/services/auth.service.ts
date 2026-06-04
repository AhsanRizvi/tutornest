import { Injectable, Inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthResponse, User } from '../models';
import { API_BASE_URL } from '../app.config';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl: string;

  readonly currentUser = signal<User | null>(null);
  readonly isAuthenticated = computed(() => this.currentUser() !== null);
  readonly userRole = computed(() => this.currentUser()?.role || null);

  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) baseUrl: string
  ) {
    this.apiUrl = `${baseUrl}/api/auth`;
    this.loadUserFromStorage();
  }

  login(credentials: { email: string; password: string }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(res => {
        localStorage.setItem('tutornest_token', res.token);
        this.setUserFromToken(res.token);
      })
    );
  }

  logout(): void {
    localStorage.removeItem('tutornest_token');
    this.currentUser.set(null);
  }

  registerTeacher(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/register-teacher`, data);
  }

  registerStudent(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/register-student`, data);
  }

  getToken(): string | null {
    return localStorage.getItem('tutornest_token');
  }

  private loadUserFromStorage(): void {
    const token = this.getToken();
    if (token) {
      this.setUserFromToken(token);
    }
  }

  private setUserFromToken(token: string): void {
    const decoded = this.decodeToken(token);
    if (decoded) {
      const expiry = decoded.exp * 1000;
      if (Date.now() > expiry) {
        this.logout();
        return;
      }
      this.currentUser.set({
        id: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
        email: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || decoded.email,
        firstName: decoded.firstName || '',
        lastName: decoded.lastName || '',
        role: decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decoded.role
      });
    } else {
      this.currentUser.set(null);
    }
  }

  private decodeToken(token: string): any {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      return JSON.parse(atob(parts[1]));
    } catch {
      return null;
    }
  }
}
