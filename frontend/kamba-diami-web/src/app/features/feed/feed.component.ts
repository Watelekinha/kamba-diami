import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { JobPostingsService } from '../../core/services/job-postings.service';
import { LocationService } from '../../core/services/location.service';
import { JobPosting, JobPostingType, PaymentType } from '../../core/models/job-posting.model';
import { Province } from '../../core/models/location.model';
import { JobPostingCardComponent } from '../../shared/components/job-posting-card/job-posting-card.component';

@Component({
  selector: 'app-feed',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, JobPostingCardComponent],
  templateUrl: './feed.component.html',
  styleUrl: './feed.component.scss'
})
export class FeedComponent implements OnInit {
  postings: JobPosting[] = [];
  provinces: Province[] = [];
  loading = signal(false);

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
    private locationService: LocationService,
    private route: ActivatedRoute,
    private destroyRef: DestroyRef
  ) {}

  ngOnInit(): void {
    this.locationService.getProvinces().subscribe((provinces) => (this.provinces = provinces));

    // A pesquisa vem da navbar como ?q=; cada nova pesquisa volta à primeira página.
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.searchQuery = params.get('q')?.trim() ?? '';
      this.page = 1;
      this.loadFeed();
    });
  }

  readonly filtersOpen = signal(false);

  get activeFilterCount(): number {
    return (this.provinceSlug ? 1 : 0) + (this.paymentTypeFilter ? 1 : 0);
  }

  clearFilters(): void {
    this.provinceSlug = '';
    this.paymentTypeFilter = '';
    this.onFilterChange();
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

  setTypeFilter(type: JobPostingType | ''): void {
    this.typeFilter = type;
    this.onFilterChange();
  }

  setPayment(paymentType: PaymentType | ''): void {
    this.paymentTypeFilter = paymentType;
    this.onFilterChange();
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
