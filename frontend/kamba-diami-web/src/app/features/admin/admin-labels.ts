import { AdminMetricKey } from '../../core/models/admin.model';

export const METRIC_LABELS: Record<AdminMetricKey, string> = {
  registrations: 'Registos',
  logins: 'Logins',
  activeUsers: 'Utilizadores activos',
  postings: 'Anúncios publicados',
  matches: 'Interesses (matches)',
  contracts: 'Contratos iniciados',
  messages: 'Mensagens trocadas',
  failedLogins: 'Logins falhados'
};

export const METRIC_HINTS: Record<AdminMetricKey, string> = {
  registrations: 'Contas novas de famílias e secretárias do lar',
  logins: 'Entradas com sucesso na plataforma',
  activeUsers: 'Pessoas diferentes que usaram a plataforma',
  postings: 'Vagas e disponibilidades publicadas',
  matches: 'Manifestações de interesse em anúncios',
  contracts: 'Contratos criados a partir de um match',
  messages: 'Mensagens enviadas no chat',
  failedLogins: 'Tentativas com palavra-passe errada, contas bloqueadas ou suspensas'
};

/** Métricas em que subir é mau sinal: a variação aparece a vermelho. */
export const METRICS_WHERE_UP_IS_BAD: ReadonlySet<AdminMetricKey> = new Set(['failedLogins']);

export const ROLE_LABELS: Record<string, string> = {
  Familia: 'Família',
  Secretaria: 'Secretária do lar',
  Admin: 'Administrador'
};

export const ACCOUNT_STATUS_LABELS: Record<string, string> = {
  Ativo: 'Activa',
  Suspenso: 'Suspensa',
  PendenteAprovacao: 'Pendente'
};

export const POSTING_STATUS_LABELS: Record<string, string> = {
  Ativo: 'Activo',
  Pausado: 'Pausado',
  Preenchido: 'Preenchido',
  Removido: 'Removido'
};

export const POSTING_TYPE_LABELS: Record<string, string> = {
  OfertaEmprego: 'Vaga',
  ProcuraEmprego: 'Disponibilidade'
};

export const CONTRACT_STATUS_LABELS: Record<string, string> = {
  Rascunho: 'Rascunho',
  PdfGerado: 'PDF gerado',
  AguardandoAssinaturas: 'A aguardar assinaturas',
  Ativo: 'Activo',
  Concluido: 'Concluído',
  Cancelado: 'Cancelado'
};

export const LOGIN_FAILURE_LABELS: Record<string, string> = {
  UtilizadorInexistente: 'Email sem conta',
  PalavraPasseErrada: 'Palavra-passe errada',
  ContaBloqueada: 'Conta bloqueada',
  ContaSuspensa: 'Conta suspensa'
};

export const AUDIT_ACTION_LABELS: Record<string, string> = {
  SuspenderConta: 'Suspendeu uma conta',
  ReactivarConta: 'Reactivou uma conta',
  DesbloquearConta: 'Desbloqueou uma conta',
  EnviarRecuperacaoPalavraPasse: 'Enviou recuperação de palavra-passe',
  CriarAdministrador: 'Criou um administrador',
  RemoverAnuncio: 'Removeu um anúncio',
  AlterarEstadoAnuncio: 'Alterou o estado de um anúncio'
};

export function label(map: Record<string, string>, key: string | null | undefined): string {
  return key ? (map[key] ?? key) : '';
}

export function contractReference(id: number): string {
  return `KD-${String(id).padStart(6, '0')}`;
}

/** Descreve o browser e o sistema a partir do user agent, para a tabela de logins. */
export function describeUserAgent(userAgent: string | null): string {
  if (!userAgent) return 'Desconhecido';
  const browser = /Edg\//.test(userAgent)
    ? 'Edge'
    : /OPR\//.test(userAgent)
      ? 'Opera'
      : /Chrome\//.test(userAgent)
        ? 'Chrome'
        : /Firefox\//.test(userAgent)
          ? 'Firefox'
          : /Safari\//.test(userAgent)
            ? 'Safari'
            : /node|curl|PostmanRuntime/i.test(userAgent)
              ? 'Script'
              : 'Outro';
  const os = /Android/.test(userAgent)
    ? 'Android'
    : /iPhone|iPad/.test(userAgent)
      ? 'iOS'
      : /Windows/.test(userAgent)
        ? 'Windows'
        : /Mac OS X/.test(userAgent)
          ? 'macOS'
          : /Linux/.test(userAgent)
            ? 'Linux'
            : '';
  return os ? `${browser} · ${os}` : browser;
}

/**
 * Cor de identidade de cada indicador: a mesma no cartão, na mini-linha e no gráfico.
 * É a paleta categórica da skill de dataviz na ordem documentada, validada para cores vizinhas
 * (validate_palette.js, superfície branca). A cor nunca vai para o texto: os nomes ficam a tinta normal.
 */
export const METRIC_COLORS: Record<AdminMetricKey, string> = {
  registrations: '#2a78d6',
  logins: '#eb6834',
  activeUsers: '#1baf7a',
  postings: '#eda100',
  matches: '#e87ba4',
  contracts: '#008300',
  messages: '#6250d6',
  failedLogins: '#e34948'
};

/** Caminhos SVG (viewBox 24) dos ícones dos indicadores. */
export const METRIC_ICONS: Record<AdminMetricKey, string> = {
  registrations: 'M15 19v-1a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v1M9 10.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7ZM19 8v6M22 11h-6',
  logins: 'M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4M10 17l5-5-5-5M15 12H3',
  activeUsers: 'M22 12h-4l-3 8L9 4l-3 8H2',
  postings: 'M6 3.5h9l4 4V20a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1V4.5a1 1 0 0 1 1-1ZM9 12h6M9 16h4',
  matches: 'M12 20s-7-4.4-7-10a4 4 0 0 1 7-2.6A4 4 0 0 1 19 10c0 5.6-7 10-7 10Z',
  contracts: 'M14 3.5H6a1 1 0 0 0-1 1V20a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V8.5l-5-5ZM9 14.5l2 2 4-4',
  messages: 'M4 5.5h16v10H9l-4 3.5v-3.5H4v-10Z',
  failedLogins: 'M12 3 4 6v5c0 5 3.4 8.6 8 10 4.6-1.4 8-5 8-10V6l-8-3ZM12 8.5v4M12 15.5h.01'
};
