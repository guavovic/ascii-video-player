# ASCII Video Player

Player de vídeo para o terminal. Cada quadro vira texto em caracteres ASCII coloridos, ajustado ao tamanho da janela, com o áudio tocando junto. Roda no Windows, no Linux e no macOS.

<p align="center">
  <img src="https://raw.githubusercontent.com/guavovic/ascii-video-player/main/docs/assets/ascii-video-player.gif" alt="Trecho de Big Buck Bunny tocando no terminal em caracteres ASCII coloridos" width="560"><br>
  <sub>Vídeo do GIF: <a href="https://peach.blender.org">Big Buck Bunny</a>, © Blender Foundation, licença <a href="https://creativecommons.org/licenses/by/3.0/">CC BY 3.0</a>.</sub>
</p>

## Como foi feito

A primeira versão era um arquivo só, presa ao Windows, com o caminho do vídeo fixo no código e o áudio quebrado. A segunda foi reconstruída em etapas, cada uma com a decisão registrada num ADR.

- **Partes separadas:** leitura do vídeo, conversão para ASCII, desenho no terminal e áudio ficam em pastas próprias, e só a leitura do vídeo e o áudio ficam atrás de interfaces.
- **Linha de comando:** o vídeo é passado como argumento, com opções de largura, FPS, paleta de caracteres, sem áudio e sem cor, e mensagens de erro em português.
- **Desenho no terminal:** a imagem acompanha o tamanho da janela, respeita a proporção do caractere e sai colorida. Cada quadro é montado num buffer e escrito de uma vez, numa tela separada que some ao terminar.
- **Áudio como relógio:** o vídeo segue a posição do que já saiu na caixa de som, como fazem os players de vídeo, e um terminal lento pula quadros em vez de ficar para trás.
- **Qualquer sistema:** o vídeo e o áudio são decodificados pelo FFmpeg, e o som sai pelo OpenAL Soft, que funciona nos três sistemas.
- **Testes e medição:** testes unitários com dublês feitos à mão, testes de integração com o FFmpeg de verdade nos três sistemas e benchmark da conversão (um quadro colorido inteiro em menos de 1 ms, sem alocar memória).
- **Distribuição:** binário nativo compilado com Native AOT para cada sistema, publicado automaticamente a cada versão.

## Tecnologias

- **Aplicação:** .NET 10, System.CommandLine, sequências ANSI para cor e controle do terminal.
- **Mídia:** FFmpeg para o vídeo e o áudio, OpenAL Soft (Silk.NET) para tocar o som.
- **Testes:** xUnit v3, Shouldly e BenchmarkDotNet.
- **Entrega:** GitHub Actions com build e testes no Windows, no Linux e no macOS, e Native AOT nas releases.

## Documentação

- [Decisões de arquitetura](docs/decisions): o porquê de cada escolha, com as alternativas consideradas.
