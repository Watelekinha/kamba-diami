import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateJobPostingRequest,
  JobPosting,
  JobPostingFilter,
  JobPostingType,
  PagedResult
} from '../models/job-posting.model';

@Injectable({ providedIn: 'root' })
export class JobPostingsService {
  constructor(private http: HttpClient) {}

  getFeed(filter: JobPostingFilter): Observable<PagedResult<JobPosting>> {
    let params = new HttpParams();
    Object.entries(filter).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });
    return this.http.get<PagedResult<JobPosting>>('/api/job-postings', { params });
  }

  getMine(): Observable<JobPosting[]> {
    return this.http.get<JobPosting[]>('/api/job-postings/mine');
  }

  getPreview(type: JobPostingType, take = 3): Observable<PagedResult<JobPosting>> {
    const params = new HttpParams().set('type', type).set('take', String(take));
    return this.http.get<PagedResult<JobPosting>>('/api/job-postings/preview', { params });
  }

  getById(id: number): Observable<JobPosting> {
    return this.http.get<JobPosting>(`/api/job-postings/${id}`);
  }

  create(request: CreateJobPostingRequest): Observable<JobPosting> {
    return this.http.post<JobPosting>('/api/job-postings', request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/job-postings/${id}`);
  }
}
