import { ProfileRating } from './user.model';

export type ContractStatus =
  | 'Rascunho'
  | 'PdfGerado'
  | 'AguardandoAssinaturas'
  | 'Ativo'
  | 'Concluido'
  | 'Cancelado';

export type PaymentType = 'Diaria' | 'Mensal';

export interface ContractParty {
  userId: string;
  name: string;
  email: string;
  phoneNumber: string;
  address: string;
  role: 'Familia' | 'Secretaria' | 'Admin';
}

export interface ContractClause {
  id: number;
  sortOrder: number;
  title: string;
  body: string;
}

export interface Contract {
  id: number;
  matchId: number;
  conversationId: number;
  jobPostingSummary: string;
  status: ContractStatus;
  paymentType: PaymentType;
  paymentValue: number;
  canEdit: boolean;
  hasGeneratedPdf: boolean;
  familiaSigned: boolean;
  secretariaSigned: boolean;
  currentUserSigned: boolean;
  canRate: boolean;
  canConfirmEnd: boolean;
  endConfirmedByFamilia: boolean;
  endConfirmedBySecretaria: boolean;
  currentUserConfirmedEnd: boolean;
  currentUserIsFamilia: boolean;
  canUploadSigned: boolean;
  canCancel: boolean;
  cancelledAt?: string | null;
  cancelledByName?: string | null;
  cancelledByMe: boolean;
  cancellationReason?: string | null;
  counterpartName: string;
  myRating?: ProfileRating | null;
  createdAt: string;
  updatedAt: string;
  familia: ContractParty;
  secretaria: ContractParty;
  clauses: ContractClause[];
}

export interface CreateContractRequest {
  matchId: number;
}

export interface ContractClauseInput {
  id?: number | null;
  title: string;
  body: string;
}

export interface UpdateContractClausesRequest {
  clauses: ContractClauseInput[];
}

export interface ContractListItem {
  id: number;
  matchId: number;
  conversationId: number;
  jobPostingSummary: string;
  status: ContractStatus;
  paymentType: PaymentType;
  paymentValue: number;
  counterpartName: string;
  counterpartUserId: string;
  canRate: boolean;
  createdAt: string;
  updatedAt: string;
  endDate?: string | null;
}
