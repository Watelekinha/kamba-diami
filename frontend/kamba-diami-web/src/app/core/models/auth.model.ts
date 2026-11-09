import { UserProfile, UserRole } from './user.model';

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  email: string;
  password: string;
  confirmPassword: string;
  role: UserRole;
  neighborhoodId: number;
  streetDetail?: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: UserProfile;
}

export interface TokenResponse {
  token: string;
  expiresAt: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface MessageResponse {
  message: string;
}

/** Regras iguais às do backend (PasswordRules.cs). */
export const PASSWORD_MIN_LENGTH = 8;

export function passwordProblems(password: string): string[] {
  const problems: string[] = [];
  if (password.length < PASSWORD_MIN_LENGTH) problems.push(`pelo menos ${PASSWORD_MIN_LENGTH} caracteres`);
  if (!/[A-Za-zÀ-ÿ]/.test(password)) problems.push('uma letra');
  if (!/[0-9]/.test(password)) problems.push('um número');
  return problems;
}

/** Aceita 9XX XXX XXX, com ou sem +244, tal como AngolaPhone.cs. */
export function isAngolanMobile(phone: string): boolean {
  const compact = phone.replace(/[^\d+]/g, '');
  return /^(?:\+?244|00244)?9\d{8}$/.test(compact);
}
