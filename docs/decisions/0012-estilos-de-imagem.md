# 12. Estilos de imagem

Data: 06/10/2026

Status: Aceito

## Contexto

Até aqui, cada célula do terminal era um pixel do vídeo, desenhado com uma letra mais ou menos densa. Faltavam o modo meio bloco (o dobro de resolução vertical), os caracteres braille, o pontilhado e os contornos. Cada um precisa de uma quantidade diferente de pixels por célula: o meio bloco usa 1×2, o braille 2×4.

## Opções consideradas

- **Um `if` por estilo dentro do conversor**: rápido de escrever, mas cada estilo novo mexeria no mesmo método, e o tamanho do quadro lido continuaria preso a um pixel por célula.
- **Uma interface de estilo**: cada estilo diz quantos pixels ocupa por célula e converte o quadro na `AsciiImage`. O player e o export pedem ao conversor um quadro do tamanho certo e não sabem qual estilo está ativo.

## Decisão

- **Interface `IImageStyle`**, com `PixelsPerColumn`, `PixelsPerRow` e `Convert`. O `AsciiConverter` cria o quadro (`colunas × PixelsPerColumn` por `linhas × PixelsPerRow`) e repassa a conversão. A redução por média da área continua a mesma, só muda o tamanho de destino.
- **`--style`** (ou `-s`) escolhe o estilo:
  - `ascii` (padrão): a letra pelo brilho.
  - `blocks`: o caractere `▀`, com o pixel de cima na cor da letra e o de baixo na cor de fundo. Sem cor, cai para `▀`, `▄`, `█` ou espaço, conforme os pixels acesos.
  - `braille`: 2×4 pontos por caractere, acesos acima do brilho médio do quadro, para cenas escuras também aparecerem.
  - `dither`: o mesmo braille, com pontilhado de Floyd–Steinberg, em que a densidade de pontos vira tom de cinza.
  - `edges`: o ASCII com os contornos por cima. Onde o filtro de Sobel acusa borda forte, a célula vira `|`, `/`, `-` ou `\`, na direção da borda.
- **Cor de fundo por célula** na `AsciiImage`. O terminal só envia o código do fundo quando ele muda, e o estilo `ascii` não gasta nenhum byte a mais.
- **Cor no brilho máximo** (#38): nos estilos de letra e de ponto, o brilho já está na densidade do caractere, então a cor leva só o tom, com o canal mais forte em 255. Antes, parte escura virava letra escura em fundo escuro. O auto-contraste por quadro foi medido e ficou de fora, porque realça o ruído das partes escuras. O meio bloco usa a cor exata, porque nele a cor é a imagem.
- Na página exportada, o meio bloco é desenhado com retângulos e o braille com pontos, para não depender da fonte do navegador.

## Consequências

- Um estilo novo é uma classe nova, sem mexer no player, no terminal nem no export.
- O meio bloco manda uma cor de fundo por célula e aumenta os bytes por quadro. A tolerância de cor (#42) e os passos de cor (#43) atacam esse custo.
- O braille depende da fonte do terminal ter os caracteres, o que é comum nos terminais atuais (Windows Terminal, GNOME Terminal, iTerm2).
