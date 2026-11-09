import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { MessageResponse } from '../models/auth.model';
import {
  AdminAuditLog,
  AdminContractFilter,
  AdminContractListItem,
  AdminLoginEvent,
  AdminLoginEventFilter,
  AdminOverview,
  AdminPostingFilter,
  AdminPostingListItem,
  AdminUserDetail,
  AdminUserFilter,
  AdminUserListItem,
  CreateAdminRequest,
  PagedResult
} from '../models/admin.model';
import { JobPostingStatus } from '../models/job-posting.model';
import { AccountStatus } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class AdminService {
  constructor(private http: HttpClient) {}

  getOverview(days: number): Observable<AdminOverview> {
    return this.http.get<AdminOverview>('/api/admin/overview', { params: { days } });
  }

  getUsers(filter: AdminUserFilter): Observable<PagedResult<AdminUserListItem>> {
    return this.http.get<PagedResult<AdminUserListItem>>('/api/admin/users', { params: toParams(filter) });
  }

  getUser(id: string): Observable<AdminUserDetail> {
    return this.http.get<AdminUserDetail>(`/api/admin/users/${id}`);
  }

  updateUserStatus(id: string, status: AccountStatus, reason?: string): Observable<MessageResponse> {
    return this.http.put<MessageResponse>(`/api/admin/users/${id}/status`, { status, reason: reason || null });
  }

  unlockUser(id: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`/api/admin/users/${id}/unlock`, {});
  }

  sendPasswordReset(id: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`/api/admin/users/${id}/password-reset`, {});
  }

  createAdmin(request: CreateAdminRequest): Observable<AdminUserListItem> {
    return this.http.post<AdminUserListItem>('/api/admin/admins', request);
  }

  getPostings(filter: AdminPostingFilter): Observable<PagedResult<AdminPostingListItem>> {
    return this.http.get<PagedResult<AdminPostingListItem>>('/api/admin/postings', { params: toParams(filter) });
  }

  updatePostingStatus(id: number, status: JobPostingStatus, reason?: string): Observable<MessageResponse> {
    return this.http.put<MessageResponse>(`/api/admin/postings/${id}/status`, { status, reason: reason || null });
  }

  getContracts(filter: AdminContractFilter): Observable<PagedResult<AdminContractListItem>> {
    return this.http.get<PagedResult<AdminContractListItem>>('/api/admin/contracts', { params: toParams(filter) });
  }

  getLoginEvents(filter: AdminLoginEventFilter): Observable<PagedResult<AdminLoginEvent>> {
    return this.http.get<PagedResult<AdminLoginEvent>>('/api/admin/login-events', { params: toParams(filter) });
  }

  getAuditLog(page: number, pageSize = 30): Observable<PagedResult<AdminAuditLog>> {
    return this.http.get<PagedResult<AdminAuditLog>>('/api/admin/audit', { params: { page, pageSize } });
  }
}

/** Ignora filtros vazios para não enviar `?status=` ao servidor. */
function toParams(filter: object): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(filter)) {
    if (value !== '' && value !== null && value !== undefined) {
      params = params.set(key, String(value));
    }
  }
  return params;
}
