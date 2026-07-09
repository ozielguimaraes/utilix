# Design System

## Princípios

1. **Mobile-first**: componentes são desenhados para tela pequena primeiro; desktop é melhoria progressiva.
2. **Mudar tema = editar tokens**. Nenhum componente hardcoda cor, espaço ou tipografia.
3. **Nomes semânticos**: `primary`, `secondary`, `surface` — nunca `green`, `orange`, `white`.
4. **Menor carga cognitiva**: ação principal sempre visível, feedback imediato, erros acionáveis.
5. **Sem ornamento desnecessário**: tipografia clara, espaço generoso, cor como sinal.

## Tokens

Definidos em `apps/web/src/styles/tokens.scss` como CSS custom properties. Trocar aparência do produto = editar este arquivo.

### Cores

```scss
:root {
  /* Brand */
  --color-primary:          #00674F;  /* verde Claude Code */
  --color-primary-hover:    #005540;
  --color-primary-active:   #004433;
  --color-primary-soft:     #E6F2EE;
  --color-primary-contrast: #FFFFFF;

  /* Secondary — coral quente (inspirada no Claude Code) */
  --color-secondary:         #D97757;
  --color-secondary-hover:   #BF5D3E;
  --color-secondary-active:  #A64830;
  --color-secondary-soft:    #FBEAE2;
  --color-secondary-contrast:#FFFFFF;

  /* Neutrals */
  --color-bg:          #FAFAF7;  /* cream */
  --color-surface:     #FFFFFF;
  --color-surface-alt: #F4F4EF;
  --color-text:        #1A1A1A;
  --color-text-muted:  #6B6B6B;
  --color-text-subtle: #9C9C96;
  --color-border:      #E6E6E1;
  --color-border-strong:#C9C9C2;

  /* States */
  --color-success: #00674F;
  --color-warning: #E8A355;
  --color-danger:  #C1472E;
  --color-info:    #4A7C96;

  /* States (soft variants p/ backgrounds de alerta) */
  --color-success-soft: #E6F2EE;
  --color-warning-soft: #FBEFD9;
  --color-danger-soft:  #F7DDD3;
  --color-info-soft:    #DFE9EF;
}

[data-theme="dark"] {
  --color-bg:          #0F1210;
  --color-surface:     #1A1F1C;
  --color-surface-alt: #222826;
  --color-text:        #F0EFEA;
  --color-text-muted:  #A3A39A;
  --color-text-subtle: #6B6B63;
  --color-border:      #2A302C;
  --color-border-strong:#3D4541;

  --color-primary-soft:   #0F2A22;
  --color-secondary-soft: #2A1B14;
  --color-success-soft:   #0F2A22;
  --color-warning-soft:   #2A1F0F;
  --color-danger-soft:    #2A130C;
  --color-info-soft:      #132028;
}
```

### Tipografia

```scss
:root {
  --font-sans: 'Inter', -apple-system, BlinkMacSystemFont, system-ui, sans-serif;
  --font-mono: 'JetBrains Mono', 'Courier New', monospace;

  /* Escala modular (razão 1.25) */
  --text-xs:    12px;
  --text-sm:    14px;
  --text-base:  16px;
  --text-lg:    18px;
  --text-xl:    22px;
  --text-2xl:   28px;
  --text-3xl:   36px;
  --text-4xl:   48px;

  --leading-tight:  1.2;
  --leading-normal: 1.5;
  --leading-loose:  1.75;

  --weight-regular: 400;
  --weight-medium:  500;
  --weight-semibold:600;
  --weight-bold:    700;
}
```

### Espaçamento

Escala de 4px para manter ritmo vertical consistente.

```scss
:root {
  --space-1:  4px;
  --space-2:  8px;
  --space-3:  12px;
  --space-4:  16px;
  --space-5:  24px;
  --space-6:  32px;
  --space-7:  40px;
  --space-8:  48px;
  --space-10: 64px;
  --space-12: 96px;
}
```

### Borda, raio, sombra

```scss
:root {
  --radius-sm: 6px;
  --radius-md: 10px;
  --radius-lg: 16px;
  --radius-xl: 24px;
  --radius-pill: 999px;

  --shadow-sm: 0 1px 2px rgba(0,0,0,.04);
  --shadow-md: 0 4px 12px rgba(0,0,0,.08);
  --shadow-lg: 0 12px 32px rgba(0,0,0,.12);
  --shadow-focus: 0 0 0 3px var(--color-primary-soft);
}
```

### Motion

```scss
:root {
  --ease:            cubic-bezier(.2, .8, .2, 1);
  --ease-bounce:     cubic-bezier(.34, 1.56, .64, 1);
  --duration-fast:   150ms;
  --duration-base:   250ms;
  --duration-slow:   400ms;
}

@media (prefers-reduced-motion: reduce) {
  :root {
    --duration-fast: 0ms;
    --duration-base: 0ms;
    --duration-slow: 0ms;
  }
}
```

### Breakpoints

Mobile-first. Uso direto em media queries.

```scss
$bp-sm: 480px;   /* phones grandes */
$bp-md: 768px;   /* tablets */
$bp-lg: 1024px;  /* desktop */
$bp-xl: 1280px;  /* desktop grande */
```

## Componentes base

Todos em `apps/web/src/app/shared/ui/`. Standalone, sem dependência de serviço.

### Button

Variantes: `primary` | `secondary` | `ghost` | `danger`. Tamanhos: `sm` | `md` | `lg`. Estados: default, hover, active, disabled, loading.

```html
<ui-button variant="primary" size="md" (clicked)="convert()">Converter</ui-button>
<ui-button variant="ghost" icon="download">Baixar</ui-button>
```

Touch target mínimo: **44×44 px** (acessibilidade iOS/Android).

### Dropzone

Área principal de upload. Estados: `idle`, `hover`, `dragging`, `uploading`, `error`.

```html
<ui-dropzone
  [accept]="['video/*', 'audio/*']"
  [maxSize]="500 * 1024 * 1024"
  (files)="onFiles($event)">
</ui-dropzone>
```

No mobile: tap abre picker nativo. No desktop: drag&drop + clique.

### Progress

Barra determinística (0–100) ou indeterminada (spinner). Mostra stage atual (ex: "Codificando vídeo...").

### Card

Container de conteúdo. Usado no catálogo da home e nos cards de job na fila.

### Badge

Pequeno chip para status (`pending`, `processing`, `completed`, `failed`) e metadados (`MP4`, `720p`).

### Modal, Toast, Input, Select

Componentes padrão com tokens aplicados.

## UX patterns

### Estados de conteúdo

Todo componente que mostra dados suporta 4 estados:

1. **Loading** — skeleton, não spinner centralizado (skeleton preserva layout).
2. **Empty** — ilustração + mensagem curta + CTA.
3. **Error** — ícone + mensagem clara + botão retry.
4. **Content** — o caso real.

### Hierarquia de ações

- **Ação primária**: botão preenchido `--color-primary`. Uma por tela.
- **Ação secundária**: outline ou ghost. Múltiplas permitidas.
- **Ação destrutiva**: `--color-danger`, sempre com confirmação (modal).

### Feedback

- Ação síncrona < 100ms: sem feedback.
- Ação 100ms–1s: botão em estado loading.
- Ação > 1s: progress bar visível + poder cancelar.
- Sucesso: toast verde auto-dismiss em 4s.
- Erro: toast vermelho persistente até ação do usuário.

### Mobile

- Bottom sheet no lugar de modal em telas < 768px.
- Nav inferior (3–5 itens), não lateral.
- Botões principais na parte de baixo da tela (alcance do polegar).
- Swipe-to-dismiss em listas de jobs concluídos.

### Acessibilidade

- Contraste AA mínimo (AAA no texto principal).
- `:focus-visible` com `--shadow-focus` em todos os interativos.
- Todos os campos com `<label>` associado.
- SVG decorativo com `aria-hidden`.
- Live regions para progresso SignalR.

## Layout

### Grid

Container centralizado máx 1280px. Padding lateral:

- mobile: `--space-4` (16px)
- tablet: `--space-6` (32px)
- desktop: `--space-8` (48px)

### Home (catálogo)

- Hero compacto (título + subtítulo, sem imagem).
- Grid de cards com `minmax(260px, 1fr)` e `gap: var(--space-4)`. 1 coluna mobile, 2 tablet, 3–4 desktop.

### Página de conversão

Uma coluna no mobile, duas no desktop:
- Coluna esquerda: dropzone + opções.
- Coluna direita: lista de jobs ativos.

## Nomenclatura

- Variáveis CSS: `--{categoria}-{nome}-{variante?}`. Ex: `--color-primary-hover`.
- Componentes: `ui-{nome}`. Ex: `<ui-dropzone>`.
- Classes utilitárias: evitar. Preferir props de componente.
- Ícones: SVG inline em `shared/ui/icons/`, componente `<ui-icon name="upload">`.

## Dark mode

Aplicado via `[data-theme="dark"]` no `<html>`. Detecção inicial:

```ts
const saved = localStorage.getItem('theme');
const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
document.documentElement.dataset.theme = saved ?? (prefersDark ? 'dark' : 'light');
```

Toggle manual sobrescreve e persiste em `localStorage`.
