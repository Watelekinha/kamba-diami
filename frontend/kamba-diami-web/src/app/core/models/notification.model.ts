export type NotificationType = 'nearby-job' | 'contract-ending' | 'match-accepted' | 'payment';

export interface AppNotification {
  id: number;
  type: NotificationType;
  title: string;
  message: string;
  createdAt: Date;
  read: boolean;
}
