# 11. Exportar para HTML

Data: 06/10/2026

Status: Aceito

## Contexto

Para mostrar o resultado a alguém, era preciso que a pessoa instalasse o player e o FFmpeg. A ideia é salvar o vídeo já convertido num arquivo que toque sozinho no navegador.

## Opções consideradas

- **HTML com um `<span>` por trecho de mesma cor**: o mais simples, mas num vídeo real quase toda célula tem cor diferente. Dez segundos de Big Buck Bunny em 120 colunas deram 29 MB.
- **GIF animado**: abre em qualquer lugar, mas exige desenhar cada letra numa imagem (uma fonte embutida) e limita a 256 cores por quadro.
- **HTML com os quadros compactados e desenhados num `<canvas>`**: cada célula vira 9 bytes (caractere, cor e fundo), o conjunto é compactado com gzip e a página descompacta com a `DecompressionStream` do navegador.

## Decisão

- **HTML com canvas e dados em gzip.** O mesmo trecho caiu de 29 MB para 2,9 MB, num arquivo único, sem dependências.
- `--export arquivo.html` (ou `-o`) converte o vídeo inteiro sem precisar de terminal, no FPS pedido, com 120 colunas por padrão (`--width` muda), e respeita a cor, a paleta e o estilo escolhidos.
- A página ajusta o tamanho da letra à janela e pausa com clique ou espaço. Não leva o áudio.

## Consequências

- A conversão é a mesma do player, então o que sai no HTML é o que se vê no terminal.
- O GIF fica para depois, se fizer falta.
- Navegadores antigos, sem `DecompressionStream`, não tocam a página.
