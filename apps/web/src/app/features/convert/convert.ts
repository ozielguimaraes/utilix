import { ChangeDetectionStrategy, Component, ElementRef, ViewChild, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from '../../shared/ui/button/button';
import { Card } from '../../shared/ui/card/card';
import { I18n } from '../../core/i18n';

type EngineInfo = {
  icon: string;
  nameKey: string;
  descKey: string;
  accepts: string;
};

const ENGINES: Record<string, EngineInfo> = {
  youtube:  { icon: '▶',  nameKey: 'engines.youtube.name',  descKey: 'engines.youtube.description',  accepts: 'url' },
  video:    { icon: '🎬', nameKey: 'engines.video.name',    descKey: 'engines.video.description',    accepts: 'video/*' },
  audio:    { icon: '♪',  nameKey: 'engines.audio.name',    descKey: 'engines.audio.description',    accepts: 'audio/*' },
  image:    { icon: '🖼',  nameKey: 'engines.image.name',    descKey: 'engines.image.description',    accepts: 'image/*' },
  pdf:      { icon: '📄', nameKey: 'engines.pdf.name',      descKey: 'engines.pdf.description',      accepts: '.pdf' },
  document: { icon: '📝', nameKey: 'engines.document.name', descKey: 'engines.document.description', accepts: '.docx,.pptx,.xlsx,.odt' },
};

function fileMatchesAccept(file: File, accept: string): boolean {
  const tokens = accept
    .split(',')
    .map((item) => item.trim().toLowerCase())
    .filter(Boolean);

  if (!tokens.length) return true;

  const mime = file.type.toLowerCase();
  const name = file.name.toLowerCase();

  return tokens.some((token) => {
    if (token === '*/*') return true;
    if (token.endsWith('/*')) return mime.startsWith(token.slice(0, -1));
    if (token.startsWith('.')) return name.endsWith(token);
    return mime === token;
  });
}

@Component({
  selector: 'app-convert',
  imports: [RouterLink, Button, Card],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './convert.html',
  styleUrl: './convert.scss',
})
export class Convert {
  private readonly i18n = inject(I18n);
  @ViewChild('fileInput') private readonly fileInput?: ElementRef<HTMLInputElement>;

  readonly slug = input.required<string>();
  protected readonly isDragging = signal(false);
  protected readonly selectedFileName = signal<string | null>(null);
  protected readonly hasInvalidType = signal(false);

  protected readonly engine = computed<EngineInfo | null>(() => ENGINES[this.slug()] ?? null);
  protected readonly t = (key: string) => this.i18n.t(key);

  protected openFilePicker(): void {
    this.fileInput?.nativeElement.click();
  }

  protected onDropAreaSpace(event: Event): void {
    event.preventDefault();
    this.openFilePicker();
  }

  protected onDropAreaDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(true);
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'copy';
    }
  }

  protected onDropAreaDragLeave(event: DragEvent): void {
    const currentTarget = event.currentTarget;
    const relatedTarget = event.relatedTarget;
    if (currentTarget instanceof Node && relatedTarget instanceof Node && currentTarget.contains(relatedTarget)) {
      return;
    }
    this.isDragging.set(false);
  }

  protected onDropAreaDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);
    const file = event.dataTransfer?.files.item(0) ?? null;
    this.handleFileSelection(file);
  }

  protected onFileInputChange(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    const file = input?.files?.item(0) ?? null;
    this.handleFileSelection(file);
    if (input) {
      input.value = '';
    }
  }

  private handleFileSelection(file: File | null): void {
    if (!file) return;

    const engine = this.engine();
    if (!engine || engine.accepts === 'url') return;

    if (!fileMatchesAccept(file, engine.accepts)) {
      this.selectedFileName.set(null);
      this.hasInvalidType.set(true);
      return;
    }

    this.hasInvalidType.set(false);
    this.selectedFileName.set(file.name);
  }
}
