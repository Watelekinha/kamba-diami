import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Contract, ContractListItem, CreateContractRequest, UpdateContractClausesRequest } from '../models/contract.model';

@Injectable({ providedIn: 'root' })
export class ContractsService {
  constructor(private http: HttpClient) {}

  create(request: CreateContractRequest): Observable<Contract> {
    return this.http.post<Contract>('/api/contracts', request);
  }

  getByMatch(matchId: number): Observable<Contract> {
    return this.http.get<Contract>(`/api/contracts/by-match/${matchId}`);
  }

  getById(id: number): Observable<Contract> {
    return this.http.get<Contract>(`/api/contracts/${id}`);
  }

  getMine(): Observable<ContractListItem[]> {
    return this.http.get<ContractListItem[]>('/api/contracts/mine');
  }

  updateClauses(id: number, request: UpdateContractClausesRequest): Observable<Contract> {
    return this.http.put<Contract>(`/api/contracts/${id}/clauses`, request);
  }

  generatePdf(id: number): Observable<Blob> {
    return this.http.post(`/api/contracts/${id}/generate-pdf`, {}, { responseType: 'blob' });
  }

  downloadPdf(id: number): Observable<Blob> {
    return this.http.get(`/api/contracts/${id}/pdf`, { responseType: 'blob' });
  }

  uploadSigned(id: number, file: File): Observable<Contract> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<Contract>(`/api/contracts/${id}/signed`, form);
  }

  downloadSigned(id: number, party: 'familia' | 'secretaria'): Observable<Blob> {
    return this.http.get(`/api/contracts/${id}/signed/${party}`, { responseType: 'blob' });
  }

  downloadSignedByBoth(id: number): Observable<Blob> {
    return this.http.get(`/api/contracts/${id}/signed`, { responseType: 'blob' });
  }

  cancel(id: number, reason: string): Observable<Contract> {
    return this.http.post<Contract>(`/api/contracts/${id}/cancel`, { reason });
  }

  confirmEnd(id: number): Observable<Contract> {
    return this.http.post<Contract>(`/api/contracts/${id}/confirm-end`, {});
  }

  rate(id: number, score: number, comment: string | null): Observable<Contract> {
    return this.http.post<Contract>(`/api/contracts/${id}/ratings`, { score, comment });
  }

  saveBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  }

  openBlob(blob: Blob): void {
    const url = URL.createObjectURL(blob);
    window.open(url, '_blank', 'noopener');
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  }
}
