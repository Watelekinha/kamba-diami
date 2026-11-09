import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';
import { ConversationSummary, Message } from '../models/chat.model';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private hubConnection: signalR.HubConnection | null = null;
  /** Conversa aberta: é preciso voltar a entrar no grupo depois de cada reconexão. */
  private currentConversationId: number | null = null;

  readonly messages = signal<Message[]>([]);
  readonly connected = signal(false);
  readonly unreadMessagesCount = signal(0);

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  getConversations(): Observable<ConversationSummary[]> {
    return this.http.get<ConversationSummary[]>('/api/conversations/mine');
  }

  refreshUnreadCount(): void {
    this.getConversations().subscribe({
      next: (list) => this.unreadMessagesCount.set(list.reduce((sum, c) => sum + c.unreadCount, 0)),
      error: () => {}
    });
  }

  getMessages(conversationId: number): Observable<Message[]> {
    return this.http.get<Message[]>(`/api/conversations/${conversationId}/messages`);
  }

  async joinConversation(conversationId: number, history: Message[]): Promise<void> {
    this.messages.set(history);
    this.currentConversationId = conversationId;
    await this.connect();
    await this.hubConnection?.invoke('JoinConversation', conversationId);
    await this.hubConnection?.invoke('MarkAsRead', conversationId);
  }

  async sendMessage(conversationId: number, content: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Sem ligação ao chat.');
    }
    await this.hubConnection.invoke('SendMessage', conversationId, content);
  }

  async disconnect(): Promise<void> {
    this.currentConversationId = null;
    if (!this.hubConnection) return;
    await this.hubConnection.stop();
    this.hubConnection = null;
    this.connected.set(false);
    this.messages.set([]);
  }

  private async connect(): Promise<void> {
    if (this.hubConnection) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/chat', { accessTokenFactory: () => this.authService.token ?? '' })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveMessage', (message: Message) => {
      if (message.conversationId !== this.currentConversationId) return;
      this.messages.update((list) => [...list, message]);
      // A conversa está aberta: a mensagem da outra parte fica logo lida.
      if (message.senderUserId !== this.authService.currentUser()?.id) {
        this.hubConnection?.invoke('MarkAsRead', message.conversationId).catch(() => {});
      }
    });

    // O servidor esquece os grupos de uma ligação que caiu; ao reconectar, volta-se a entrar na
    // conversa e recarrega-se o histórico para não perder mensagens enviadas entretanto.
    this.hubConnection.onreconnecting(() => this.connected.set(false));
    this.hubConnection.onreconnected(async () => {
      this.connected.set(true);
      const conversationId = this.currentConversationId;
      if (conversationId === null) return;
      try {
        await this.hubConnection?.invoke('JoinConversation', conversationId);
        this.getMessages(conversationId).subscribe({
          next: (history) => {
            if (this.currentConversationId === conversationId) this.messages.set(history);
          },
          error: () => {}
        });
        await this.hubConnection?.invoke('MarkAsRead', conversationId);
      } catch {
        // A falha volta a aparecer ao tentar enviar; não há mais nada a fazer aqui.
      }
    });
    this.hubConnection.onclose(() => this.connected.set(false));

    await this.hubConnection.start();
    this.connected.set(true);
  }
}
