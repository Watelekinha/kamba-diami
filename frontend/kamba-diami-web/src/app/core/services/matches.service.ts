import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateMatchRequest, Match, RespondMatchRequest } from '../models/chat.model';

@Injectable({ providedIn: 'root' })
export class MatchesService {
  constructor(private http: HttpClient) {}

  getMine(): Observable<Match[]> {
    return this.http.get<Match[]>('/api/matches/mine');
  }

  create(request: CreateMatchRequest): Observable<Match> {
    return this.http.post<Match>('/api/matches', request);
  }

  respond(id: number, request: RespondMatchRequest): Observable<Match> {
    return this.http.put<Match>(`/api/matches/${id}/respond`, request);
  }
}
