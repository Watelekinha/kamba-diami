import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ChatService } from '../../../core/services/chat.service';
import { MatchesService } from '../../../core/services/matches.service';
import { ConversationSummary } from '../../../core/models/chat.model';
import { MATCH_STATUS_LABELS, conversationContext, initials, participantRoleLabel } from '../../../core/utils/conversation-context';

@Component({
  selector: 'app-conversation-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './conversation-detail.component.html',
  styleUrl: './conversation-detail.component.scss'
})
export class ConversationDetailComponent implements OnInit, OnDestroy {
  conversationId!: number;
  conversation = signal<ConversationSummary | null>(null);

  readonly statusLabels = MATCH_STATUS_LABELS;
  readonly context = conversationContext;
  readonly roleLabel = participantRoleLabel;
  readonly initials = initials;
  notFound = signal(false);
  loading = signal(true);
  draft = '';
  sending = signal(false);
  sendError = signal<string | null>(null);
  responding = signal(false);

  @ViewChild('scrollAnchor') scrollAnchor?: ElementRef<HTMLDivElement>;

  constructor(
    private route: ActivatedRoute,
    public authService: AuthService,
    public chatService: ChatService,
    private matchesService: MatchesService
  ) {}

  ngOnInit(): void {
    this.conversationId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadConversationMeta();

    this.chatService.getMessages(this.conversationId).subscribe({
      next: (history) => {
        this.chatService.joinConversation(this.conversationId, history).then(() => {
          this.loading.set(false);
          this.scrollToBottom();
        });
      },
      error: () => {
        this.loading.set(false);
        this.notFound.set(true);
      }
    });
  }

  ngOnDestroy(): void {
    this.chatService.disconnect();
  }

  loadConversationMeta(): void {
    this.chatService.getConversations().subscribe((list) => {
      this.conversation.set(list.find((c) => c.id === this.conversationId) ?? null);
    });
  }

  send(): void {
    const content = this.draft.trim();
    if (!content) return;
    this.sending.set(true);
    this.sendError.set(null);
    this.chatService
      .sendMessage(this.conversationId, content)
      .then(() => {
        this.draft = '';
        this.scrollToBottom();
      })
      .catch(() => {
        // O texto fica no campo para se poder tentar de novo.
        this.sendError.set('Não foi possível enviar a mensagem. Verifica a ligação e tenta de novo.');
      })
      .finally(() => this.sending.set(false));
  }

  respond(accept: boolean): void {
    const conv = this.conversation();
    if (!conv) return;
    this.responding.set(true);
    this.matchesService.respond(conv.matchId, { accept }).subscribe({
      next: (updated) => {
        this.responding.set(false);
        this.conversation.update((c) => (c ? { ...c, matchStatus: updated.status } : c));
      },
      error: () => this.responding.set(false)
    });
  }

  isMine(senderUserId: string): boolean {
    return senderUserId === this.authService.currentUser()?.id;
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      this.scrollAnchor?.nativeElement.scrollIntoView({ behavior: 'smooth' });
    }, 50);
  }
}
