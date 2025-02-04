import * as signalR from '@microsoft/signalr';
import { api } from './api';
import { Notification } from '../types/Notification';
import { NotificationType } from '../types/NotificationType';

class SignalRService {
    private connection: signalR.HubConnection | null = null;
    private reconnectAttempts = 0;
    private readonly maxReconnectAttempts = 5;

    public async startConnection(): Promise<void> {
        try {
            if (this.connection?.state === signalR.HubConnectionState.Connected) {
                console.log('SignalR already connected');
                return;
            }

            const baseUrl = process.env.REACT_APP_API_URL || 'http://localhost:5000';
            const userId = localStorage.getItem('userId');
            const token = localStorage.getItem('token');
            
            if (!userId || !token) {
                console.error('Missing userId or token');
                return;
            }

            console.log('Configuring SignalR connection...');
            
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(`${baseUrl}/notificationHub`, {
                    transport: signalR.HttpTransportType.WebSockets,
                    skipNegotiation: false,
                    headers: {
                        'UserId': userId,
                        'Authorization': `Bearer ${token}`
                    }
                })
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: retryContext => {
                        if (retryContext.previousRetryCount >= this.maxReconnectAttempts) {
                            return null;
                        }
                        return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
                    }
                })
                .configureLogging(signalR.LogLevel.Debug)
                .build();

            this.setupConnectionHandlers();

            console.log('Starting SignalR connection...');
            await this.connection.start();
            console.log('SignalR Connected successfully');

            // Join user group after successful connection
            await this.connection.invoke('JoinUserGroup', userId);
        } catch (err) {
            console.error('Error starting SignalR connection:', err);
            throw err;
        }
    }

    private setupConnectionHandlers(): void {
        if (!this.connection) return;

        this.connection.onreconnecting(error => {
            console.log('SignalR reconnecting:', error);
            this.reconnectAttempts++;
        });

        this.connection.onreconnected(connectionId => {
            console.log('SignalR reconnected. Connection ID:', connectionId);
            this.reconnectAttempts = 0;
        });

        this.connection.onclose(error => {
            console.log('SignalR connection closed:', error);
            if (this.reconnectAttempts >= this.maxReconnectAttempts) {
                console.log('Max reconnection attempts reached');
                window.location.href = '/login';
            }
        });
    }

    public isConnected(): boolean {
        return this.connection?.state === signalR.HubConnectionState.Connected;
    }

    public onNotification(callback: (notification: Notification) => void): void {
        this.connection?.on('ReceiveNotification', (notification: Notification) => {
            console.log('Received notification:', notification);
            callback(notification);
        });
    }

    public offNotification(): void {
        this.connection?.off('ReceiveNotification');
    }

    public addNoteUpdateListener(callback: (note: any) => void): void {
        this.connection?.on('NoteUpdated', callback);
    }

    public addNoteDeleteListener(callback: (noteId: number) => void): void {
        this.connection?.on('NoteDeleted', callback);
    }

    public async stopConnection(): Promise<void> {
        try {
            await this.connection?.stop();
            console.log('SignalR Disconnected');
        } catch (err) {
            console.error('Error stopping SignalR connection:', err);
            throw err;
        }
    }

    public async reconnect(): Promise<void> {
        if (!this.isConnected()) {
            await this.startConnection();
        }
    }
}

export const signalRService = new SignalRService(); 