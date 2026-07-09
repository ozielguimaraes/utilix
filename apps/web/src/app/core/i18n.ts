import { Injectable, computed, signal } from '@angular/core';

export type Lang = 'pt-BR' | 'en';

type Dict = Record<string, string>;

const DICTIONARIES: Record<Lang, Dict> = {
  'pt-BR': {
    'app.title': 'Utilix',
    'app.tagline': 'Converta arquivos em segundos',
    'home.hero.title': 'Um lugar só para converter tudo',
    'home.hero.subtitle':
      'Vídeo, áudio, imagem, PDF, documentos e YouTube. Sem cadastro. Seus arquivos somem em 1 hora.',
    'home.catalog.title': 'Escolha um utilitário',
    'engines.youtube.name': 'YouTube',
    'engines.youtube.description': 'Baixe vídeo ou áudio do YouTube',
    'engines.video.name': 'Vídeo',
    'engines.video.description': 'Converta entre MP4, WebM, MOV, GIF',
    'engines.audio.name': 'Áudio',
    'engines.audio.description': 'Converta entre MP3, WAV, OGG, FLAC',
    'engines.image.name': 'Imagem',
    'engines.image.description': 'JPG, PNG, WebP, AVIF, redimensionar',
    'engines.pdf.name': 'PDF',
    'engines.pdf.description': 'Juntar, dividir, comprimir',
    'engines.document.name': 'Documento',
    'engines.document.description': 'DOCX, PPTX, XLSX para PDF',
    'common.open': 'Abrir',
    'common.toggleTheme': 'Alternar tema',
    'common.language': 'Idioma',
    'convert.back': 'Voltar ao início',
    'convert.wip.badge': 'Em desenvolvimento',
    'convert.wip.title': 'Utilitário em construção',
    'convert.wip.desc':
      'A interface está pronta, mas o motor de conversão ainda está sendo implementado. Em breve.',
    'convert.stub.url': 'Cole a URL aqui...',
    'convert.stub.drop': 'Arraste o arquivo ou toque para escolher',
    'convert.stub.selected': 'Arquivo selecionado:',
    'convert.stub.invalidType': 'Formato de arquivo não suportado para este utilitário.',
    'convert.stub.cta': 'Converter',
    'convert.notfound.title': 'Utilitário não encontrado',
    'convert.notfound.desc': 'A URL que você acessou não corresponde a nenhum utilitário disponível.',
    'convert.youtube.urlLabel': 'URL do YouTube',
    'convert.youtube.formatLabel': 'Formato',
    'convert.youtube.format.video': 'Vídeo (MP4)',
    'convert.youtube.format.audio': 'Áudio (MP3)',
    'convert.youtube.qualityLabel': 'Qualidade',
    'convert.youtube.quality.best': 'Melhor qualidade',
    'convert.youtube.submit': 'Converter',
    'convert.youtube.submitting': 'Criando conversão...',
    'convert.youtube.completed': 'Conversão concluída!',
    'convert.youtube.error': 'Erro na conversão',
    'convert.youtube.download': 'Baixar',
    'convert.youtube.tryAgain': 'Tentar novamente',
    'convert.youtube.expiresIn': 'Link disponível por 1 hora',
    'convert.another': 'Converter outro',
    'errors.engine.youtube_failed': 'Erro ao processar vídeo do YouTube',
    'errors.engine.invalid_youtube_url': 'URL do YouTube inválida',
    'errors.job.timeout': 'Conversão expirou (tempo muito longo)',
    'errors.job.unknown_error': 'Erro desconhecido na conversão',
  },
  en: {
    'app.title': 'Utilix',
    'app.tagline': 'Convert files in seconds',
    'home.hero.title': 'One place to convert everything',
    'home.hero.subtitle':
      'Video, audio, image, PDF, documents and YouTube. No signup. Files auto-delete in 1 hour.',
    'home.catalog.title': 'Pick a tool',
    'engines.youtube.name': 'YouTube',
    'engines.youtube.description': 'Download video or audio from YouTube',
    'engines.video.name': 'Video',
    'engines.video.description': 'Convert between MP4, WebM, MOV, GIF',
    'engines.audio.name': 'Audio',
    'engines.audio.description': 'Convert between MP3, WAV, OGG, FLAC',
    'engines.image.name': 'Image',
    'engines.image.description': 'JPG, PNG, WebP, AVIF, resize',
    'engines.pdf.name': 'PDF',
    'engines.pdf.description': 'Merge, split, compress',
    'engines.document.name': 'Document',
    'engines.document.description': 'DOCX, PPTX, XLSX to PDF',
    'common.open': 'Open',
    'common.toggleTheme': 'Toggle theme',
    'common.language': 'Language',
    'convert.back': 'Back to home',
    'convert.wip.badge': 'Work in progress',
    'convert.wip.title': 'Tool under construction',
    'convert.wip.desc':
      'The UI is ready, but the conversion engine is still being implemented. Coming soon.',
    'convert.stub.url': 'Paste the URL here...',
    'convert.stub.drop': 'Drag a file or tap to choose',
    'convert.stub.selected': 'Selected file:',
    'convert.stub.invalidType': 'Unsupported file type for this tool.',
    'convert.stub.cta': 'Convert',
    'convert.notfound.title': 'Tool not found',
    'convert.notfound.desc': 'The URL you accessed does not match any available tool.',
    'convert.youtube.urlLabel': 'YouTube URL',
    'convert.youtube.formatLabel': 'Format',
    'convert.youtube.format.video': 'Video (MP4)',
    'convert.youtube.format.audio': 'Audio (MP3)',
    'convert.youtube.qualityLabel': 'Quality',
    'convert.youtube.quality.best': 'Best quality',
    'convert.youtube.submit': 'Convert',
    'convert.youtube.submitting': 'Creating conversion...',
    'convert.youtube.completed': 'Conversion complete!',
    'convert.youtube.error': 'Conversion error',
    'convert.youtube.download': 'Download',
    'convert.youtube.tryAgain': 'Try again',
    'convert.youtube.expiresIn': 'Link available for 1 hour',
    'convert.another': 'Convert another',
    'errors.engine.youtube_failed': 'Error processing YouTube video',
    'errors.engine.invalid_youtube_url': 'Invalid YouTube URL',
    'errors.job.timeout': 'Conversion timeout (took too long)',
    'errors.job.unknown_error': 'Unknown conversion error',
  },
};

function detectLanguage(): Lang {
  if (typeof localStorage !== 'undefined') {
    const saved = localStorage.getItem('utilix.lang') as Lang | null;
    if (saved === 'pt-BR' || saved === 'en') return saved;
  }
  if (typeof navigator !== 'undefined') {
    const browser = navigator.language?.toLowerCase() ?? '';
    if (browser.startsWith('pt')) return 'pt-BR';
  }
  return 'en';
}

@Injectable({ providedIn: 'root' })
export class I18n {
  private readonly _lang = signal<Lang>(detectLanguage());
  readonly lang = this._lang.asReadonly();
  readonly dict = computed(() => DICTIONARIES[this._lang()]);

  t(key: string): string {
    return this.dict()[key] ?? key;
  }

  setLang(lang: Lang): void {
    this._lang.set(lang);
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('utilix.lang', lang);
    }
  }

  toggle(): void {
    this.setLang(this._lang() === 'pt-BR' ? 'en' : 'pt-BR');
  }
}
