# 8. FFmpeg e OpenAL para rodar em qualquer sistema

Data: 01/10/2026

Status: Aceito

## Contexto

O player só rodava no Windows. O vídeo vinha do OpenCV com o runtime nativo do Windows (`OpenCvSharp4.runtime.win`), e o áudio do NAudio, que usa o Media Foundation e o WinMM.

O OpenCvSharp tem runtime oficial atual para o Linux, mas para o macOS só existe um pacote parado desde 2023, e só para Intel. Ele não roda nos Macs com Apple Silicon.

## Opções consideradas

Para decodificar o vídeo e o áudio:

- **FFmpeg instalado na máquina, lido por pipe.** Funciona nos três sistemas, abre qualquer formato e já entrega o quadro reduzido e o áudio em PCM. Quem usa precisa ter o FFmpeg instalado.
- **OpenCvSharp com o runtime do Linux.** Muda pouco, mas deixa o macOS de fora.
- **FFmpeg por bindings (Sdcb.FFmpeg).** O runtime nativo só existe para Windows x64.

Para tocar o áudio (as três trazem as bibliotecas nativas de Windows, Linux e macOS, x64 e arm64):

- **OpenAL Soft (Silk.NET).** Padrão de áudio multiplataforma em jogos. A posição tocada vem pronta da API.
- **SDL3 (ppy.SDL3-CS).** Mantida pelo time do osu!. A posição precisa ser calculada pela fila.
- **PortAudio (PortAudioSharp2).** Funciona por callback, o que dá mais trabalho para sincronizar.

## Decisão

FFmpeg instalado e OpenAL Soft.

- O `ffprobe` lê o tamanho, o FPS, se há áudio e a rotação (vídeos de celular gravados em pé).
- O `ffmpeg` entrega os quadros em `bgr24` por pipe, já reduzidos com média da área (no máximo 640 pixels de largura) e com FPS constante. Uma redução por média da área em C# leva cada quadro ao tamanho exato do terminal. Assim, redimensionar a janela não obriga a reiniciar o `ffmpeg`.
- Um segundo `ffmpeg` entrega o áudio em PCM de 16 bits, estéreo, 48 kHz. Uma thread alimenta o OpenAL com quatro blocos de 100 ms. O relógio mestre é o total já tocado mais a posição dentro do bloco atual (`AL_SAMPLE_OFFSET`).
- Os processos do `ffmpeg` rodam com `-nostdin`, para não roubar as teclas do terminal.
- Sem o FFmpeg no PATH, o player avisa como resolver e sai com erro.
- O OpenCV e o NAudio saem do projeto, e com eles cerca de 60 MB de bibliotecas nativas.
- O CI compila em Ubuntu, Windows e macOS.

## Consequências

Medido no Windows com o vídeo de teste de flash e bipe:

| Cenário | Desenhados | Pulados | Erro médio | Erro máximo |
| --- | --- | --- | --- | --- |
| Com áudio | 294 | 6 | 7,6 ms | 27 ms |
| Com áudio e terminal lento (+80 ms por quadro) | 121 | 179 | 15 ms | 30 ms |
| Sem áudio | 298 | 2 | 15 ms | 18 ms |
| 4 s de áudio num vídeo de 10 s | 299 | 1 | 12 ms | 33 ms |

- O erro continua abaixo de um quadro. Ele é um pouco maior que com o NAudio, porque o OpenAL atualiza a posição em passos maiores.
- No Linux (container com terminal simulado), o vídeo toca do começo ao fim, com cor e tela alternativa. Sem placa de som, o player toca sem áudio, e a própria biblioteca de som do Linux (ALSA) escreve avisos no terminal antes do vídeo começar.
- O macOS é só compilado no CI, sem execução de verdade.
- O FFmpeg passa a ser um pré-requisito de quem usa o player.
