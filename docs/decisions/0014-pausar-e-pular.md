# 14. Pausar e pular

Data: 06/10/2026

Status: Aceito

## Contexto

O player só tocava do começo ao fim, e o Esc saía. Faltava pausar, avançar, voltar e ver em que ponto o vídeo está. O vídeo e o áudio vêm de dois processos do FFmpeg que leem o arquivo em sequência, e o áudio é o relógio (ADR 0007).

## Opções consideradas

- **Pular lendo e descartando quadros**: só funciona para a frente, fica lento em saltos longos e não ajuda o áudio.
- **Reabrir os dois processos na posição nova**: o FFmpeg já sabe começar de qualquer ponto (`-ss` antes do `-i`), rápido mesmo no meio de um filme longo.

## Decisão

- **Pular é recomeçar adiante.** O player devolve "pular para tal tempo", e o laço de fora (o mesmo do `--loop`) abre vídeo, áudio e relógio de novo a partir dali. Nada do player precisa saber voltar no tempo.
- **Teclas:** espaço pausa e continua, ← e → pulam 5 s, ↑ e ↓ pulam 1 min (como no mpv), Esc ou Q saem. Pular durante a pausa continua pausado, mostrando o quadro novo.
- **Pausa no relógio.** O OpenAL pausa a fonte de som, e o relógio sem áudio para o cronômetro. Na volta, os dois continuam de onde estavam.
- **Barra de progresso** na última linha, com o tempo atual, o total e a barra (`━` já tocado, `─` o resto). Aparece na pausa, com as teclas, e por 3 s depois de pausar ou pular, para não cobrir o vídeo o tempo todo. A duração vem do `ffprobe`. Sem duração (uma transmissão, por exemplo), mostra só o tempo atual.
- **Texto por cima da imagem** (`Overlay`): o terminal desenha a linha de status e, acima dela, a legenda (#34), e apaga essas linhas no quadro seguinte, porque a imagem nem sempre cobre a parte de baixo da janela.
- **`--start`** começa de um ponto (`90`, `1:30` ou `1:02:03`), no player e no export.

## Consequências

- Cada salto custa abrir dois processos do FFmpeg, o que leva uma fração de segundo e é imperceptível na prática.
- A tecla é lida a cada quadro, então a resposta leva no máximo um quadro.
