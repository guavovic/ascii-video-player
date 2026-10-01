# 4. Pacotes do NAudio separados

Data: 01/10/2026

Status: Aceito

## Contexto

O ADR 0001 deixou o `NAudio` na 2.2.1, achando que a quebra vinha da versão 3. O Dependabot propôs a 2.4.0, e o build quebrou do mesmo jeito: `MediaFoundationReader` e `WaveOutEvent` não encontrados.

A causa é outra. O `NAudio` é um pacote que só junta os outros. A partir da 2.4.0, num projeto `net10.0`, ele traz só o `NAudio.Core` e o `NAudio.Midi`. O `NAudio.Wasapi` (onde está o `MediaFoundationReader`) e o `NAudio.WinMM` (onde está o `WaveOutEvent`) só vêm quando o projeto mira o Windows (`net10.0-windows`).

Na versão 3, o `NAudio.WinMM` só é publicado para `net9.0-windows`.

## Opções consideradas

- **Mirar `net10.0-windows`**, para usar o pacote `NAudio` na versão 3. O projeto inteiro vira só-Windows, o que vai contra o item de multiplataforma.
- **Referenciar o `NAudio.Wasapi` e o `NAudio.WinMM` direto, na 2.4.0.** O projeto continua `net10.0`, e só a parte de áudio depende do Windows.

## Decisão

Referenciar o `NAudio.Wasapi` e o `NAudio.WinMM` 2.4.0 no lugar do `NAudio`.

- O `NAudioPlayer` é marcado com `[SupportedOSPlatform("windows")]` e só é criado quando `OperatingSystem.IsWindows()`. O analisador de plataforma (CA1416) passa a barrar qualquer uso dele sem essa checagem.
- O `BufferedWaveProvider` recebe a duração do buffer no construtor, porque a propriedade virou somente leitura na 2.4.0.
- O Dependabot ignora as versões major dos pacotes `NAudio.*`. Misturar versões compila, mas o áudio falha ao carregar: com o `NAudio.Wasapi` na 3.1.0 e o `NAudio.WinMM` na 2.4.0, o player roda sem som e mostra `Could not load type 'NAudio.MmResult'`.

## Consequências

- O projeto continua compilando para qualquer sistema. Fora do Windows, ele toca o vídeo sem áudio em vez de quebrar.
- O áudio fica na 2.4.0 até o item de multiplataforma, que troca essa parte.
