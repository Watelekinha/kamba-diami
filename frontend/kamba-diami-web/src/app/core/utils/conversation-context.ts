import { ConversationSummary, MatchStatus } from '../models/chat.model';

/**
 * Quem manifesta interesse é sempre o iniciador; o anúncio pertence à outra parte.
 * Estas frases dizem, do ponto de vista de quem lê, de quem é o anúncio e quem tomou a iniciativa.
 */
export function conversationContext(c: ConversationSummary): string {
  const kind = c.jobPostingType === 'OfertaEmprego' ? 'oferta de emprego' : 'procura de emprego';
  const firstName = c.counterpart.name.split(' ')[0];
  return c.isInitiatedByMe
    ? `Manifestaste interesse na ${kind} publicada por ${firstName}`
    : `${firstName} manifestou interesse na ${kind} que publicaste`;
}

/** Versão curta para a lista de conversas. */
export function conversationContextShort(c: ConversationSummary): string {
  const kind = c.jobPostingType === 'OfertaEmprego' ? 'Oferta' : 'Procura de emprego';
  const firstName = c.counterpart.name.split(' ')[0];
  return c.isInitiatedByMe
    ? `${kind} publicada por ${firstName}`
    : `${kind} publicada por ti`;
}

export const MATCH_STATUS_LABELS: Record<MatchStatus, string> = {
  Pendente: 'A aguardar resposta',
  Aceite: 'Interesse aceite',
  Recusado: 'Recusado',
  Encerrado: 'Encerrado'
};

export function participantRoleLabel(role: string): string {
  return role === 'Familia' ? 'Família' : role === 'Secretaria' ? 'Secretária do lar' : 'Administrador';
}

export function initials(name: string): string {
  return name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toUpperCase();
}
