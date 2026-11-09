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

  selectedProvinceId: number | null = null;
  selectedMunicipalityId: number | null = null;
  selectedNeighborhoodId: number | null = null;

  selectedLocations: SelectedLocation[] = [];

  addingMunicipality = signal(false);
  newMunicipalityName = '';
  savingMunicipality = signal(false);

  addingNeighborhood = signal(false);
  newNeighborhoodName = '';
  savingNeighborhood = signal(false);

  constructor(private locationService: LocationService) {}

  ngOnInit(): void {
    this.locationService.getProvinces().subscribe((provinces) => (this.provinces = provinces));
  }

  onProvinceChange(provinceId: number | null): void {
    this.selectedProvinceId = provinceId;
    this.selectedMunicipalityId = null;
    this.selectedNeighborhoodId = null;
    this.neighborhoods = [];
    this.addingMunicipality.set(false);
    this.addingNeighborhood.set(false);

    if (!provinceId) {
      this.municipalities = [];
      return;
    }

    this.locationService.getMunicipalities(provinceId).subscribe((municipalities) => {
      this.municipalities = municipalities;
    });
  }

  onMunicipalityChange(municipalityId: number | null): void {
    this.selectedMunicipalityId = municipalityId;
    this.selectedNeighborhoodId = null;
    this.addingNeighborhood.set(false);

    if (!municipalityId) {
      this.neighborhoods = [];
      return;
    }

    this.locationService.getNeighborhoods(municipalityId).subscribe((neighborhoods) => {
      this.neighborhoods = neighborhoods;
    });
  }

  onNeighborhoodChange(neighborhoodId: number | null): void {
    this.selectedNeighborhoodId = neighborhoodId;
    this.addLocation();
    this.selectedNeighborhoodId = null;
  }

  addLocation(): void {
    if (!this.selectedNeighborhoodId) return;
    if (this.selectedLocations.some((l) => l.neighborhoodId === this.selectedNeighborhoodId)) return;

    const neighborhood = this.neighborhoods.find((n) => n.id === this.selectedNeighborhoodId);
    const municipality = this.municipalities.find((m) => m.id === this.selectedMunicipalityId);
    if (!neighborhood || !municipality) return;

    this.selectedLocations.push({
      neighborhoodId: neighborhood.id,
      label: `${neighborhood.name}, ${municipality.name}`
    });
    this.emitChange();
  }

  removeLocation(neighborhoodId: number): void {
    this.selectedLocations = this.selectedLocations.filter((l) => l.neighborhoodId !== neighborhoodId);
    this.emitChange();
  }

  readonly addError = signal<string | null>(null);

  toggleAddMunicipality(): void {
    this.addError.set(null);
    this.addingMunicipality.update((v) => !v);
    this.newMunicipalityName = '';
  }

  confirmAddMunicipality(): void {
    const name = this.newMunicipalityName.trim();
    if (!name || !this.selectedProvinceId) return;

    this.addError.set(null);
    this.savingMunicipality.set(true);
    this.locationService.createMunicipality(this.selectedProvinceId, name).subscribe({
      next: (municipality) => {
        this.savingMunicipality.set(false);
        this.addingMunicipality.set(false);
        this.municipalities = [...this.municipalities, municipality].sort((a, b) => a.name.localeCompare(b.name));
        this.onMunicipalityChange(municipality.id);
      },
      error: (err) => {
        this.savingMunicipality.set(false);
        this.addError.set(apiErrorMessage(err, 'Não foi possível adicionar o município. Tenta de novo.'));
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
    if (!name || !this.selectedMunicipalityId) return;

    this.addError.set(null);
    this.savingNeighborhood.set(true);
    this.locationService.createNeighborhood(this.selectedMunicipalityId, name).subscribe({
      next: (neighborhood) => {
        this.savingNeighborhood.set(false);
        this.addingNeighborhood.set(false);
        this.neighborhoods = [...this.neighborhoods, neighborhood].sort((a, b) => a.name.localeCompare(b.name));
        this.onNeighborhoodChange(neighborhood.id);
      },
      error: (err) => {
        this.savingNeighborhood.set(false);
        this.addError.set(apiErrorMessage(err, 'Não foi possível adicionar o bairro. Tenta de novo.'));
      }
    });
  }

  private emitChange(): void {
    this.selectionChange.emit(this.selectedLocations.map((l) => l.neighborhoodId));
    this.labelsChange.emit(this.selectedLocations.map((l) => l.label));
  }
}
