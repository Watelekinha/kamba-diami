import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
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

  get canSave(): boolean {
    return this.posting.authorUserId !== this.authService.currentUser()?.id;
  }

  get locationsSummary(): string {
    const all = [...new Set(this.posting.locations.map((l) => l.municipalityName))];
    const names = all.slice(0, 3);
    const extra = all.length - names.length;
    return names.join(', ') + (extra > 0 ? ` +${extra}` : '');
  }

  get title(): string {
    return this.posting.categories.length > 0 ? this.posting.categories[0].name : 'Serviço doméstico';
  }

  get extraCategories(): { id: number; name: string }[] {
    return this.posting.categories.slice(1);
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
