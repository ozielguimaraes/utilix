import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export interface ConversionEvents {
  onProgress?: (percent: number, stage: string, message?: string) => void;
  onCompleted?: () => void;
  onFailed?: (errorMessageKey: string) => void;
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private connection: HubConnection | null = null;
  private activeSubscriptions = new Map<string, ConversionEvents>();

  async subscribeToJob(jobId: string, events: ConversionEvents): Promise<void> {
    if (!this.connection) {
      this.connection = new HubConnectionBuilder()
        .withUrl('/api/hubs/conversion')
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build() as HubConnection;

      try {
        await this.connection.start();
      } catch (error) {
        console.warn('SignalR connection failed, will use polling:', error);
        this.connection = null;
        return;
      }
    }

    this.activeSubscriptions.set(jobId, events);

    try {
      await this.connection.invoke('SubscribeToJob', jobId);

      this.connection.off('progress');
      this.connection.off('completed');
      this.connection.off('failed');

      this.connection.on('progress', (data: { percent: number; stage: string; message?: string }) => {
        const subs = this.activeSubscriptions.get(jobId);
        subs?.onProgress?.(data.percent, data.stage, data.message);
      });

      this.connection.on('completed', () => {
        const subs = this.activeSubscriptions.get(jobId);
        subs?.onCompleted?.();
      });

      this.connection.on('failed', (data: { errorMessageKey: string }) => {
        const subs = this.activeSubscriptions.get(jobId);
        subs?.onFailed?.(data.errorMessageKey);
      });
    } catch (error) {
      console.warn('Failed to subscribe to job events, will use polling:', error);
    }
  }

  async unsubscribeFromJob(jobId: string): Promise<void> {
    this.activeSubscriptions.delete(jobId);

    if (this.connection?.state === 'Connected') {
      try {
        await this.connection.invoke('UnsubscribeFromJob', jobId);
      } catch (error) {
        console.warn('Failed to unsubscribe from job:', error);
      }
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection?.state === 'Connected') {
      try {
        await this.connection.stop();
      } catch (error) {
        console.warn('Failed to disconnect SignalR:', error);
      }
    }
  }
}
