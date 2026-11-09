import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { TokenResponse } from '../models/auth.model';
import { ChangePasswordRequest, ProfileRating, UpdateProfileRequest, UserProfile } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UsersService {
  constructor(private http: HttpClient) {}

  getById(id: string): Observable<UserProfile> {
    return this.http.get<UserProfile>(`/api/users/${id}`);
  }

  getRatings(userId: string): Observable<ProfileRating[]> {
    return this.http.get<ProfileRating[]>(`/api/users/${userId}/ratings`);
  }

  updateMe(request: UpdateProfileRequest): Observable<UserProfile> {
    return this.http.put<UserProfile>('/api/users/me', request);
  }

  changePassword(request: ChangePasswordRequest): Observable<TokenResponse> {
    return this.http.put<TokenResponse>('/api/users/me/password', request);
  }

  uploadPhoto(file: File): Observable<UserProfile> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<UserProfile>('/api/users/me/photo', formData);
  }
}
