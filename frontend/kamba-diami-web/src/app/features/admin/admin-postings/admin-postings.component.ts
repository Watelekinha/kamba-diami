import { CommonModule } from '@angular/common';
import { Component, EventEmitter, OnDestroy, OnInit, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, debounceTime } from 'rxjs';
import { AdminPostingFilter, AdminPostingListItem } from '../../../core/models/admin.model';
import { JobPostingStatus } from '../../../core/models/job-posting.model';
import { AdminService } from '../../../core/services/admin.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { KwanzaCurrencyPipe } from '../../../shared/pipes/kwanza-currency.pipe';
import { POSTING_STATUS_LABELS, POSTING_TYPE_LABELS, label } from '../admin-labels';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-admin-postings',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, KwanzaCurrencyPipe],
  templateUrl: './admin-postings.component.html',
  styleUrl: './admin-postings.component.scss'
})
export class AdminPostingsComponent implements OnInit, OnDestroy {
  @Output() openUser = new EventEmitter<string>();

  readonly statusLabels = POSTING_STATUS_LABELS;
  readonly typeLabels = POSTING_TYPE_LABELS;
  readonly label = label;

  filter: Required<Pick<AdminPostingFilter, 'search' | 'status' | 'type'>> = { search: '', status: '', type: '' };
  readonly page = signal(1);
  readonly items = signal<AdminPostingListItem[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);

  /** Anúncio com o formulário de remoção aberto. */
  readonly removingId = signal<number | null>(null);
  readonly busyId = signal<number | null>(null);
  removeReason = '';

  private readonly search$ = new Subject<void>();

  constructor(private adminService: AdminService) {
    this.search$.pipe(debounceTime(300)).subscribe(() => {
      this.page.set(1);
      this.load();
    });
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount() / PAGE_SIZE));
  }

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.search$.complete();
  }

  onSearchInput(): void {
    this.search$.next();
  }

  onFilterChange(): void {
    this.page.set(1);
    this.load();
  }

  goToPage(page: number): void {
    this.page.set(Math.min(Math.max(page, 1), this.totalPages));
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.adminService
      .getPostings({ ...this.filter, search: this.filter.search.trim(), page: this.page(), pageSize: PAGE_SIZE })
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(apiErrorMessage(err, 'Não foi possível carregar os anúncios.'));
        }
      });
  }

  startRemove(id: number): void {
    this.removeReason = '';
    this.removingId.set(this.removingId() === id ? null : id);
  }

  confirmRemove(posting: AdminPostingListItem): void {
    if (this.removeReason.trim().length < 5) {
      this.error.set('Indica o motivo da remoção (pelo menos 5 caracteres).');
      return;
    }
    this.changeStatus(posting, 'Removido', this.removeReason.trim());
  }

  restore(posting: AdminPostingListItem): void {
    this.changeStatus(posting, 'Ativo');
  }

  statusTone(status: JobPostingStatus): string {
    return status === 'Ativo' ? 'good' : status === 'Removido' ? 'bad' : 'plain';
  }

  private changeStatus(posting: AdminPostingListItem, status: JobPostingStatus, reason?: string): void {
    this.busyId.set(posting.id);
    this.error.set(null);
    this.message.set(null);
    this.adminService.updatePostingStatus(posting.id, status, reason).subscribe({
      next: (response) => {
        this.busyId.set(null);
        this.removingId.set(null);
        this.message.set(response.message);
        this.load();
      },
      error: (err) => {
        this.busyId.set(null);
        this.error.set(apiErrorMessage(err, 'Não foi possível alterar o anúncio.'));
      }
    });
  }
}
