# 17. Player no navegador

Data: 06/10/2026

Status: Aceito

## Contexto

Para experimentar o player era preciso instalar o .NET (ou baixar o binário) e o FFmpeg. A ideia da #29 é uma página no GitHub Pages onde a pessoa escolhe um vídeo do próprio computador e ele toca em ASCII ali mesmo, sem enviar o vídeo para lugar nenhum e sem servidor para manter.

O navegador já decodifica o vídeo e toca o som, então o FFmpeg e o OpenAL não fazem falta. Falta só a conversão.

## Opções consideradas

- **Reescrever a conversão em JavaScript**: leve, mas seriam dois códigos para manter, e um estilo novo teria que ser feito duas vezes.
- **Blazor WebAssembly**: roda C#, mas traz componentes, roteamento e renderização que a página não usa.
- **WebAssembly puro do .NET** (`Microsoft.NET.Sdk.WebAssembly`), com `[JSExport]`: o C# expõe só as funções que a página chama, e o resto da página é HTML e JavaScript comuns. Não precisa de workload extra para compilar.

## Decisão

- **WebAssembly puro com o mesmo núcleo.** O projeto `AsciiVideoPlayer.Web` compila os mesmos arquivos do player de terminal (`Ascii/`, `VideoFrame`, `FrameLayout`, `SubtitleTrack`), ligados pelo `.csproj`, não copiados. Um estilo novo aparece no terminal, no export e no navegador de uma vez.
- **O navegador reduz, o C# converte.** A página desenha o quadro do `<video>` num canvas do tamanho que o estilo pede (com suavização alta), manda os pixels para o C# e recebe as células codificadas.
- **Um desenho só.** O desenho das células num canvas (`desenho.js`) é o mesmo da página exportada (ADR 0011): vai embutido no executável para o export e é servido como arquivo na página.
- **O `<video>` é o relógio.** A página desenha quando o tempo do vídeo muda, e o som sai pelo próprio navegador, sempre em sincronia.
- A página tem o exemplo de 12 s do Big Buck Bunny (CC BY 3.0), os cinco estilos, colunas, cor, legenda `.srt`, tela cheia e as mesmas teclas do terminal (espaço e setas).
- **Publicação pelo GitHub Actions**, a cada mudança em `src/` na main, no GitHub Pages.

Medido no Edge, com 120 colunas, o C# leva por quadro: 0,3 ms no ascii, 0,2 ms no meio bloco, 0,6 ms nos contornos e 2,5 a 2,8 ms no braille e no pontilhado. Com 200 colunas, o pior caso é 7 ms. Isso já vem com o interpretador do .NET com o jiterpreter, sem compilação AOT.

## Consequências

- O vídeo nunca sai do computador da pessoa.
- A página baixa o runtime do .NET na primeira visita (alguns MB, comprimidos), e depois fica em cache.
- Os formatos de vídeo são os que o navegador abre (MP4 com H.264, WebM). Para outros, o player de terminal continua sendo o caminho.
- Se algum estilo ficar pesado demais, a compilação AOT do WebAssembly é o próximo passo, ao custo de um download maior.
