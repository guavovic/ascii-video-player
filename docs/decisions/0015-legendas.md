# 15. Legendas

Data: 06/10/2026

Status: Aceito

## Contexto

Faltava mostrar legendas. O formato mais comum é o `.srt`: blocos com número, intervalo de tempo e texto, às vezes com tags de formatação (`<i>`, `<font>`) e de posição (`{\an8}`). Legendas em português baixadas da internet muitas vezes vêm em Windows-1252, e não em UTF-8.

## Decisão

- **Leitor próprio de `.srt`**, sem biblioteca: uma expressão regular acha a linha de tempo, e as linhas seguintes até a linha em branco são o texto. Aceita vírgula ou ponto nos milissegundos e quebra de linha do Windows. As tags são removidas, porque o terminal não tem como mostrar itálico nem posição.
- **UTF-8 primeiro, Latin-1 de reserva.** Se o arquivo não for UTF-8 válido, é lido como Latin-1, que acerta os acentos de um arquivo em Windows-1252. A diferença entre os dois (aspas curvas, travessão) é rara em legenda, e o Latin-1 já vem no .NET, sem pacote extra para o Native AOT.
- **`--subtitles arquivo.srt`**, e, sem a opção, um `.srt` com o mesmo nome do vídeo é carregado sozinho, como fazem o VLC e o mpv.
- **Por cima da imagem**, centralizada, em branco sobre preto, logo acima da linha da barra de progresso (ADR 0014), sincronizada com o relógio do áudio. A legenda certa é achada por busca binária. Se uma legenda longa estiver por baixo de uma curta, a longa volta quando a curta acaba.
- **No export também**: as legendas vão na página como uma lista em JSON, já ajustadas ao `--start`, e são desenhadas no canvas por cima do quadro.

## Consequências

- Legenda com erro de leitura não impede o vídeo de tocar: o player avisa e segue sem ela.
- Outros formatos (`.ass`, `.vtt`) ficam para depois, se fizer falta.
