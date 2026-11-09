import { Injectable, computed, signal } from '@angular/core';
import { AppNotification } from '../models/notification.model';

/**
 * Ainda não existe um endpoint de notificações no backend, por isso a lista começa vazia
 * em vez de mostrar exemplos inventados. Este serviço é o ponto onde ligar as notificações
 * reais (interesses recebidos, contratos para assinar) quando o endpoint existir.
 */
@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly notificationsSignal = signal<AppNotification[]>([]);

  readonly notifications = this.notificationsSignal.asReadonly();
  readonly unreadCount = computed(() => this.notificationsSignal().filter((n) => !n.read).length);

  markAsRead(id: number): void {
    this.notificationsSignal.update((list) =>
      list.map((n) => (n.id === id ? { ...n, read: true } : n))
    );
  }

  markAllAsRead(): void {
    this.notificationsSignal.update((list) => list.map((n) => ({ ...n, read: true })));
  }
}
