# 18. Desenho em WebGL e pontilhado ordenado

Data: 07/10/2026

Status: Aceito

## Contexto

No navegador, com mais colunas o vídeo travava. Medido no Edge com o exemplo de 640×360, por quadro:

| Estilo e colunas | Ler o quadro | Converter (C#) | Desenhar (canvas 2D) |
| --- | --- | --- | --- |
| ascii, 120 | 1,6 ms | 0,6 ms | 10 ms |
| pontilhado, 250 | 3,7 ms | 12 ms | 57 ms |
| ascii, 500 | 4,8 ms | 8 ms | 212 ms |
| pontilhado, 500 | 12 ms | 69 ms | 370 ms |

O desenho (um `fillText` por letra, um círculo por ponto de braille) era quase todo o tempo. Depois dele, a conversão do pontilhado, que usava Floyd–Steinberg: um laço em que cada ponto depende dos anteriores.

## Decisão

- **WebGL para desenhar.** As letras são desenhadas uma vez só numa imagem (o atlas), e cada quadro envia duas texturas do tamanho da grade: cor e índice da letra, e cor do fundo. Um shader pinta todas as células de uma vez. As legendas ficam num canvas 2D por cima, redesenhado só quando o texto muda. Sem WebGL, cai para o canvas 2D, que só troca a cor quando ela muda. Vale para a página do navegador e para a página exportada, que usam o mesmo `desenho.js`.
- **Pontilhado ordenado (matriz de Bayer 4×4)** no lugar do Floyd–Steinberg: cada ponto compara o brilho com um limite fixo da matriz, sem depender dos vizinhos. É mais barato e, no vídeo, a área parada não "ferve" de um quadro para o outro. Vale também no terminal.

Depois, no mesmo teste:

| Estilo e colunas | Ler o quadro | Converter (C#) | Desenhar (WebGL) |
| --- | --- | --- | --- |
| ascii, 120 | 1,9 ms | 0,6 ms | 3 ms |
| ascii, 500 | 2,1 ms | 5,4 ms | 1 ms |
| contornos, 500 | 3,4 ms | 11 ms | 1 ms |
| pontilhado, 500 | 5,2 ms | 16 ms | 1 ms |

Com 500 colunas, o quadro inteiro leva de 8 a 22 ms, o suficiente para 45 a 120 quadros por segundo.

## Consequências

- O desenho deixa de depender do número de colunas. O limite passa a ser a conversão em C#, que ainda pode ganhar com a compilação AOT do WebAssembly, se fizer falta.
- O pontilhado tem agora a textura regular da matriz de Bayer, em vez do granulado do Floyd–Steinberg.
