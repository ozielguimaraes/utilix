import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface CreateJobPayload {
  engineSlug: string;
  url?: string;
  options?: Record<string, string>;
}

export interface ProgressReportDto {
  percent: number;
  stage: string;
  message?: string;
}

export interface JobResultInfoDto {
  fileName: string;
  downloadUrl: string;
  sizeBytes: number;
  expiresAt: string;
}

export interface JobErrorInfoDto {
  messageKey: string;
  retryable: boolean;
}

export interface JobStatusResponse {
  jobId: string;
  status: string;
  progress?: ProgressReportDto;
  result?: JobResultInfoDto;
  error?: JobErrorInfoDto;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
}

export interface CreateJobResponse {
  jobId: string;
  status: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class JobsApi {
  private readonly http = inject(HttpClient);

  createJob(payload: CreateJobPayload) {
    return this.http.post<CreateJobResponse>('/api/jobs', payload);
  }

  getJob(id: string) {
    return this.http.get<JobStatusResponse>(`/api/jobs/${id}`);
  }

  downloadUrl(id: string) {
    return `/api/jobs/${id}/download`;
  }
}
