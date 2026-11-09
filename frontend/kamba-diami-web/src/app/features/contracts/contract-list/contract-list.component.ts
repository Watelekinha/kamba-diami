import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ContractListItem, ContractStatus } from '../../../core/models/contract.model';
import { ContractsService } from '../../../core/services/contracts.service';

@Component({
  selector: 'app-contract-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './contract-list.component.html',
  styleUrl: './contract-list.component.scss'
})
export class ContractListComponent implements OnInit {
  contracts = signal<ContractListItem[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  readonly active = computed(() => this.contracts().filter((c) => c.status === 'Ativo'));
  readonly concluded = computed(() => this.contracts().filter((c) => c.status === 'Concluido'));
  readonly inProgress = computed(() =>
    this.contracts().filter((c) =>
      c.status === 'Rascunho' || c.status === 'PdfGerado' || c.status === 'AguardandoAssinaturas'
    )
  );
  readonly cancelled = computed(() => this.contracts().filter((c) => c.status === 'Cancelado'));

  constructor(private contractsService: ContractsService) {}

  ngOnInit(): void {
    this.contractsService.getMine().subscribe({
      next: (items) => {
        this.contracts.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Não foi possível carregar os contratos.');
      }
    });
  }

  statusLabel(status: ContractStatus): string {
    switch (status) {
      case 'Rascunho':
        return 'Rascunho';
      case 'PdfGerado':
        return 'PDF gerado';
      case 'AguardandoAssinaturas':
        return 'À espera de assinaturas';
      case 'Ativo':
        return 'Activo';
      case 'Concluido':
        return 'Concluído';
      case 'Cancelado':
        return 'Cancelado';
    }
  }

  paymentLabel(item: ContractListItem): string {
    const period = item.paymentType === 'Mensal' ? 'por mês' : 'por dia';
    return `${item.paymentValue.toLocaleString('pt-PT')} Kzs ${period}`;
  }

  contractNumber(id: number): string {
    return `KD-${String(id).padStart(6, '0')}`;
  }
}
