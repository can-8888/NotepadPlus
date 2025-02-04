import { Notification } from '../types/Notification';
import { NotificationType } from '../types/NotificationType';
import { api } from './api';

class NotificationService {
    async getNotifications(): Promise<Notification[]> {
        return await api.getNotifications();
    }

    async markAsRead(notificationId: number): Promise<void> {
        await api.markNotificationAsRead(notificationId);
    }

    async markAllAsRead(): Promise<void> {
        await api.markAllNotificationsAsRead();
    }
}

export const notificationService = new NotificationService(); 