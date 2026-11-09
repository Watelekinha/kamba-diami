import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import {
  AuthResponse,
  ForgotPasswordRequest,
  LoginRequest,
  MessageResponse,
  RegisterRequest,
  ResetPasswordRequest,
  TokenResponse
} from '../models/auth.model';
import { UserProfile } from '../models/user.model';

const TOKEN_KEY = 'kd_token';
const USER_KEY = 'kd_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly currentUserSignal = signal<UserProfile | null>(null);
  private readonly tokenSignal = signal<string | null>(null);

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.isTokenUsable(this.tokenSignal()));

  constructor(private http: HttpClient) {
    const token = localStorage.getItem(TOKEN_KEY);
    if (this.isTokenUsable(token)) {
      this.tokenSignal.set(token);
      this.currentUserSignal.set(this.readStoredUser());
    } else {
      this.clearStoredSession();
    }
  }

  get token(): string | null {
    return this.tokenSignal();
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>('/api/auth/login', request)
      .pipe(tap((response) => this.persistSession(response)));
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>('/api/auth/register', request)
      .pipe(tap((response) => this.persistSession(response)));
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<MessageResponse> {
    return this.http.post<MessageResponse>('/api/auth/forgot-password', request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<MessageResponse> {
    return this.http.post<MessageResponse>('/api/auth/reset-password', request);
  }

  /** Depois de mudar a palavra-passe o token antigo deixa de valer; o servidor devolve um novo. */
  replaceToken(response: TokenResponse): void {
    localStorage.setItem(TOKEN_KEY, response.token);
    this.tokenSignal.set(response.token);
  }

  /** Página inicial de cada papel depois de entrar. */
  homeRoute(): string {
    return this.currentUserSignal()?.role === 'Admin' ? '/admin' : '/feed';
  }

  logout(): void {
    this.clearStoredSession();
    this.tokenSignal.set(null);
    this.currentUserSignal.set(null);
  }

  refreshMe(): Observable<UserProfile> {
    return this.http.get<UserProfile>('/api/users/me').pipe(tap((user) => this.setUser(user)));
  }

  setUser(user: UserProfile): void {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this.currentUserSignal.set(user);
  }

  private persistSession(response: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, response.token);
    localStorage.setItem(USER_KEY, JSON.stringify(response.user));
    this.tokenSignal.set(response.token);
    this.currentUserSignal.set(response.user);
  }

  private readStoredUser(): UserProfile | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as UserProfile;
    } catch {
      return null;
    }
  }

  private clearStoredSession(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  }

  private isTokenUsable(token: string | null): boolean {
    if (!token) {
      return false;
    }

    const parts = token.split('.');
    if (parts.length !== 3) {
      return false;
    }

    try {
      const payload = JSON.parse(this.decodeBase64Url(parts[1])) as { exp?: number };
      if (typeof payload.exp !== 'number') {
        return false;
      }

      const clockSkewMs = 30_000;
      return payload.exp * 1000 > Date.now() + clockSkewMs;
    } catch {
      return false;
    }
  }

  private decodeBase64Url(value: string): string {
    const base64 = value.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    return atob(padded);
  }
}
