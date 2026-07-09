import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'ui-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article [attr.data-interactive]="interactive() ? '' : null">
      <ng-content />
    </article>
  `,
  styleUrl: './card.scss',
})
export class Card {
  readonly interactive = input<boolean>(false);
}
