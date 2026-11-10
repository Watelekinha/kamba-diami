import { CommonModule } from '@angular/common';
import { Component, HostListener, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { JobPostingsService } from '../../../core/services/job-postings.service';
import { LocationService } from '../../../core/services/location.service';
import { ServiceCategory } from '../../../core/models/location.model';
import { PaymentType } from '../../../core/models/job-posting.model';
import { MultiLocationPickerComponent } from '../../../shared/components/multi-location-picker/multi-location-picker.component';
import { KwanzaCurrencyPipe } from '../../../shared/pipes/kwanza-currency.pipe';
import { categoryIcon } from '../../../shared/utils/category-icons';

const MINIMUM_MONTHLY_VALUE = 50000;
const COMMENTS_MAX_LENGTH = 500;

@Component({
  selector: 'app-create-posting',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, MultiLocationPickerComponent, KwanzaCurrencyPipe],
  templateUrl: './create-posting.component.html',
  styleUrl: './create-posting.component.scss'
})
export class CreatePostingComponent implements OnInit {
  readonly minimumMonthlyValue = MINIMUM_MONTHLY_VALUE;
  readonly commentsMaxLength = COMMENTS_MAX_LENGTH;

  categories: ServiceCategory[] = [];
  selectedCategoryIds = new Set<number>();
  customCategoryName = '';

  paymentType: PaymentType = 'Diaria';
  paymentValue: number | null = null;
  hoursPerDay: number | null = null;
  comments = '';
  municipalitySlugs: string[] = [];
  locationLabels: string[] = [];

  submitting = signal(false);
  errorMessage = signal<string | null>(null);

  constructor(
    public authService: AuthService,
    private locationService: LocationService,
    private jobPostingsService: JobPostingsService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.locationService.getServiceCategories().subscribe((categories) => (this.categories = categories));
  }

  get isFamilia(): boolean {
    return this.authService.currentUser()?.role === 'Familia';
  }

  get otherCategory(): ServiceCategory | undefined {
    return this.categories.find((c) => c.name.toLowerCase() === 'outro');
  }

  get isOtherCategorySelected(): boolean {
    const other = this.otherCategory;
    return !!other && this.selectedCategoryIds.has(other.id);
  }

  get selectedCategoryNames(): string[] {
    return this.categories
      .filter((c) => this.selectedCategoryIds.has(c.id) && c.id !== this.otherCategory?.id)
      .map((c) => c.name)
      .concat(this.isOtherCategorySelected && this.customCategoryName.trim() ? [this.customCategoryName.trim()] : []);
  }

  get isBelowMonthlyMinimum(): boolean {
    return this.paymentType === 'Mensal' && !!this.paymentValue && this.paymentValue < MINIMUM_MONTHLY_VALUE;
  }

  get servicesDone(): boolean {
    return this.selectedCategoryNames.length > 0 && (!this.isOtherCategorySelected || !!this.customCategoryName.trim());
  }

  get conditionsDone(): boolean {
    return !!this.paymentValue && this.paymentValue > 0 && !this.isBelowMonthlyMinimum;
  }

  get locationsDone(): boolean {
    return this.municipalitySlugs.length > 0;
  }

  get completedSteps(): number {
    return [this.servicesDone, this.conditionsDone, this.locationsDone].filter(Boolean).length;
  }

  iconFor(name: string): string[] {
    return categoryIcon(name);
  }

  /** Lista de serviços em dropdown (escolha múltipla). */
  readonly servicesOpen = signal(false);

  get selectedCategories(): ServiceCategory[] {
    return this.categories.filter((c) => this.selectedCategoryIds.has(c.id));
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.servicesOpen() && !(event.target as Element).closest?.('.services-select')) {
      this.servicesOpen.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.servicesOpen.set(false);
  }

  toggleCategory(id: number): void {
    if (this.selectedCategoryIds.has(id)) {
      this.selectedCategoryIds.delete(id);
    } else {
      this.selectedCategoryIds.add(id);
    }
  }

  onLocationsChange(municipalitySlugs: string[]): void {
    this.municipalitySlugs = municipalitySlugs;
  }

  onLocationLabelsChange(labels: string[]): void {
    this.locationLabels = labels;
  }

  submit(): void {
    this.errorMessage.set(null);

    if (this.selectedCategoryIds.size === 0) {
      this.errorMessage.set('Selecciona pelo menos uma área/categoria.');
      return;
    }
    if (this.isOtherCategorySelected && !this.customCategoryName.trim()) {
      this.errorMessage.set('Indica o nome da categoria em "Outro".');
      return;
    }
    if (!this.paymentValue || this.paymentValue <= 0) {
      this.errorMessage.set('Indica um valor de pagamento válido.');
      return;
    }
    if (this.isBelowMonthlyMinimum) {
      this.errorMessage.set('Para contratos mensais, o valor mínimo é 50.000 Kzs.');
      return;
    }
    if (this.municipalitySlugs.length === 0) {
      this.errorMessage.set('Adiciona pelo menos um município.');
      return;
    }

    this.submitting.set(true);

    if (this.isOtherCategorySelected) {
      this.locationService
        .createServiceCategory(this.customCategoryName.trim())
        .pipe(switchMap((category) => this.createPosting(this.buildCategoryIds(category.id))))
        .subscribe({
          next: (posting) => this.onSuccess(posting.id),
          error: (err) => this.onError(err)
        });
    } else {
      this.createPosting(Array.from(this.selectedCategoryIds)).subscribe({
        next: (posting) => this.onSuccess(posting.id),
        error: (err) => this.onError(err)
      });
    }
  }

  private buildCategoryIds(customCategoryId: number): number[] {
    const otherId = this.otherCategory!.id;
    return Array.from(this.selectedCategoryIds)
      .filter((id) => id !== otherId)
      .concat(customCategoryId);
  }

  private createPosting(categoryIds: number[]) {
    return this.jobPostingsService.create({
      categoryIds,
      paymentType: this.paymentType,
      paymentValue: this.paymentValue!,
      hoursPerDay: this.hoursPerDay,
      comments: this.comments.trim() || null,
      municipalitySlugs: this.municipalitySlugs
    });
  }

  private onSuccess(postingId: number): void {
    this.submitting.set(false);
    this.router.navigate(['/job-postings', postingId]);
  }

  private onError(err: { error?: { message?: string } }): void {
    this.submitting.set(false);
    this.errorMessage.set(err?.error?.message ?? 'Não foi possível publicar. Verifica os dados.');
  }
}
