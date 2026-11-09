import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of, shareReplay } from 'rxjs';
import { Municipality, Neighborhood, Province, ServiceCategory } from '../models/location.model';

/**
 * As províncias e os municípios vêm da API pública de Angola, através do nosso backend
 * (que guarda a resposta em cache). Os bairros são da plataforma.
 */
@Injectable({ providedIn: 'root' })
export class LocationService {
  private provinces$?: Observable<Province[]>;
  private readonly municipalities$ = new Map<string, Observable<Municipality[]>>();

  constructor(private http: HttpClient) {}

  getProvinces(): Observable<Province[]> {
    this.provinces$ ??= this.http.get<Province[]>('/api/locations/provinces').pipe(shareReplay(1));
    return this.provinces$;
  }

  getMunicipalities(provinceSlug: string): Observable<Municipality[]> {
    let cached = this.municipalities$.get(provinceSlug);
    if (!cached) {
      cached = this.http
        .get<Municipality[]>(`/api/locations/provinces/${encodeURIComponent(provinceSlug)}/municipalities`)
        .pipe(shareReplay(1));
      this.municipalities$.set(provinceSlug, cached);
    }
    return cached;
  }

  getNeighborhoods(municipalitySlug: string): Observable<Neighborhood[]> {
    return this.http.get<Neighborhood[]>(
      `/api/locations/municipalities/${encodeURIComponent(municipalitySlug)}/neighborhoods`
    );
  }

  createNeighborhood(municipalitySlug: string, name: string): Observable<Neighborhood> {
    return this.http.post<Neighborhood>('/api/locations/neighborhoods', { municipalitySlug, name });
  }

  /** Devolve o bairro com id; uma comuna sugerida pela API é criada como bairro primeiro. */
  ensureNeighborhood(neighborhood: Neighborhood): Observable<Neighborhood> {
    return neighborhood.id !== null
      ? of(neighborhood)
      : this.createNeighborhood(neighborhood.municipalitySlug, neighborhood.name);
  }

  getServiceCategories(): Observable<ServiceCategory[]> {
    return this.http.get<ServiceCategory[]>('/api/service-categories');
  }

  createServiceCategory(name: string): Observable<ServiceCategory> {
    return this.http.post<ServiceCategory>('/api/service-categories', { name });
  }
}
