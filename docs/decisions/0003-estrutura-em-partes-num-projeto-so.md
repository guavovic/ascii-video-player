# 3. Estrutura em partes num projeto só

Data: 01/10/2026

Status: Aceito

## Contexto

O player inteiro estava num arquivo, numa classe estática, com a leitura do vídeo, a conversão para ASCII, o desenho no terminal e o áudio misturados. Nada podia ser testado sozinho, e os próximos itens da v2 mexem em partes diferentes: o desenho no terminal (item 3), o áudio (item 4) e as bibliotecas de vídeo e áudio de cada sistema (item 5).

Os players de vídeo em ASCII mais conhecidos seguem o mesmo pipeline: ler o quadro, reduzir, transformar o brilho em caractere e desenhar, com o áudio como parte opcional.

## Opções consideradas

- **Um projeto, com uma pasta por parte.** Interface só onde a troca já está prevista.
- **Biblioteca + app de console** (`Core` e `Cli`). Mais reaproveitável, mas é estrutura demais para um player desse tamanho.
- **Arquitetura hexagonal**, com portas e adaptadores. Isola tudo, mas para um player de terminal lê como exagero.

Para o fluxo dos quadros:

- **Laço único** (ler, converter, desenhar, esperar).
- **Fila com `System.Threading.Channels`**, com a leitura numa tarefa e o desenho em outra.

## Decisão

Um projeto (`src/AsciiVideoPlayer`), com uma pasta por parte:

- `Video/`: `IVideoSource` e `OpenCvVideoSource`. O quadro sai como `VideoFrame`, com os pixels num `byte[]` próprio, para o resto do código não depender do OpenCV.
- `Ascii/`: `AsciiConverter` e `CharacterPalette`, sem nada de console nem de OpenCV.
- `Terminal/`: `TerminalRenderer`.
- `Audio/`: `IAudioPlayer` e `NAudioPlayer`.
- `Playback/`: `Player`, com o laço.

Interface só para o vídeo e o áudio, que são as partes que o item 5 troca por plataforma.

O laço continua único. A fila com Channels volta a ser considerada no item 3, se a medição mostrar que a leitura atrasa o desenho.

Na mesma mudança, o projeto passa de `VideoToAsciiConverter` para `AsciiVideoPlayer`, o mesmo nome do repositório. A solução vira `AsciiVideoPlayer.slnx`, e entra um `.gitattributes` com `* text=auto`, para o Git guardar todo arquivo de texto com LF e cada sistema receber o fim de linha dele no checkout.

## Consequências

- O comportamento é o mesmo de antes. Isso inclui o que já estava errado: o caminho fixo do vídeo (item 2) e o avanço de três quadros por volta do laço, com espera calculada para dois (item 4).
- A conversão pode ser testada e medida sem vídeo e sem terminal (item 6).
- O item 5 troca as implementações de `IVideoSource` e `IAudioPlayer` sem mexer no resto.
