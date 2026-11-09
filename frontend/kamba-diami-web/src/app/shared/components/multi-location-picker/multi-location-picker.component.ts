import { CommonModule } from '@angular/common';
import { Component, EventEmitter, OnInit, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocationService } from '../../../core/services/location.service';
import { Municipality, Province } from '../../../core/models/location.model';
import { apiErrorMessage } from '../../../core/utils/api-error';

interface SelectedLocation {
  municipalitySlug: string;
  label: string;
}

/**
 * Escolha de vários municípios para um anúncio. As províncias e os municípios vêm da API pública de Angola;
 * a plataforma já não pede o bairro.
 */
@Component({
  selector: 'app-multi-location-picker',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './multi-location-picker.component.html',
  styleUrl: './multi-location-picker.component.scss'
})
export class MultiLocationPickerComponent implements OnInit {
  @Output() selectionChange = new EventEmitter<string[]>();
  @Output() labelsChange = new EventEmitter<string[]>();

  provinces: Province[] = [];
  municipalities: Municipality[] = [];

  selectedProvinceSlug: string | null = null;
  /** O select de município volta sempre a vazio: cada escolha é acrescentada à lista. */
  selectedMunicipalitySlug: string | null = null;

  selectedLocations: SelectedLocation[] = [];

  loadingProvinces = signal(true);
  readonly loadError = signal<string | null>(null);

  constructor(private locationService: LocationService) {}

  ngOnInit(): void {
    this.locationService.getProvinces().subscribe({
      next: (provinces) => {
        this.loadingProvinces.set(false);
        this.provinces = provinces;
      },
      error: (err) => {
        this.loadingProvinces.set(false);
        this.loadError.set(apiErrorMessage(err, 'Não foi possível carregar as províncias. Actualiza a página e tenta de novo.'));
      }
    });
  }

  onProvinceChange(provinceSlug: string | null): void {
    this.selectedProvinceSlug = provinceSlug;
    this.selectedMunicipalitySlug = null;
    this.municipalities = [];
    if (!provinceSlug) return;

    this.locationService.getMunicipalities(provinceSlug).subscribe({
      next: (municipalities) => (this.municipalities = municipalities),
      error: (err) => this.loadError.set(apiErrorMessage(err, 'Não foi possível carregar os municípios.'))
    });
  }

  onMunicipalityChange(municipalitySlug: string | null): void {
    this.selectedMunicipalitySlug = municipalitySlug;
    // Volta a "Município" logo a seguir, para se poder escolher outro.
    setTimeout(() => (this.selectedMunicipalitySlug = null));
    if (!municipalitySlug || this.selectedLocations.some((l) => l.municipalitySlug === municipalitySlug)) return;

    const municipality = this.municipalities.find((m) => m.slug === municipalitySlug);
    const province = this.provinces.find((p) => p.slug === this.selectedProvinceSlug);
    const name = municipality?.name ?? municipalitySlug;
    this.selectedLocations.push({
      municipalitySlug,
      label: province ? `${name}, ${province.name}` : name
    });
    this.emitChange();
  }

  removeLocation(municipalitySlug: string): void {
    this.selectedLocations = this.selectedLocations.filter((l) => l.municipalitySlug !== municipalitySlug);
    this.emitChange();
  }

  private emitChange(): void {
    this.selectionChange.emit(this.selectedLocations.map((l) => l.municipalitySlug));
    this.labelsChange.emit(this.selectedLocations.map((l) => l.label));
  }
}
