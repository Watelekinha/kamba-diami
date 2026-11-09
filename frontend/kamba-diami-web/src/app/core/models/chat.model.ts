import { JobPostingType } from './job-posting.model';
import { UserRole } from './user.model';

export type MatchStatus = 'Pendente' | 'Aceite' | 'Recusado' | 'Encerrado';

export interface ParticipantSummary {
  userId: string;
  name: string;
  photoPath?: string | null;
  role: UserRole;
}

export interface Match {
  id: number;
  jobPostingId: number;
  jobPostingSummary: string;
  counterpart: ParticipantSummary;
  isInitiatedByMe: boolean;
  status: MatchStatus;
  conversationId?: number | null;
  createdAt: string;
  respondedAt?: string | null;
}

export interface CreateMatchRequest {
  jobPostingId: number;
}

export interface RespondMatchRequest {
  accept: boolean;
}

export interface Message {
  id: number;
  conversationId: number;
  senderUserId: string;
  senderName: string;
  content: string;
  sentAt: string;
  readAt?: string | null;
}

export interface ConversationSummary {
  id: number;
  matchId: number;
  jobPostingId: number;
  jobPostingType: JobPostingType;
  jobPostingSummary: string;
  matchStatus: MatchStatus;
  isInitiatedByMe: boolean;
  counterpart: ParticipantSummary;
  lastMessage?: string | null;
  lastMessageAt?: string | null;
  unreadCount: number;
}
