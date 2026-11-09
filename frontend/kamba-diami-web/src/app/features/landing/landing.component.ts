import { AfterViewInit, Component, ElementRef, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { JobPosting, JobPostingType } from '../../core/models/job-posting.model';
import { UserRole } from '../../core/models/user.model';
import { JobPostingsService } from '../../core/services/job-postings.service';
import { scrollToAnchor } from '../../core/utils/scroll-to-anchor';
import { KwanzaCurrencyPipe } from '../../shared/pipes/kwanza-currency.pipe';

/** A página inicial mostra só uma amostra dos anúncios mais recentes. */
const LANDING_SAMPLE_SIZE = 3;

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, KwanzaCurrencyPipe],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent implements OnInit, AfterViewInit {
  readonly currentYear = new Date().getFullYear();
  
  marketMode: 'secretarias' | 'ofertas' = 'secretarias';
  listings: JobPosting[] = [];
  listingsLoading = false;
  listingsError: string | null = null;

  constructor(
    private readonly host: ElementRef<HTMLElement>,
    private readonly router: Router,
    private readonly jobPostingsService: JobPostingsService
  ) {}

  get registerRole(): UserRole {
    return this.marketMode === 'secretarias' ? 'Familia' : 'Secretaria';
  }

  ngOnInit(): void {
    this.loadListings();
  }

  ngAfterViewInit(): void {
    const revealElements = this.host.nativeElement.querySelectorAll<HTMLElement>('.l-reveal');
    const prefersReducedMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;

    if (prefersReducedMotion || !('IntersectionObserver' in window)) {
      revealElements.forEach((el) => el.classList.add('l-revealed'));
    } else {
      const observer = new IntersectionObserver(
        (entries) => {
          for (const entry of entries) {
            if (entry.isIntersecting) {
              entry.target.classList.add('l-revealed');
              observer.unobserve(entry.target);
            }
          }
        },
        { threshold: 0.15, rootMargin: '0px 0px -60px 0px' }
      );

      revealElements.forEach((el) => observer.observe(el));
    }

    const fragment = this.router.parseUrl(this.router.url).fragment;
    if (fragment) {
      setTimeout(() => scrollToAnchor(fragment), 0);
    }
  }

  scrollToFragment(event: Event, fragmentId: string): void {
    event.preventDefault();
    scrollToAnchor(fragmentId);
  }

  setMarketMode(mode: 'secretarias' | 'ofertas'): void {
    if (this.marketMode === mode) {
      return;
    }

    this.marketMode = mode;
    this.loadListings();
  }

  headline(posting: JobPosting): string {
    if (posting.categories.length === 0) {
      return 'Serviço doméstico';
    }

    return posting.categories.map((category) => category.name).join(', ');
  }

  locationLabel(posting: JobPosting): string {
    const location = posting.locations[0];
    if (!location) {
      return 'Angola';
    }

    return `${location.neighborhoodName}, ${location.municipalityName}`;
  }

  snippet(posting: JobPosting): string {
    const text = posting.comments?.trim();
    if (!text) {
      return 'Cria conta para ler o anúncio completo e falar com esta pessoa.';
    }

    return text.length > 160 ? `${text.slice(0, 157).trimEnd()}...` : text;
  }

  initials(name: string): string {
    return name
      .split(' ')
      .filter((part) => part.length > 0)
      .slice(0, 2)
      .map((part) => part[0])
      .join('')
      .toUpperCase();
  }

  private loadListings(): void {
    const type: JobPostingType = this.marketMode === 'secretarias' ? 'ProcuraEmprego' : 'OfertaEmprego';
    this.listingsLoading = true;
    this.listingsError = null;

    this.jobPostingsService.getPreview(type, LANDING_SAMPLE_SIZE).subscribe({
      next: (page) => {
        this.listings = page.items;
        this.listingsLoading = false;
      },
      error: () => {
        this.listings = [];
        this.listingsLoading = false;
        this.listingsError = 'Não foi possível carregar os anúncios neste momento.';
      }
    });
  }
}
