import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { JobPostingsService } from '../../../core/services/job-postings.service';
import { MatchesService } from '../../../core/services/matches.service';
import { SavedPostingsService } from '../../../core/services/saved-postings.service';
import { JobPosting } from '../../../core/models/job-posting.model';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { KwanzaCurrencyPipe } from '../../../shared/pipes/kwanza-currency.pipe';

const STATUS_LABELS: Record<string, string> = {
  Pausado: 'Pausado',
  Preenchido: 'Já preenchido',
  Removido: 'Removido'
};

@Component({
  selector: 'app-posting-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, KwanzaCurrencyPipe],
  templateUrl: './posting-detail.component.html',
  styleUrl: './posting-detail.component.scss'
})
export class PostingDetailComponent implements OnInit {
  posting = signal<JobPosting | null>(null);
  notFound = signal(false);
  contacting = signal(false);
  contactError = signal<string | null>(null);

  readonly isOffer = computed(() => this.posting()?.type === 'OfertaEmprego');
  readonly isAdmin = computed(() => this.authService.currentUser()?.role === 'Admin');

  readonly title = computed(() => {
    const names = this.posting()?.categories.map((c) => c.name) ?? [];
    if (names.length === 0) return 'Serviço doméstico';
    if (names.length === 1) return names[0];
    return `${names.slice(0, -1).join(', ')} e ${names[names.length - 1]}`;
  });

  /** "Município, Província", sem repetições (anúncios antigos podiam ter vários bairros no mesmo município). */
  readonly placeLabels = computed(() => [
    ...new Set((this.posting()?.locations ?? []).map((l) => `${l.municipalityName}, ${l.provinceName}`))
  ]);
  readonly authorRoleLabel = computed(() => {
    const role = this.posting()?.authorRole;
    return role === 'Familia' ? 'Família' : role === 'Secretaria' ? 'Secretária do lar' : 'Administrador';
  });

  readonly statusLabel = computed(() => {
    const status = this.posting()?.status;
    return status && status !== 'Ativo' ? STATUS_LABELS[status] ?? status : null;
  });

  /** O botão de interesse só faz sentido para quem está do outro lado do anúncio. */
  readonly canContact = computed(() => {
    const posting = this.posting();
    const role = this.authService.currentUser()?.role;
    if (!posting || this.isOwnPosting || posting.status !== 'Ativo') return false;
    return posting.type === 'OfertaEmprego' ? role === 'Secretaria' : role === 'Familia';
  });

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private jobPostingsService: JobPostingsService,
    private matchesService: MatchesService,
    public authService: AuthService,
    public savedPostings: SavedPostingsService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.jobPostingsService.getById(id).subscribe({
      next: (posting) => this.posting.set(posting),
      error: () => this.notFound.set(true)
    });
  }

  get isOwnPosting(): boolean {
    return this.posting()?.authorUserId === this.authService.currentUser()?.id;
  }

  get backLink(): { url: string; label: string; query?: Record<string, string> } {
    if (this.isAdmin()) return { url: '/admin', label: 'Voltar aos anúncios', query: { tab: 'postings' } };
    if (this.isOwnPosting) return { url: '/job-postings/mine', label: 'Voltar aos meus anúncios' };
    return { url: '/feed', label: 'Voltar ao feed' };
  }

  initials(name: string): string {
    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0])
      .join('')
      .toUpperCase();
  }

  timeAgo(iso: string): string {
    const days = Math.floor((Date.now() - new Date(iso).getTime()) / 86_400_000);
    if (days <= 0) return 'hoje';
    if (days === 1) return 'ontem';
    if (days < 30) return `há ${days} dias`;
    const months = Math.floor(days / 30);
    return months === 1 ? 'há 1 mês' : `há ${months} meses`;
  }

  contact(): void {
    const posting = this.posting();
    if (!posting || this.contacting()) return;

    this.contacting.set(true);
    this.contactError.set(null);

    this.matchesService.create({ jobPostingId: posting.id }).subscribe({
      next: (match) => {
        this.contacting.set(false);
        if (match.conversationId) {
          this.router.navigate(['/messages', match.conversationId]);
        }
      },
      error: (err) => {
        this.contacting.set(false);
        this.contactError.set(apiErrorMessage(err, 'Não foi possível iniciar a conversa.'));
      }
    });
  }
}
