import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { JobPosting } from '../../../core/models/job-posting.model';
import { AuthService } from '../../../core/services/auth.service';
import { SavedPostingsService } from '../../../core/services/saved-postings.service';
import { KwanzaCurrencyPipe } from '../../pipes/kwanza-currency.pipe';

@Component({
  selector: 'app-job-posting-card',
  standalone: true,
  imports: [CommonModule, RouterLink, KwanzaCurrencyPipe],
  templateUrl: './job-posting-card.component.html',
  styleUrl: './job-posting-card.component.scss'
})
export class JobPostingCardComponent {
  @Input({ required: true }) posting!: JobPosting;
  @Input() showDeleteButton = false;
  @Input() useModalOutput = false;
  @Output() deleteRequested = new EventEmitter<number>();
  @Output() viewDetails = new EventEmitter<void>();

  constructor(
    public savedPostings: SavedPostingsService,
    private authService: AuthService
  ) {}

  /** Mensagem curta no botão "Partilhar" depois de copiar a ligação. */
  readonly shareFeedback = signal<string | null>(null);

  get canSave(): boolean {
    return this.posting.authorUserId !== this.authService.currentUser()?.id;
  }

  get isMine(): boolean {
    return !this.canSave;
  }

  get initials(): string {
    const parts = this.posting.authorName.trim().split(/\s+/);
    const first = parts[0]?.charAt(0) ?? '';
    const last = parts.length > 1 ? parts[parts.length - 1].charAt(0) : '';
    return `${first}${last}`.toUpperCase() || '?';
  }

  /** Província do primeiro local, para a linha por baixo do nome. */
  get mainPlace(): string {
    return this.posting.locations[0]?.provinceName ?? 'Angola';
  }

  get scheduleLabel(): string {
    if (this.posting.hoursPerDay) {
      return `${this.posting.hoursPerDay}h por dia`;
    }
    return this.posting.paymentType === 'Mensal' ? 'Horário a combinar' : 'Por dia de trabalho';
  }

  /** Texto do botão principal, conforme quem vê o anúncio. */
  get ctaLabel(): string {
    if (this.isMine) return 'Ver detalhes';
    const role = this.authService.currentUser()?.role;
    if (role === 'Secretaria' && this.posting.type === 'OfertaEmprego') return 'Candidatar-me';
    if (role === 'Familia' && this.posting.type === 'ProcuraEmprego') return 'Contactar';
    return 'Ver detalhes';
  }

  async share(event: Event): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    const url = `${window.location.origin}/job-postings/${this.posting.id}`;
    try {
      if (navigator.share) {
        await navigator.share({ title: `${this.title} · Kamba Diami`, url });
        return;
      }
      await navigator.clipboard.writeText(url);
      this.shareFeedback.set('Ligação copiada');
      setTimeout(() => this.shareFeedback.set(null), 2000);
    } catch {
      // A pessoa cancelou a partilha ou o navegador não deixou copiar: não há mais nada a fazer.
    }
  }

  get locationsSummary(): string {
    const all = [...new Set(this.posting.locations.map((l) => l.municipalityName))];
    const names = all.slice(0, 3);
    const extra = all.length - names.length;
    return names.join(', ') + (extra > 0 ? ` +${extra}` : '');
  }

  /** Título a partir das categorias, por exemplo "Procura-se: Limpeza e Cozinha". */
  get title(): string {
    const names = this.posting.categories.map((c) => c.name);
    const list =
      names.length === 0
        ? 'Serviço doméstico'
        : names.length === 1
          ? names[0]
          : `${names.slice(0, -1).join(', ')} e ${names[names.length - 1]}`;
    return this.posting.type === 'OfertaEmprego' ? `Procura-se: ${list}` : `Disponível para: ${list}`;
  }

  get statusLabel(): string {
    switch (this.posting.status) {
      case 'Preenchido':
        return 'Preenchido';
      case 'Pausado':
        return 'Pausado';
      case 'Removido':
        return 'Removido';
      default:
        return this.posting.status;
    }
  }

  get postedAgo(): string {
    const published = new Date(this.posting.createdAt);
    const diffMs = Date.now() - published.getTime();
    const minutes = Math.floor(diffMs / 60000);
    if (minutes < 1) return 'agora mesmo';
    if (minutes < 60) return `há ${minutes} min`;

    const hours = Math.floor(minutes / 60);
    if (hours < 24) return hours === 1 ? 'há 1 hora' : `há ${hours} horas`;

    const days = Math.floor(hours / 24);
    if (days <= 5) return days === 1 ? 'há 1 dia' : `há ${days} dias`;

    return published.toLocaleDateString('pt-PT', {
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    });
  }

  toggleSave(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.savedPostings.toggle(this.posting.id);
  }
}
