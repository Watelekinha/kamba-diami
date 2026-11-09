import { UserRole } from './user.model';

export type JobPostingType = 'OfertaEmprego' | 'ProcuraEmprego';
export type PaymentType = 'Diaria' | 'Mensal';
export type JobPostingStatus = 'Ativo' | 'Pausado' | 'Preenchido' | 'Removido';

export interface JobPostingLocationDto {
  neighborhoodId: number;
  neighborhoodName: string;
  municipalityId: number;
  municipalityName: string;
  provinceId: number;
  provinceName: string;
}

export interface JobPostingCategoryRef {
  id: number;
  name: string;
}

export interface JobPosting {
  id: number;
  authorUserId: string;
  authorName: string;
  authorPhotoPath?: string | null;
  authorRole: UserRole;
  authorAverageRating?: number | null;
  authorRatingsCount: number;
  type: JobPostingType;
  paymentType: PaymentType;
  paymentValue: number;
  hoursPerDay?: number | null;
  comments?: string | null;
  status: JobPostingStatus;
  categories: JobPostingCategoryRef[];
  locations: JobPostingLocationDto[];
  createdAt: string;
}

export interface CreateJobPostingRequest {
  categoryIds: number[];
  paymentType: PaymentType;
  paymentValue: number;
  hoursPerDay?: number | null;
  comments?: string | null;
  neighborhoodIds: number[];
}

export interface JobPostingFilter {
  type?: JobPostingType;
  categoryId?: number;
  paymentType?: PaymentType;
  provinceId?: number;
  municipalityId?: number;
  neighborhoodId?: number;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
