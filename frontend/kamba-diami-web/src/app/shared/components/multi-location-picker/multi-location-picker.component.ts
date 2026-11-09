import { CommonModule } from '@angular/common';
import { Component, EventEmitter, OnInit, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocationService } from '../../../core/services/location.service';
import { Municipality, Neighborhood, Province } from '../../../core/models/location.model';
import { apiErrorMessage } from '../../../core/utils/api-error';

interface SelectedLocation {
  neighborhoodId: number;
  label: string;
}

/**
 * Escolha de vários bairros para um anúncio. As províncias e os municípios vêm da API pública de Angola;
 * os bairros são da plataforma, com as comunas da API como sugestão.
 */
@Component({
  selector: 'app-multi-location-picker',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './multi-location-picker.component.html',
  styleUrl: './multi-location-picker.component.scss'
})
export class MultiLocationPickerComponent implements OnInit {
  @Output() selectionChange = new EventEmitter<number[]>();
  @Output() labelsChange = new EventEmitter<string[]>();

  provinces: Province[] = [];
  municipalities: Municipality[] = [];
  neighborhoods: Neighborhood[] = [];

  selectedProvinceSlug: string | null = null;
  selectedMunicipalitySlug: string | null = null;
  /** O select de bairro volta sempre a vazio: cada escolha é acrescentada à lista. */
  selectedNeighborhood: Neighborhood | null = null;

  selectedLocations: SelectedLocation[] = [];

  loadingProvinces = signal(true);
  addingNeighborhood = signal(false);
  newNeighborhoodName = '';
  savingNeighborhood = signal(false);

  readonly addError = signal<string | null>(null);

  constructor(private locationService: LocationService) {}

  ngOnInit(): void {
    this.locationService.getProvinces().subscribe({
      next: (provinces) => {
        this.loadingProvinces.set(false);
        this.provinces = provinces;
      },
      error: (err) => {
        this.loadingProvinces.set(false);
        this.addError.set(apiErrorMessage(err, 'Não foi possível carregar as províncias. Actualiza a página e tenta de novo.'));
      }
    });
  }

  onProvinceChange(provinceSlug: string | null): void {
    this.selectedProvinceSlug = provinceSlug;
    this.selectedMunicipalitySlug = null;
    this.selectedNeighborhood = null;
    this.neighborhoods = [];
    this.addingNeighborhood.set(false);

    if (!provinceSlug) {
      this.municipalities = [];
      return;
    }

    this.locationService.getMunicipalities(provinceSlug).subscribe({
      next: (municipalities) => (this.municipalities = municipalities),
      error: (err) => this.addError.set(apiErrorMessage(err, 'Não foi possível carregar os municípios.'))
    });
  }

  onMunicipalityChange(municipalitySlug: string | null): void {
    this.selectedMunicipalitySlug = municipalitySlug;
    this.selectedNeighborhood = null;
    this.addingNeighborhood.set(false);

    if (!municipalitySlug) {
      this.neighborhoods = [];
      return;
    }

    this.locationService.getNeighborhoods(municipalitySlug).subscribe({
      next: (neighborhoods) => (this.neighborhoods = neighborhoods),
      error: (err) => this.addError.set(apiErrorMessage(err, 'Não foi possível carregar os bairros.'))
    });
  }

  onNeighborhoodChange(neighborhood: Neighborhood | null): void {
    this.selectedNeighborhood = neighborhood;
    // Volta a "Bairro" logo a seguir, para se poder escolher outro.
    setTimeout(() => (this.selectedNeighborhood = null));
    if (!neighborhood) return;

    this.addError.set(null);
    // Uma comuna sugerida pela API passa a bairro da plataforma quando é escolhida.
    this.locationService.ensureNeighborhood(neighborhood).subscribe({
      next: (saved) => {
        if (saved !== neighborhood) {
          this.neighborhoods = this.neighborhoods.map((n) => (n === neighborhood ? saved : n));
        }
        this.addLocation(saved);
      },
      error: (err) => this.addError.set(apiErrorMessage(err, 'Não foi possível escolher este bairro. Tenta de novo.'))
    });
  }

  removeLocation(neighborhoodId: number): void {
    this.selectedLocations = this.selectedLocations.filter((l) => l.neighborhoodId !== neighborhoodId);
    this.emitChange();
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
        this.addLocation(neighborhood);
      },
      error: (err) => {
        this.savingNeighborhood.set(false);
        this.addError.set(apiErrorMessage(err, 'Não foi possível adicionar o bairro. Tenta de novo.'));
      }
    });
  }

  private addLocation(neighborhood: Neighborhood): void {
    const id = neighborhood.id;
    if (id === null || this.selectedLocations.some((l) => l.neighborhoodId === id)) return;

    const municipality = this.municipalities.find((m) => m.slug === neighborhood.municipalitySlug);
    this.selectedLocations.push({
      neighborhoodId: id,
      label: municipality ? `${neighborhood.name}, ${municipality.name}` : neighborhood.name
    });
    this.emitChange();
  }

  private emitChange(): void {
    this.selectionChange.emit(this.selectedLocations.map((l) => l.neighborhoodId));
    this.labelsChange.emit(this.selectedLocations.map((l) => l.label));
  }
}
