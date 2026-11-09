import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { JobPostingsService } from '../../core/services/job-postings.service';
import { LocationService } from '../../core/services/location.service';
import { JobPosting, JobPostingType, PaymentType } from '../../core/models/job-posting.model';
import { Province } from '../../core/models/location.model';
import { JobPostingCardComponent } from '../../shared/components/job-posting-card/job-posting-card.component';
import { KwanzaCurrencyPipe } from '../../shared/pipes/kwanza-currency.pipe';

@Component({
  selector: 'app-feed',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, JobPostingCardComponent, KwanzaCurrencyPipe],
  templateUrl: './feed.component.html',
  styleUrl: './feed.component.scss'
})
export class FeedComponent implements OnInit {
  postings: JobPosting[] = [];
  provinces: Province[] = [];
  loading = signal(false);
  selectedPosting = signal<JobPosting | null>(null);

  searchQuery = '';
  typeFilter: JobPostingType | '' = '';
  paymentTypeFilter: PaymentType | '' = '';
  provinceSlug: string | '' = '';

  totalCount = 0;
  page = 1;
  pageSize = 12;

  constructor(
    public authService: AuthService,
    private jobPostingsService: JobPostingsService,
    private locationService: LocationService
  ) {}

  ngOnInit(): void {
    const role = this.authService.currentUser()?.role;
    this.typeFilter = role === 'Familia' ? 'ProcuraEmprego' : role === 'Secretaria' ? 'OfertaEmprego' : '';

    this.locationService.getProvinces().subscribe((provinces) => (this.provinces = provinces));
    this.loadFeed();
  }

  get heading(): string {
    if (this.typeFilter === 'ProcuraEmprego') return 'Secretárias disponíveis';
    if (this.typeFilter === 'OfertaEmprego') return 'Vagas para ti';
    return 'Anúncios recentes';
  }

  loadFeed(): void {
    this.loading.set(true);
    this.jobPostingsService
      .getFeed({
        type: this.typeFilter || undefined,
        paymentType: this.paymentTypeFilter || undefined,
        provinceSlug: this.provinceSlug || undefined,
        search: this.searchQuery.trim() || undefined,
        page: this.page,
        pageSize: this.pageSize
      })
      .subscribe({
        next: (result) => {
          this.postings = result.items;
          this.totalCount = result.totalCount;
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadFeed();
  }

  onSearchSubmit(event: Event): void {
    event.preventDefault();
    this.onFilterChange();
  }

  setTypeFilter(type: JobPostingType | ''): void {
    this.typeFilter = type;
    this.onFilterChange();
  }

  setPayment(paymentType: PaymentType | ''): void {
    this.paymentTypeFilter = paymentType;
    this.onFilterChange();
  }

  postedAgo(posting: JobPosting): string {
    const published = new Date(posting.createdAt);
    const diffMs = Date.now() - published.getTime();
    const minutes = Math.floor(diffMs / 60000);
    if (minutes < 1) return 'agora mesmo';
    if (minutes < 60) return `há ${minutes} min`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return hours === 1 ? 'há 1 hora' : `há ${hours} horas`;
    const days = Math.floor(hours / 24);
    if (days <= 5) return days === 1 ? 'há 1 dia' : `há ${days} dias`;
    return published.toLocaleDateString('pt-PT', { day: 'numeric', month: 'long', year: 'numeric' });
  }

  locationsSummary(posting: JobPosting): string {
    const all = [...new Set(posting.locations.map((l) => l.municipalityName))];
    const names = all.slice(0, 3);
    const extra = all.length - names.length;
    return names.join(', ') + (extra > 0 ? ` +${extra}` : '');
  }

  openDetails(posting: JobPosting): void {
    this.selectedPosting.set(posting);
  }

  closeDetails(): void {
    this.selectedPosting.set(null);
  }

  nextPage(): void {
    if (this.page * this.pageSize >= this.totalCount) return;
    this.page++;
    this.loadFeed();
  }

  prevPage(): void {
    if (this.page <= 1) return;
    this.page--;
    this.loadFeed();
  }
}
