import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Municipality, Neighborhood, Province, ServiceCategory } from '../models/location.model';

@Injectable({ providedIn: 'root' })
export class LocationService {
  constructor(private http: HttpClient) {}

  getProvinces(): Observable<Province[]> {
    return this.http.get<Province[]>('/api/locations/provinces');
  }

  getMunicipalities(provinceId: number): Observable<Municipality[]> {
    return this.http.get<Municipality[]>(`/api/locations/provinces/${provinceId}/municipalities`);
  }

  getNeighborhoods(municipalityId: number): Observable<Neighborhood[]> {
    return this.http.get<Neighborhood[]>(`/api/locations/municipalities/${municipalityId}/neighborhoods`);
  }

  getServiceCategories(): Observable<ServiceCategory[]> {
    return this.http.get<ServiceCategory[]>('/api/service-categories');
  }

  createServiceCategory(name: string): Observable<ServiceCategory> {
    return this.http.post<ServiceCategory>('/api/service-categories', { name });
  }

  createNeighborhood(municipalityId: number, name: string): Observable<Neighborhood> {
    return this.http.post<Neighborhood>('/api/locations/neighborhoods', { municipalityId, name });
  }

  createMunicipality(provinceId: number, name: string): Observable<Municipality> {
    return this.http.post<Municipality>('/api/locations/municipalities', { provinceId, name });
  }
}
