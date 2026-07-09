import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Card } from '../../shared/ui/card/card';
import { I18n } from '../../core/i18n';

type EngineCard = {
  slug: string;
  icon: string;
  nameKey: string;
  descKey: string;
};

const ENGINES: readonly EngineCard[] = [
  { slug: 'youtube',  icon: '▶',  nameKey: 'engines.youtube.name',  descKey: 'engines.youtube.description' },
  { slug: 'video',    icon: '🎬', nameKey: 'engines.video.name',    descKey: 'engines.video.description' },
  { slug: 'audio',    icon: '♪',  nameKey: 'engines.audio.name',    descKey: 'engines.audio.description' },
  { slug: 'image',    icon: '🖼',  nameKey: 'engines.image.name',    descKey: 'engines.image.description' },
  { slug: 'pdf',      icon: '📄', nameKey: 'engines.pdf.name',      descKey: 'engines.pdf.description' },
  { slug: 'document', icon: '📝', nameKey: 'engines.document.name', descKey: 'engines.document.description' },
];

@Component({
  selector: 'app-home',
  imports: [Card, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  private readonly i18n = inject(I18n);

  protected readonly engines = ENGINES;
  protected readonly t = (key: string) => this.i18n.t(key);
}
