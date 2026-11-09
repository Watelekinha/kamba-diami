import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocationService } from '../../../core/services/location.service';
import { Municipality, Province } from '../../../core/models/location.model';
import { apiErrorMessage } from '../../../core/utils/api-error';

/**
 * Escolha de província e município, ambos da API pública de Angola. A plataforma já não pede o bairro:
 * a morada fica ao nível do município (o detalhe da rua é um campo de texto à parte).
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
  @Output() municipalityChange = new EventEmitter<string | null>();

  provinces: Province[] = [];
  municipalities: Municipality[] = [];

  selectedProvinceSlug: string | null = null;
  selectedMunicipalitySlug: string | null = null;

  loadingProvinces = signal(true);
  loadError = signal<string | null>(null);

  constructor(private locationService: LocationService) {}

  ngOnInit(): void {
    this.selectedProvinceSlug = this.initialProvinceSlug;
    this.selectedMunicipalitySlug = this.initialMunicipalitySlug;

    this.locationService.getProvinces().subscribe({
      next: (provinces) => {
        this.loadingProvinces.set(false);
        this.provinces = provinces;
        if (this.selectedProvinceSlug) {
          this.loadMunicipalities(this.selectedProvinceSlug);
        }
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
    this.municipalityChange.emit(null);
    this.municipalities = [];
    if (provinceSlug) {
      this.loadMunicipalities(provinceSlug);
    }
  }

  onMunicipalityChange(municipalitySlug: string | null): void {
    this.selectedMunicipalitySlug = municipalitySlug;
    this.municipalityChange.emit(municipalitySlug);
  }

  private loadMunicipalities(provinceSlug: string): void {
    this.locationService.getMunicipalities(provinceSlug).subscribe({
      next: (municipalities) => (this.municipalities = municipalities),
      error: (err) => this.loadError.set(apiErrorMessage(err, 'Não foi possível carregar os municípios.'))
    });
  }
}
