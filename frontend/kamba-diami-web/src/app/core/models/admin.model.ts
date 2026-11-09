import { ContractStatus } from './contract.model';
import { JobPostingStatus, JobPostingType, PagedResult, PaymentType } from './job-posting.model';
import { AccountStatus, UserRole } from './user.model';

export type { PagedResult };

export type AdminMetricKey =
  | 'registrations'
  | 'logins'
  | 'activeUsers'
  | 'postings'
  | 'matches'
  | 'contracts'
  | 'messages'
  | 'failedLogins';

export interface AdminMetric {
  key: AdminMetricKey;
  current: number;
  previous: number;
  /** null quando o período anterior foi zero: a interface mostra "novo". */
  changePercent: number | null;
}

export interface AdminDailyPoint {
  date: string;
  registrations: number;
  logins: number;
  activeUsers: number;
  postings: number;
  matches: number;
  contracts: number;
  messages: number;
}

export interface AdminTotals {
  users: number;
  familias: number;
  secretarias: number;
  admins: number;
  suspendedUsers: number;
  activePostings: number;
  postings: number;
  matches: number;
  contracts: number;
  activeContracts: number;
  completedContracts: number;
  messages: number;
  ratings: number;
  averageRating: number | null;
  activeUsersToday: number;
}

export interface AdminFunnel {
  postings: number;
  matches: number;
  acceptedMatches: number;
  contracts: number;
  signedContracts: number;
}

export interface AdminCount {
  label: string;
  count: number;
}

export interface AdminActivityItem {
  type: 'registration' | 'posting' | 'match' | 'contract';
  description: string;
  occurredAt: string;
  userId: string | null;
  userName: string | null;
}

export interface AdminOverview {
  periodDays: number;
  from: string;
  to: string;
  generatedAt: string;
  totals: AdminTotals;
  metrics: AdminMetric[];
  series: AdminDailyPoint[];
  funnel: AdminFunnel;
  contractsByStatus: AdminCount[];
  usersByProvince: AdminCount[];
  topCategories: AdminCount[];
  recentActivity: AdminActivityItem[];
}

export interface AdminUserListItem {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  photoPath: string | null;
  role: UserRole;
  status: AccountStatus;
  location: string;
  createdAt: string;
  lastLoginAt: string | null;
  isLockedOut: boolean;
  postingsCount: number;
  contractsCount: number;
}

export interface AdminLoginEvent {
  id: number;
  userId: string | null;
  userName: string | null;
  email: string;
  succeeded: boolean;
  failureReason: string | null;
  ipAddress: string | null;
  userAgent: string | null;
  occurredAt: string;
}

export interface AdminPostingListItem {
  id: number;
  type: JobPostingType;
  status: JobPostingStatus;
  paymentType: PaymentType;
  paymentValue: number;
  comments: string | null;
  authorId: string;
  authorName: string;
  categories: string[];
  locations: string[];
  matchesCount: number;
  createdAt: string;
}

export interface AdminContractListItem {
  id: number;
  reference: string;
  status: ContractStatus;
  paymentType: PaymentType;
  paymentValue: number;
  familiaId: string;
  familiaName: string;
  secretariaId: string;
  secretariaName: string;
  createdAt: string;
  updatedAt: string;
}

export interface AdminUserDetail extends AdminUserListItem {
  suspendedAt: string | null;
  suspensionReason: string | null;
  lockoutEnd: string | null;
  failedLoginAttempts: number;
  loginsLast30Days: number;
  activeDaysLast30Days: number;
  matchesCount: number;
  messagesCount: number;
  averageRating: number | null;
  ratingsCount: number;
  recentLogins: AdminLoginEvent[];
  recentPostings: AdminPostingListItem[];
  recentContracts: AdminContractListItem[];
}

export interface AdminAuditLog {
  id: number;
  adminUserId: string;
  adminName: string;
  action: string;
  targetType: string;
  targetId: string | null;
  details: string | null;
  createdAt: string;
}

export interface AdminUserFilter {
  search?: string;
  role?: UserRole | '';
  status?: AccountStatus | '';
  sort?: 'recent' | 'oldest' | 'name' | 'lastLogin';
  page?: number;
  pageSize?: number;
}

export interface AdminPostingFilter {
  search?: string;
  status?: JobPostingStatus | '';
  type?: JobPostingType | '';
  page?: number;
  pageSize?: number;
}

export interface AdminContractFilter {
  search?: string;
  status?: ContractStatus | '';
  page?: number;
  pageSize?: number;
}

export interface AdminLoginEventFilter {
  search?: string;
  succeeded?: boolean | '';
  page?: number;
  pageSize?: number;
}

export interface CreateAdminRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  password: string;
}
