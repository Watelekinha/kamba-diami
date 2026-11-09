import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, of, switchMap } from 'rxjs';
import { Contract, ContractStatus } from '../../../core/models/contract.model';
import { AuthService } from '../../../core/services/auth.service';
import { ContractsService } from '../../../core/services/contracts.service';
import { KwanzaCurrencyPipe } from '../../../shared/pipes/kwanza-currency.pipe';

interface ClauseDraft {
  key: string;
  title: string;
  body: string;
}

@Component({
  selector: 'app-contract-workspace',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, KwanzaCurrencyPipe],
  templateUrl: './contract-workspace.component.html',
  styleUrl: './contract-workspace.component.scss'
})
export class ContractWorkspaceComponent implements OnInit {
  contract = signal<Contract | null>(null);
  clauses: ClauseDraft[] = [];
  loading = signal(true);
  saving = signal(false);
  generating = signal(false);
  uploading = signal(false);
  openingSigned = signal<string | null>(null);
  confirmingEnd = signal(false);
  waitingForFamilia = signal(false);
  cancelOpen = signal(false);
  cancelling = signal(false);
  cancelReason = '';
  ratingScore = 0;
  ratingComment = '';
  submittingRating = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  constructor(
    private route: ActivatedRoute,
    public auth: AuthService,
    private contracts: ContractsService
  ) {}

  ngOnInit(): void {
    const matchId = Number(this.route.snapshot.paramMap.get('matchId'));
    this.contracts
      .getByMatch(matchId)
      .pipe(
        catchError((err: HttpErrorResponse) => {
          if (err.status === 404) return of(null);
          throw err;
        }),
        // Só a família cria o contrato; a secretária do lar espera que ele exista.
        switchMap((existing) =>
          existing || !this.isFamiliaUser ? of(existing) : this.contracts.create({ matchId })
        )
      )
      .subscribe({
        next: (contract) => {
          if (contract) {
            this.apply(contract);
          } else {
            this.waitingForFamilia.set(true);
          }
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.loading.set(false);
          this.error.set(this.readError(err, 'Não foi possível abrir o contrato.'));
        }
      });
  }

  get isFamiliaUser(): boolean {
    return this.auth.currentUser()?.role === 'Familia';
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

  paymentLabel(contract: Contract): string {
    const period = contract.paymentType === 'Mensal' ? 'por mês' : 'por dia';
    return `${contract.paymentValue.toLocaleString('pt-PT')} Kzs ${period}`;
  }

  addClause(): void {
    this.clauses.push({ key: `nova-${Date.now()}-${this.clauses.length}`, title: '', body: '' });
  }

  removeClause(index: number): void {
    this.clauses.splice(index, 1);
  }

  saveClauses(): void {
    const contract = this.contract();
    if (!contract) return;
    if (this.clauses.length === 0) {
      this.error.set('O contrato deve ter pelo menos uma cláusula.');
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    this.contracts
      .updateClauses(contract.id, {
        clauses: this.clauses.map((c) => ({ title: c.title.trim(), body: c.body.trim() }))
      })
      .subscribe({
        next: (updated) => {
          this.apply(updated);
          this.saving.set(false);
          this.success.set('Cláusulas guardadas. Podes gerar o PDF com a marca Kamba Diami.');
        },
        error: (err: HttpErrorResponse) => {
          this.saving.set(false);
          this.error.set(this.readError(err, 'Não foi possível guardar as cláusulas.'));
        }
      });
  }

  generateAndDownload(): void {
    const contract = this.contract();
    if (!contract) return;
    if (this.clauses.length === 0) {
      this.error.set('O contrato deve ter pelo menos uma cláusula.');
      return;
    }

    this.generating.set(true);
    this.error.set(null);

    const generate = (id: number) => {
      this.contracts.generatePdf(id).subscribe({
        next: (blob) => {
          this.contracts.saveBlob(blob, `contrato-kamba-diami-KD-${String(id).padStart(6, '0')}.pdf`);
          this.contracts.getById(id).subscribe((updated) => {
            this.apply(updated);
            this.generating.set(false);
            this.success.set('PDF gerado. Imprime ou assina digitalmente e depois carrega o ficheiro assinado.');
          });
        },
        error: (err: HttpErrorResponse) => {
          this.generating.set(false);
          this.error.set(this.readError(err, 'Não foi possível gerar o PDF.'));
        }
      });
    };

    if (!contract.canEdit) {
      generate(contract.id);
      return;
    }

    this.contracts
      .updateClauses(contract.id, {
        clauses: this.clauses.map((c) => ({ title: c.title.trim(), body: c.body.trim() }))
      })
      .subscribe({
        next: (updated) => {
          this.apply(updated);
          generate(updated.id);
        },
        error: (err: HttpErrorResponse) => {
          this.generating.set(false);
          this.error.set(this.readError(err, 'Não foi possível guardar as cláusulas antes de gerar o PDF.'));
        }
      });
  }

  downloadExisting(): void {
    const contract = this.contract();
    if (!contract) return;
    this.contracts.downloadPdf(contract.id).subscribe({
      next: (blob) =>
        this.contracts.saveBlob(blob, `contrato-kamba-diami-KD-${String(contract.id).padStart(6, '0')}.pdf`),
      error: (err: HttpErrorResponse) =>
        this.error.set(this.readError(err, 'Não foi possível descarregar o PDF.'))
    });
  }

  viewSigned(party: 'familia' | 'secretaria'): void {
    const contract = this.contract();
    if (!contract) return;
    this.openingSigned.set(party);
    this.error.set(null);
    this.contracts.downloadSigned(contract.id, party).subscribe({
      next: (blob) => {
        this.openingSigned.set(null);
        this.contracts.openBlob(blob);
      },
      error: (err: HttpErrorResponse) => {
        this.openingSigned.set(null);
        this.error.set(this.readError(err, 'Não foi possível abrir o documento assinado.'));
      }
    });
  }

  downloadSigned(party: 'familia' | 'secretaria'): void {
    const contract = this.contract();
    if (!contract) return;
    const who = party === 'secretaria' ? 'secretaria' : 'familia';
    this.contracts.downloadSigned(contract.id, party).subscribe({
      next: (blob) =>
        this.contracts.saveBlob(
          blob,
          `contrato-kamba-diami-KD-${String(contract.id).padStart(6, '0')}-assinado-${who}.pdf`
        ),
      error: (err: HttpErrorResponse) =>
        this.error.set(this.readError(err, 'Não foi possível descarregar o documento assinado.'))
    });
  }

  downloadSignedByBoth(): void {
    const contract = this.contract();
    if (!contract) return;
    this.contracts.downloadSignedByBoth(contract.id).subscribe({
      next: (blob) =>
        this.contracts.saveBlob(
          blob,
          `contrato-kamba-diami-KD-${String(contract.id).padStart(6, '0')}-assinado-pelas-partes.pdf`
        ),
      error: (err: HttpErrorResponse) =>
        this.error.set(this.readError(err, 'Não foi possível descarregar os contratos assinados.'))
    });
  }

  starLabel(score: number): string {
    return '★'.repeat(score) + '☆'.repeat(5 - score);
  }

  cancelContract(): void {
    const contract = this.contract();
    if (!contract) return;
    const reason = this.cancelReason.trim();
    if (reason.length < 5) {
      this.error.set('Indica o motivo do cancelamento (pelo menos 5 caracteres).');
      return;
    }
    this.cancelling.set(true);
    this.error.set(null);
    this.success.set(null);
    this.contracts.cancel(contract.id, reason).subscribe({
      next: (updated) => {
        this.apply(updated);
        this.cancelling.set(false);
        this.cancelOpen.set(false);
        this.cancelReason = '';
        this.success.set('Contrato cancelado. A outra parte foi avisada na conversa e o anúncio voltou ao mural.');
      },
      error: (err: HttpErrorResponse) => {
        this.cancelling.set(false);
        this.error.set(this.readError(err, 'Não foi possível cancelar o contrato.'));
      }
    });
  }

  /** Pré-visualização dos períodos de pagamento enquanto o registo de pagamentos (HU21) não existe. */
  paymentPreview(contract: Contract): string[] {
    if (contract.paymentType === 'Diaria') {
      return ['1.º dia de trabalho', '2.º dia de trabalho', '3.º dia de trabalho'];
    }
    const months = ['Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho', 'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'];
    const start = new Date();
    return [0, 1, 2].map((i) => {
      const d = new Date(start.getFullYear(), start.getMonth() + i, 1);
      return `${months[d.getMonth()]} de ${d.getFullYear()}`;
    });
  }

  confirmEnd(): void {
    const contract = this.contract();
    if (!contract) return;
    this.confirmingEnd.set(true);
    this.error.set(null);
    this.contracts.confirmEnd(contract.id).subscribe({
      next: (updated) => {
        this.apply(updated);
        this.confirmingEnd.set(false);
        this.success.set(
          updated.status === 'Concluido'
            ? 'Ambas as partes confirmaram o fim do trabalho. O contrato está concluído.'
            : 'Fim do trabalho confirmado. Falta a confirmação da outra parte.'
        );
      },
      error: (err: HttpErrorResponse) => {
        this.confirmingEnd.set(false);
        this.error.set(this.readError(err, 'Não foi possível confirmar o fim do trabalho.'));
      }
    });
  }

  submitRating(): void {
    const contract = this.contract();
    if (!contract) return;
    if (this.ratingScore < 1 || this.ratingScore > 5) {
      this.error.set('Selecciona uma classificação de 1 a 5 estrelas.');
      return;
    }
    this.submittingRating.set(true);
    this.error.set(null);
    this.contracts.rate(contract.id, this.ratingScore, this.ratingComment.trim() || null).subscribe({
      next: (updated) => {
        this.apply(updated);
        this.submittingRating.set(false);
        this.success.set(`Avaliação de ${this.ratingScore} estrelas enviada.`);
        this.ratingScore = 0;
        this.ratingComment = '';
      },
      error: (err: HttpErrorResponse) => {
        this.submittingRating.set(false);
        this.error.set(this.readError(err, 'Não foi possível enviar a avaliação.'));
      }
    });
  }

  onSignedSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    const contract = this.contract();
    if (!file || !contract) return;

    this.uploading.set(true);
    this.error.set(null);
    this.contracts.uploadSigned(contract.id, file).subscribe({
      next: (updated) => {
        this.apply(updated);
        this.uploading.set(false);
        this.success.set(
          updated.status === 'Ativo'
            ? 'Ambas as partes carregaram o contrato assinado. O contrato está activo.'
            : 'Contrato assinado carregado. Falta a assinatura da outra parte.'
        );
      },
      error: (err: HttpErrorResponse) => {
        this.uploading.set(false);
        this.error.set(this.readError(err, 'Não foi possível carregar o contrato assinado.'));
      }
    });
  }

  private apply(contract: Contract): void {
    this.contract.set(contract);
    this.clauses = contract.clauses.map((c) => ({
      key: String(c.id),
      title: c.title,
      body: c.body
    }));
  }

  private readError(err: HttpErrorResponse, fallback: string): string {
    if (err.status === 0 || err.status === 504 || err.status === 408) {
      return 'O servidor demorou demasiado a responder. Tenta outra vez.';
    }
    const body = err.error;
    if (body instanceof Blob) {
      return fallback;
    }
    if (typeof body?.message === 'string') return body.message;
    if (body?.errors) {
      const first = Object.values(body.errors).flat()[0];
      if (typeof first === 'string') return first;
    }
    return fallback;
  }
}
