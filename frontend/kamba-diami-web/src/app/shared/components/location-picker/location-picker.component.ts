import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocationService } from '../../../core/services/location.service';
import { Municipality, Neighborhood, Province } from '../../../core/models/location.model';
import { apiErrorMessage } from '../../../core/utils/api-error';

@Component({
  selector: 'app-location-picker',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './location-picker.component.html',
  styleUrl: './location-picker.component.scss'
})
export class LocationPickerComponent implements OnInit {
  @Input() initialProvinceId: number | null = null;
  @Input() initialMunicipalityId: number | null = null;
  @Input() initialNeighborhoodId: number | null = null;
  @Output() neighborhoodChange = new EventEmitter<number | null>();

  provinces: Province[] = [];
  municipalities: Municipality[] = [];
  neighborhoods: Neighborhood[] = [];

  selectedProvinceId: number | null = null;
  selectedMunicipalityId: number | null = null;
  selectedNeighborhoodId: number | null = null;

  addingMunicipality = signal(false);
  newMunicipalityName = '';
  savingMunicipality = signal(false);

  addingNeighborhood = signal(false);
  newNeighborhoodName = '';
  savingNeighborhood = signal(false);

  addError = signal<string | null>(null);

  constructor(private locationService: LocationService) {}

  ngOnInit(): void {
    this.selectedProvinceId = this.initialProvinceId;
    this.selectedMunicipalityId = this.initialMunicipalityId;
    this.selectedNeighborhoodId = this.initialNeighborhoodId;

    this.locationService.getProvinces().subscribe((provinces) => {
      this.provinces = provinces;
      if (this.selectedProvinceId) {
        this.onProvinceChange(this.selectedProvinceId, false);
      }
    });
  }

  onProvinceChange(provinceId: number | null, resetChildren = true): void {
    this.selectedProvinceId = provinceId;
    this.addingMunicipality.set(false);
    this.addingNeighborhood.set(false);

    if (resetChildren) {
      this.selectedMunicipalityId = null;
      this.selectedNeighborhoodId = null;
      this.neighborhoods = [];
      this.neighborhoodChange.emit(null);
    }

    if (!provinceId) {
      this.municipalities = [];
      return;
    }

    this.locationService.getMunicipalities(provinceId).subscribe((municipalities) => {
      this.municipalities = municipalities;
      if (this.selectedMunicipalityId) {
        this.onMunicipalityChange(this.selectedMunicipalityId, false);
      }
    });
  }

  onMunicipalityChange(municipalityId: number | null, resetChildren = true): void {
    this.selectedMunicipalityId = municipalityId;
    this.addingNeighborhood.set(false);

    if (resetChildren) {
      this.selectedNeighborhoodId = null;
      this.neighborhoodChange.emit(null);
    }

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
    this.neighborhoodChange.emit(neighborhoodId);
  }

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
        this.onMunicipalityChange(municipality.id, true);
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
}
