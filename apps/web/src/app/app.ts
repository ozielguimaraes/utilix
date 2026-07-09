import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { I18n } from './core/i18n';
import { ThemeService } from './core/theme';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly i18n = inject(I18n);
  private readonly themeService = inject(ThemeService);

  protected readonly lang = this.i18n.lang;
  protected readonly theme = this.themeService.theme;

  protected readonly title = () => this.i18n.t('app.title');
  protected readonly t = (key: string) => this.i18n.t(key);

  protected toggleLang(): void {
    this.i18n.toggle();
  }

  protected toggleTheme(): void {
    this.themeService.toggle();
  }
}
