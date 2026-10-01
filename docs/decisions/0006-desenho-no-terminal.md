# 6. Desenho no terminal

Data: 01/10/2026

Status: Aceito

## Contexto

O tamanho da imagem saía do vídeo (metade da largura: 640 colunas num vídeo 720p), e não do terminal. Numa janela com 120 colunas, cada linha quebrava em várias e a imagem virava lixo, a não ser com a janela maximizada. Além disso, o player desenhava por cima do que estava no terminal, deixava o cursor piscando no meio da imagem, e cada caractere vinha de um único pixel.

Antes de mexer, cada etapa do quadro foi medida com vídeos de teste em 720p e 1080p:

| Cenário | Tempo por quadro | Ler | Converter | Desenhar | Atrasados |
| --- | --- | --- | --- | --- | --- |
| 720p, 640 colunas, 15 fps | 66 ms | 2,3 ms | 0,5 ms | 1,5 ms | 0 |
| 1080p, 960 colunas, 15 fps | 66 ms | 4,5 ms | 1,0 ms | 2,0 ms | 0 |
| 1080p, 160 colunas, 30 fps | 33 ms | 4,2 ms | 0,08 ms | 0,3 ms | 0 |

## Decisão

- **Sem fila entre leitura e desenho.** O pior caso usa menos de 8 ms dos 33 disponíveis por quadro, então uma fila com `System.Threading.Channels` só traria complexidade.
- **Caber no terminal.** A imagem ocupa o máximo da janela sem distorcer, considerando que o caractere é cerca de duas vezes mais alto que largo, e fica centralizada. O tamanho é conferido a cada quadro, e a imagem acompanha quando a janela muda. O `--width` passa a ser a largura máxima.
- **Redução por média da área.** O OpenCV reduz o quadro direto para o tamanho final (`Cv2.Resize` com `InterpolationFlags.Area`), e cada caractere vira a média dos pixels que ele cobre. Detalhes finos não somem nem piscam, e o quadro copiado para o .NET já é pequeno.
- **Colorido por padrão**, com cor de 24 bits (`ESC[38;2;r;g;bm`). O código da cor só é enviado quando ela muda. `--no-color` desliga, e a variável de ambiente `NO_COLOR` também, como pede a convenção de no-color.org.
- **Tela alternativa.** Igual a programas como vim e htop: o player abre numa tela separada, esconde o cursor e, ao sair (fim do vídeo, Esc ou Ctrl+C), devolve o terminal como estava. No Windows, o modo de terminal virtual do console é ligado para os códigos ANSI funcionarem também no console antigo.
- **Separação.** `Ascii/` produz uma `AsciiImage` (caracteres e cores) sem saber nada de ANSI. `Terminal/` calcula o encaixe (`FrameLayout`) e transforma a imagem em bytes num buffer reaproveitado, com um único write por quadro. A saída é UTF-8, então a paleta aceita caracteres como `░▒▓█`.
- Se a saída estiver redirecionada, o player avisa e sai com erro, em vez de quebrar com exceção.

Medido depois da mudança, na janela padrão do console:

| Cenário | Tamanho | Bytes por quadro | Desenhar | Atrasados |
| --- | --- | --- | --- | --- |
| 720p colorido, 30 fps | 100×28 | 14,7 KB | 0,5 ms | 0 |
| 1080p colorido, 30 fps | 107×30 | 15,8 KB | 0,3 ms | 0 |
| 1080p sem cor, 30 fps | 107×30 | 3,4 KB | 0,4 ms | 0 |

## Consequências

- O player funciona em qualquer tamanho de janela, sem precisar maximizar.
- A cor multiplica por cerca de 5 os bytes enviados por quadro. Se algum terminal não der conta, a próxima medida é reduzir a precisão da cor para as sequências ficarem maiores.
- O tamanho da imagem acompanha a janela, mas o áudio continua sem sincronia até o item de áudio.
