import { NotificationType } from './NotificationType';

export interface Notification {
    id: number;
    userId: number;
    message: string;
    type: NotificationType;
    isRead: boolean;
    createdAt: string;
    relatedEntityId?: number;
    relatedEntityType?: string;
} 