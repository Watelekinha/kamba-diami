import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocationService } from '../../../core/services/location.service';
import { Municipality, Neighborhood, Province } from '../../../core/models/location.model';
import { apiErrorMessage } from '../../../core/utils/api-error';

/**
 * Escolha de província, município e bairro. As províncias e os municípios vêm da API pública de Angola;
 * os bairros são da plataforma, com as comunas da API como sugestão.
 */
@Component({
  selector: 'app-location-picker',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './location-picker.component.html',
  styleUrl: './location-picker.component.scss'
})
export class LocationPickerComponent implements OnInit {
  @Input() initialProvinceSlug: string | null = null;
  @Input() initialMunicipalitySlug: string | null = null;
  @Input() initialNeighborhoodId: number | null = null;
  @Output() neighborhoodChange = new EventEmitter<number | null>();

  provinces: Province[] = [];
  municipalities: Municipality[] = [];
  neighborhoods: Neighborhood[] = [];

  selectedProvinceSlug: string | null = null;
  selectedMunicipalitySlug: string | null = null;
  selectedNeighborhood: Neighborhood | null = null;

  loadingProvinces = signal(true);
  loadError = signal<string | null>(null);

  addingNeighborhood = signal(false);
  newNeighborhoodName = '';
  savingNeighborhood = signal(false);

  addError = signal<string | null>(null);

  constructor(private locationService: LocationService) {}

  ngOnInit(): void {
    this.selectedProvinceSlug = this.initialProvinceSlug;
    this.selectedMunicipalitySlug = this.initialMunicipalitySlug;

    this.locationService.getProvinces().subscribe({
      next: (provinces) => {
        this.loadingProvinces.set(false);
        this.provinces = provinces;
        if (this.selectedProvinceSlug) {
          this.onProvinceChange(this.selectedProvinceSlug, false);
        }
      },
      error: (err) => {
        this.loadingProvinces.set(false);
        this.loadError.set(apiErrorMessage(err, 'Não foi possível carregar as províncias. Actualiza a página e tenta de novo.'));
      }
    });
  }

  /** Compara bairros pelo id ou, nas sugestões, pelo nome. */
  sameNeighborhood = (a: Neighborhood | null, b: Neighborhood | null): boolean =>
    a === b || (!!a && !!b && a.municipalitySlug === b.municipalitySlug && (a.id ?? a.name) === (b.id ?? b.name));

  onProvinceChange(provinceSlug: string | null, resetChildren = true): void {
    this.selectedProvinceSlug = provinceSlug;
    this.addingNeighborhood.set(false);

    if (resetChildren) {
      this.selectedMunicipalitySlug = null;
      this.selectedNeighborhood = null;
      this.neighborhoods = [];
      this.neighborhoodChange.emit(null);
    }

    if (!provinceSlug) {
      this.municipalities = [];
      return;
    }

    this.locationService.getMunicipalities(provinceSlug).subscribe({
      next: (municipalities) => {
        this.municipalities = municipalities;
        if (this.selectedMunicipalitySlug) {
          this.onMunicipalityChange(this.selectedMunicipalitySlug, false);
        }
      },
      error: (err) => this.loadError.set(apiErrorMessage(err, 'Não foi possível carregar os municípios.'))
    });
  }

  onMunicipalityChange(municipalitySlug: string | null, resetChildren = true): void {
    this.selectedMunicipalitySlug = municipalitySlug;
    this.addingNeighborhood.set(false);

    if (resetChildren) {
      this.selectedNeighborhood = null;
      this.neighborhoodChange.emit(null);
    }

    if (!municipalitySlug) {
      this.neighborhoods = [];
      return;
    }

    this.locationService.getNeighborhoods(municipalitySlug).subscribe({
      next: (neighborhoods) => {
        this.neighborhoods = neighborhoods;
        if (!resetChildren && this.initialNeighborhoodId !== null) {
          this.selectedNeighborhood = neighborhoods.find((n) => n.id === this.initialNeighborhoodId) ?? null;
        }
      },
      error: (err) => this.loadError.set(apiErrorMessage(err, 'Não foi possível carregar os bairros.'))
    });
  }

  onNeighborhoodChange(neighborhood: Neighborhood | null): void {
    this.addError.set(null);
    this.selectedNeighborhood = neighborhood;
    if (!neighborhood) {
      this.neighborhoodChange.emit(null);
      return;
    }

    // Uma comuna sugerida pela API passa a bairro da plataforma quando é escolhida.
    this.locationService.ensureNeighborhood(neighborhood).subscribe({
      next: (saved) => {
        this.replaceNeighborhood(neighborhood, saved);
        this.neighborhoodChange.emit(saved.id);
      },
      error: (err) => {
        this.selectedNeighborhood = null;
        this.neighborhoodChange.emit(null);
        this.addError.set(apiErrorMessage(err, 'Não foi possível escolher este bairro. Tenta de novo.'));
      }
    });
  }

  toggleAddNeighborhood(): void {
    this.addError.set(null);
    this.addingNeighborhood.update((v) => !v);
    this.newNeighborhoodName = '';
  }

  confirmAddNeighborhood(): void {
    const name = this.newNeighborhoodName.trim();
    if (!name || !this.selectedMunicipalitySlug) return;

    this.addError.set(null);
    this.savingNeighborhood.set(true);
    this.locationService.createNeighborhood(this.selectedMunicipalitySlug, name).subscribe({
      next: (neighborhood) => {
        this.savingNeighborhood.set(false);
        this.addingNeighborhood.set(false);
        if (!this.neighborhoods.some((n) => n.id === neighborhood.id)) {
          this.neighborhoods = [...this.neighborhoods, neighborhood].sort((a, b) => a.name.localeCompare(b.name));
        }
        this.selectedNeighborhood = neighborhood;
        this.neighborhoodChange.emit(neighborhood.id);
      },
      error: (err) => {
        this.savingNeighborhood.set(false);
        this.addError.set(apiErrorMessage(err, 'Não foi possível adicionar o bairro. Tenta de novo.'));
      }
    });
  }

  private replaceNeighborhood(previous: Neighborhood, saved: Neighborhood): void {
    if (previous === saved) return;
    this.neighborhoods = this.neighborhoods.map((n) => (n === previous ? saved : n));
    this.selectedNeighborhood = saved;
  }
}
