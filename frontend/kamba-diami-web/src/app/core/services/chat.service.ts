import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';
import { ConversationSummary, Message } from '../models/chat.model';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private hubConnection: signalR.HubConnection | null = null;

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
    await this.connect();
    await this.hubConnection?.invoke('JoinConversation', conversationId);
    await this.hubConnection?.invoke('MarkAsRead', conversationId);
  }

  async sendMessage(conversationId: number, content: string): Promise<void> {
    await this.hubConnection?.invoke('SendMessage', conversationId, content);
  }

  async disconnect(): Promise<void> {
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
      this.messages.update((list) => [...list, message]);
    });

    await this.hubConnection.start();
    this.connected.set(true);
  }
}
