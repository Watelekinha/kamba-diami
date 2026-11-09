export type UserRole = 'Familia' | 'Secretaria' | 'Admin';
export type AccountStatus = 'PendenteAprovacao' | 'Ativo' | 'Suspenso';

export interface AddressDto {
  neighborhoodId: number;
  neighborhoodName: string;
  municipalitySlug: string;
  municipalityName: string;
  provinceSlug: string;
  provinceName: string;
  streetDetail?: string | null;
}

export interface UserProfile {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  photoPath?: string | null;
  role: UserRole;
  status: AccountStatus;
  address: AddressDto;
  averageRating?: number | null;
  ratingsCount: number;
  bio?: string | null;
  yearsOfExperience?: number | null;
  familyDescription?: string | null;
  householdSize?: number | null;
}

export interface ProfileRating {
  id: number;
  contractId: number;
  score: number;
  comment?: string | null;
  createdAt: string;
  raterUserId: string;
  raterName: string;
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  municipalitySlug: string;
  streetDetail?: string | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}
