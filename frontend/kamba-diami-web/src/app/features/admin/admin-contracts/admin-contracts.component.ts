import { CommonModule } from '@angular/common';
import { Component, EventEmitter, OnDestroy, OnInit, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime } from 'rxjs';
import { AdminContractFilter, AdminContractListItem } from '../../../core/models/admin.model';
import { ContractStatus } from '../../../core/models/contract.model';
import { AdminService } from '../../../core/services/admin.service';
import { apiErrorMessage } from '../../../core/utils/api-error';
import { KwanzaCurrencyPipe } from '../../../shared/pipes/kwanza-currency.pipe';
import { CONTRACT_STATUS_LABELS, contractReference, label } from '../admin-labels';

const PAGE_SIZE = 20;

/**
 * Só leitura: o administrador acompanha o estado dos contratos, mas os PDFs e as cláusulas
 * continuam acessíveis apenas às duas partes (dados pessoais, Lei n.º 22/11).
 */
@Component({
  selector: 'app-admin-contracts',
  standalone: true,
  imports: [CommonModule, FormsModule, KwanzaCurrencyPipe],
  templateUrl: './admin-contracts.component.html'
})
export class AdminContractsComponent implements OnInit, OnDestroy {
  @Output() openUser = new EventEmitter<string>();

  readonly statusLabels = CONTRACT_STATUS_LABELS;
  readonly statuses = Object.keys(CONTRACT_STATUS_LABELS) as ContractStatus[];
  readonly label = label;
  readonly contractReference = contractReference;

  filter: Required<Pick<AdminContractFilter, 'search' | 'status'>> = { search: '', status: '' };
  readonly page = signal(1);
  readonly items = signal<AdminContractListItem[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

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
      .getContracts({ ...this.filter, search: this.filter.search.trim(), page: this.page(), pageSize: PAGE_SIZE })
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(apiErrorMessage(err, 'Não foi possível carregar os contratos.'));
        }
      });
  }

  statusTone(status: ContractStatus): string {
    switch (status) {
      case 'Ativo':
      case 'Concluido':
        return 'good';
      case 'Cancelado':
        return 'bad';
      case 'AguardandoAssinaturas':
        return 'warn';
      default:
        return 'plain';
    }
  }
}
