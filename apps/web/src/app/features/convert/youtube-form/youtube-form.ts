import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Button } from '../../../shared/ui/button/button';
import { I18n } from '../../../core/i18n';
import { createConversionStore } from '../conversion-store';
import { JobsApi } from '../../../core/jobs-api';

@Component({
  selector: 'app-youtube-form',
  standalone: true,
  imports: [CommonModule, FormsModule, Button],
  templateUrl: './youtube-form.html',
  styleUrl: './youtube-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class YoutubeForm {
  private readonly i18n = inject(I18n);
  private readonly jobsApi = inject(JobsApi);
  private readonly store = createConversionStore();

  protected readonly url = signal('');
  protected readonly format = signal<'video' | 'audio'>('video');
  protected readonly quality = signal('best');

  protected readonly t = (key: string) => this.i18n.t(key);
  protected readonly status = this.store.status;
  protected readonly progress = this.store.progress;
  protected readonly result = this.store.result;
  protected readonly error = this.store.error;

  protected isSubmitting = () => this.status() === 'submitting';
  protected isProcessing = () => this.status() === 'processing';
  protected isCompleted = () => this.status() === 'completed';
  protected isFailed = () => this.status() === 'failed';
  protected isDisabled = () => !this.url() || this.isSubmitting() || this.isProcessing();

  protected onUrlInput = (e: Event) => {
    const input = e.target as HTMLInputElement;
    this.url.set(input.value);
  };

  protected onFormatChange = (e: Event) => {
    const select = e.target as HTMLSelectElement;
    this.format.set(select.value === 'audio' ? 'audio' : 'video');
  };

  protected onQualityChange = (e: Event) => {
    const select = e.target as HTMLSelectElement;
    this.quality.set(select.value);
  };

  protected submit() {
    const urlValue = this.url().trim();
    if (!urlValue) return;

    this.store.start(urlValue, this.format(), this.quality());
  }

  protected download() {
    if (!this.result()) return;
    const link = document.createElement('a');
    link.href = this.result()!.downloadUrl;
    link.download = this.result()!.fileName;
    link.click();
  }

  protected reset() {
    this.url.set('');
    this.format.set('video');
    this.quality.set('best');
    this.store.reset();
  }
}
