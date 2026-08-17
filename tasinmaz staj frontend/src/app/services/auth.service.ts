import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Router } from '@angular/router';

import { environment } from '../../environments/environment';
import { LoginRequest } from '../models/login-request.model';
import { LoginResponse } from '../models/login-response.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/Auth`;
  
  private authStatus = signal<boolean>(this.checkAuthStatus());
  private roleStatus = signal<string | null>(this.getRoleFromStorage());
  private emailStatus = signal<string | null>(this.getEmailFromStorage());

  constructor(private http: HttpClient, private router: Router) {}

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, request).pipe(
      tap((response) => {
        localStorage.setItem('token', response.token);
        localStorage.setItem('role', response.role);
        localStorage.setItem('email', response.email);
        
        this.authStatus.set(true);
        this.roleStatus.set(response.role);
        this.emailStatus.set(response.email);
      })
    );
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    localStorage.removeItem('email');
    
    this.authStatus.set(false);
    this.roleStatus.set(null);
    this.emailStatus.set(null);
    
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  getRoleFromStorage(): string | null {
    return localStorage.getItem('role');
  }

  getEmailFromStorage(): string | null {
    return localStorage.getItem('email');
  }

  getRole(): string | null {
    return this.roleStatus();
  }

  getEmail(): string | null {
    return this.emailStatus();
  }

  private checkAuthStatus(): boolean {
    const token = this.getToken();
    if (!token) return false;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const expiry = payload.exp;
      // exp is in seconds, Date.now() is in milliseconds
      return (Math.floor(Date.now() / 1000)) < expiry;
    } catch {
      return false;
    }
  }

  isTokenExpired(): boolean {
    return !this.checkAuthStatus();
  }

  isAuthenticated(): boolean {
    return this.authStatus();
  }

  forgotPassword(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/forgot-password`, { email });
  }

  resetPassword(email: string, token: string, newPassword: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/reset-password`, {
      email,
      token,
      newPassword
    });
  }

  isAdmin(): boolean {
    return this.roleStatus() === 'Admin';
  }
}