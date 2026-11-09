import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ChatService } from '../../../core/services/chat.service';
import { MatchesService } from '../../../core/services/matches.service';
import { ConversationSummary } from '../../../core/models/chat.model';
import { MATCH_STATUS_LABELS, conversationContextShort, initials, participantRoleLabel } from '../../../core/utils/conversation-context';

@Component({
  selector: 'app-conversation-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './conversation-list.component.html',
  styleUrl: './conversation-list.component.scss'
})
export class ConversationListComponent implements OnInit {
  readonly statusLabels = MATCH_STATUS_LABELS;
  readonly contextShort = conversationContextShort;
  readonly roleLabel = participantRoleLabel;
  readonly initials = initials;

  conversations = signal<ConversationSummary[]>([]);
  loading = signal(false);
  respondingId = signal<number | null>(null);

  constructor(
    private chatService: ChatService,
    private matchesService: MatchesService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.chatService.getConversations().subscribe({
      next: (list) => {
        this.conversations.set(list);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  respond(matchId: number, accept: boolean, event: Event): void {
    event.stopPropagation();
    event.preventDefault();
    this.respondingId.set(matchId);
    this.matchesService.respond(matchId, { accept }).subscribe({
      next: () => {
        this.respondingId.set(null);
        this.load();
      },
      error: () => this.respondingId.set(null)
    });
  }
}
