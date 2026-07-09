import { inject, signal, computed } from '@angular/core';
import { JobsApi } from '../../core/jobs-api';
import { SignalRService } from '../../core/signalr';

export type ConversionStatus = 'idle' | 'submitting' | 'processing' | 'completed' | 'failed';

export interface ConversionState {
  status: ConversionStatus;
  progress: { percent: number; stage: string } | null;
  result: { fileName: string; downloadUrl: string } | null;
  error: string | null;
}

export function createConversionStore() {
  const jobsApi = inject(JobsApi);
  const signalR = inject(SignalRService);

  const status = signal<ConversionStatus>('idle');
  const progress = signal<{ percent: number; stage: string } | null>(null);
  const result = signal<{ fileName: string; downloadUrl: string } | null>(null);
  const error = signal<string | null>(null);

  let currentJobId: string | null = null;
  let pollingInterval: ReturnType<typeof setInterval> | null = null;

  const reset = () => {
    status.set('idle');
    progress.set(null);
    result.set(null);
    error.set(null);
    if (currentJobId) {
      signalR.unsubscribeFromJob(currentJobId).catch(() => {});
      currentJobId = null;
    }
    if (pollingInterval) {
      clearInterval(pollingInterval);
      pollingInterval = null;
    }
  };

  const start = async (url: string, format: string, quality: string) => {
    reset();
    status.set('submitting');

    try {
      const response = await jobsApi
        .createJob({
          engineSlug: 'youtube',
          url,
          options: { format, quality },
        })
        .toPromise();

      if (!response) {
        throw new Error('Sem resposta do servidor');
      }

      currentJobId = response.jobId;
      status.set('processing');

      signalR
        .subscribeToJob(currentJobId, {
          onProgress: (percent, stage) => {
            progress.set({ percent, stage });
          },
          onCompleted: () => {
            status.set('completed');
            stopPolling();
          },
          onFailed: (messageKey) => {
            error.set(messageKey);
            status.set('failed');
            stopPolling();
          },
        })
        .catch(() => {
          startPolling(currentJobId!);
        });

      startPolling(currentJobId);
    } catch (err: any) {
      error.set(err?.message || 'Erro ao criar conversão');
      status.set('failed');
    }
  };

  const startPolling = (jobId: string) => {
    if (pollingInterval) return;

    pollingInterval = setInterval(async () => {
      try {
        const jobStatus = await jobsApi.getJob(jobId).toPromise();
        if (!jobStatus) return;

        if (jobStatus.progress) {
          progress.set({ percent: jobStatus.progress.percent, stage: jobStatus.progress.stage });
        }

        if (jobStatus.status === 'completed') {
          if (jobStatus.result) {
            result.set({
              fileName: jobStatus.result.fileName,
              downloadUrl: jobStatus.result.downloadUrl,
            });
          }
          status.set('completed');
          stopPolling();
        } else if (jobStatus.status === 'failed') {
          error.set(jobStatus.error?.messageKey || 'Erro desconhecido');
          status.set('failed');
          stopPolling();
        }
      } catch (err) {
        console.error('Erro ao fazer polling:', err);
      }
    }, 2000);
  };

  const stopPolling = () => {
    if (pollingInterval) {
      clearInterval(pollingInterval);
      pollingInterval = null;
    }
  };

  return {
    status: status.asReadonly(),
    progress: progress.asReadonly(),
    result: result.asReadonly(),
    error: error.asReadonly(),
    start,
    reset,
  };
}
