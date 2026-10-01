# 1. Atualização para o .NET 10

Data: 01/10/2026

Status: Aceito

## Contexto

O player usava o .NET 7, que está sem suporte desde maio de 2024 e não recebe mais correções de segurança. O .NET 10 é uma versão de suporte longo (LTS), com suporte até novembro de 2028.

## Decisão

Mudar para `net10.0` e atualizar os pacotes:

- `OpenCvSharp4` e `OpenCvSharp4.runtime.win`, de 4.10.0.20240616 para 4.13.0.20260627.
- `NAudio` fica na 2.2.1. Na versão 3, o `MediaFoundationReader` e o `WaveOutEvent` não estão mais no pacote principal, e o projeto deixa de compilar. O áudio vai ser refeito no item de sincronia e trocado no item de multiplataforma, então migrar agora seria trabalho jogado fora. O Dependabot ignora as versões major do NAudio até lá.

Na mesma mudança:

- Sai a pasta `tests/`, que tinha um arquivo inteiro comentado e era compilado junto com o programa. Os testes voltam como projeto próprio no item de testes.
- Com os avisos tratados como erro, o compilador apontou que o campo `reader` do áudio nunca recebia valor. O `MediaFoundationReader` era criado numa variável local dentro de um `using` e descartado logo em seguida, e o `WriteAudio` estourava `NullReferenceException` no primeiro quadro. A correção mínima guarda o leitor no campo e chama `Play()` no player. A sincronia do áudio com o vídeo continua sendo assunto do item de áudio.

## Consequências

- O projeto roda num runtime com suporte por mais dois anos.
- Só o SDK do .NET 10 é necessário para compilar.
- O programa não quebra mais quando o vídeo tem áudio.
- Nenhum pacote com vulnerabilidade conhecida.
