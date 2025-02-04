import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { Notification } from '../types/Notification';
import { NotificationType } from '../types/NotificationType';
import { notificationService } from '../services/notificationService';
import { signalRService } from '../services/signalRService';
import './NotificationBell.css';

const NotificationBell: React.FC = () => {
    const [notifications, setNotifications] = useState<Notification[]>([]);
    const [showDropdown, setShowDropdown] = useState(false);
    const [isLoading, setIsLoading] = useState(false);
    const navigate = useNavigate();
    const dropdownRef = useRef<HTMLDivElement>(null);

    const loadNotifications = useCallback(async () => {
        try {
            setIsLoading(true);
            console.log('Loading notifications...');
            const data = await notificationService.getNotifications();
            console.log('Loaded notifications:', data);
            setNotifications(data as Notification[]); // Type assertion to match the expected type
        } catch (error) {
            console.error('Failed to load notifications:', error);
        } finally {
            setIsLoading(false);
        }
    }, []);

    useEffect(() => {
        console.log('Setting up notification listeners');
        loadNotifications();
        
        const setupNotifications = async () => {
            try {
                if (!signalRService.isConnected()) {
                    console.log('SignalR not connected, connecting...');
                    await signalRService.startConnection();
                }

                signalRService.onNotification((notification: Notification) => {
                    console.log('NotificationBell received notification:', notification);
                    setNotifications(prev => {
                        if (prev.some(n => n.id === notification.id)) {
                            console.log('Notification already exists, skipping');
                            return prev;
                        }
                        console.log('Adding new notification');
                        return [notification, ...prev];
                    });
                });
            } catch (error) {
                console.error('Error setting up notifications:', error);
            }
        };

        setupNotifications();

        const interval = setInterval(loadNotifications, 300000);
        
        return () => {
            console.log('Cleaning up notification listeners');
            clearInterval(interval);
            signalRService.offNotification();
        };
    }, [loadNotifications]);

    // Add click outside handler
    useEffect(() => {
        const handleClickOutside = (event: MouseEvent) => {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
                setShowDropdown(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, []);

    const handleNotificationClick = async (notification: Notification) => {
        try {
            if (!notification.isRead) {
                await notificationService.markAsRead(notification.id);
                setNotifications(prev =>
                    prev.map(n =>
                        n.id === notification.id ? { ...n, isRead: true } : n
                    )
                );
            }

            // Navigate based on notification type
            switch (notification.type) {
                case NotificationType.NEW_PUBLIC_NOTE:
                    console.log('Navigating to public notes');
                    navigate('/notes', { state: { type: 'public' } });
                    break;
                case NotificationType.NOTE_SHARED:
                    console.log('Navigating to shared notes');
                    navigate('/notes', { state: { type: 'shared' } });
                    break;
                default:
                    console.log('No navigation for notification type:', notification.type);
            }
        } catch (error) {
            console.error('Error handling notification click:', error);
        }
    };

    const handleMarkAllAsRead = async () => {
        await notificationService.markAllAsRead();
        loadNotifications();
    };

    const unreadCount = notifications.filter(n => !n.isRead).length;

    // Update the notification list rendering to show history with dates
    const renderNotificationList = () => {
        if (isLoading) {
            return (
                <div className="notifications-loading">
                    Loading notifications...
                </div>
            );
        }

        if (notifications.length === 0) {
            return (
                <div className="no-notifications">
                    No notifications yet
                </div>
            );
        }

        // Group notifications by date
        const groupedNotifications = notifications.reduce((groups: { [key: string]: Notification[] }, notification) => {
            const date = new Date(notification.createdAt).toLocaleDateString();
            if (!groups[date]) {
                groups[date] = [];
            }
            groups[date].push(notification);
            return groups;
        }, {});

        return Object.entries(groupedNotifications).map(([date, notifs]) => (
            <div key={date} className="notification-group">
                <div className="notification-date">{date}</div>
                {notifs.map(notification => (
                    <div
                        key={`${notification.id}-${notification.type}`}
                        className={`notification-item ${!notification.isRead ? 'unread' : ''}`}
                        onClick={() => handleNotificationClick(notification)}
                    >
                        <div className="notification-content">
                            <p>{notification.message}</p>
                            <span className="notification-time">
                                {new Date(notification.createdAt).toLocaleTimeString()}
                            </span>
                        </div>
                    </div>
                ))}
            </div>
        ));
    };

    const getNotificationIcon = (type: NotificationType) => {
        switch (type) {
            case NotificationType.SUCCESS:
                return '✅';
            case NotificationType.ERROR:
                return '❌';
            case NotificationType.WARNING:
                return '⚠️';
            default:
                return 'ℹ️';
        }
    };

    return (
        <div className="notification-bell" ref={dropdownRef}>
            <div className="bell-icon" onClick={() => setShowDropdown(!showDropdown)}>
                🔔
                {unreadCount > 0 && (
                    <span className="notification-badge">{unreadCount}</span>
                )}
            </div>

            {showDropdown && (
                <div className="notification-dropdown">
                    <div className="notification-header">
                        <h3>Notifications</h3>
                        {unreadCount > 0 && (
                            <button 
                                className="mark-all-read"
                                onClick={handleMarkAllAsRead}
                            >
                                Mark all as read
                            </button>
                        )}
                    </div>
                    <div className="notification-list">
                        {renderNotificationList()}
                    </div>
                </div>
            )}
        </div>
    );
};

export default NotificationBell; 