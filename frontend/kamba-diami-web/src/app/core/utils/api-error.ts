import { HttpErrorResponse } from '@angular/common/http';

/**
 * Extrai uma mensagem legível de um erro da API. O backend responde de duas formas:
 * `{ message }` nos erros de negócio e `{ errors: { campo: [mensagens] } }` nos erros de validação.
 */
export function apiErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  if (error.status === 0) {
    return 'Não foi possível contactar o servidor. Verifica a tua ligação à internet.';
  }

  const body = error.error as { message?: string; errors?: Record<string, string[]> } | null;
  if (body?.message) {
    return body.message;
  }

  const validation = body?.errors ? Object.values(body.errors).flat().filter(Boolean) : [];
  if (validation.length > 0) {
    return [...new Set(validation)].join(' ');
  }

  if (error.status === 429) {
    return 'Demasiadas tentativas seguidas. Aguarda alguns minutos e tenta de novo.';
  }

  return fallback;
}
