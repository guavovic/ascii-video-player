# 9. Testes e benchmark

Data: 01/10/2026

Status: Aceito

## Contexto

O player não tinha testes. Cada mudança era conferida rodando o programa e olhando a tela, com cópias instrumentadas do código para medir a sincronia. A parte mais delicada, a decisão de esperar ou pular quadros, não podia ser testada sem o console de verdade e sem esperar o tempo passar.

## Opções consideradas

- **xUnit v3**, o mais usado no ecossistema .NET aberto e o mesmo do Achaí.
- **TUnit**, que gera o código dos testes na compilação e roda tudo em paralelo. As vantagens dele são velocidade e não fazem diferença com algumas dezenas de testes de milissegundos.
- **MSTest**, que não traz nada a mais para este projeto.

Para os dublês de teste:

- **NSubstitute**, como no Achaí.
- **Fakes escritos à mão.** Aqui os dublês têm estado: um relógio que avança quando mandado, uma fonte de vídeo com um número de quadros, um terminal que guarda o que foi escrito.

## Decisão

- **xUnit v3 + Shouldly**, com a Microsoft.Testing.Platform, e **fakes escritos à mão** (`FakeClock`, `FakeVideoSource`, `FakeTerminal`).
- O `TerminalRenderer` passa a escrever num `ITerminal` (tamanho, escrita e tecla), com o `ConsoleTerminal` como implementação real.
- A espera do `Player` passa a ser do relógio (`IPlaybackClock.WaitUntil`). Num teste, esperar 100 ms é só avançar o relógio falso, sem dormir de verdade.
- **Testes unitários:** paleta, conversão, redução por média da área, encaixe no terminal, geração do ANSI e sincronia do `Player` (esperar, pular quadros atrasados, FPS menor, Esc e cancelamento).
- **Testes de integração com o FFmpeg de verdade:** os vídeos são gerados na hora (`ffmpeg -f lavfi`). Eles conferem o `ffprobe` (tamanho, FPS, áudio, vídeo em pé, arquivo inválido) e a leitura dos quadros. Quando o FFmpeg não está instalado, eles são pulados. No CI eles falham, para não passar verde sem testar nada.
- O CI instala o FFmpeg e roda os testes em Ubuntu, Windows e macOS. É o único lugar onde o player é testado no macOS.
- **Benchmark com BenchmarkDotNet** das etapas de cada quadro, em terminais de 120×30 e 240×60. Ele não roda no CI, porque o resultado depende da máquina.

## Consequências

- 37 testes, em cerca de 5 s com o FFmpeg. Três quebras propositais (pular quadros desligado, média da área errada e cor enviada em todo caractere) derrubaram 6 testes.
- Os testes acharam um defeito: o `Dispose` matava o `ffmpeg` sem esperar o processo terminar, e o arquivo continuava preso por um instante.
- O áudio pelo OpenAL não tem teste automático, porque os runners do CI não têm placa de som.

Benchmark num Intel Core i5-13420H, com imagem de ruído aleatório (o pior caso para a cor, que muda em todo caractere) e o quadro decodificado em 640×360:

| Etapa | 120×30 | 240×60 |
| --- | --- | --- |
| Reduzir até o terminal | 284 μs | 292 μs |
| Converter em caracteres e cores | 12 μs | 29 μs |
| Gerar o ANSI colorido | 164 μs | 322 μs |
| Gerar o ANSI sem cor | 20 μs | 41 μs |
| Quadro inteiro colorido | 290 μs | 623 μs |

Nenhuma etapa aloca memória por quadro. Um quadro colorido inteiro usa menos de 2% dos 33 ms disponíveis a 30 fps.
