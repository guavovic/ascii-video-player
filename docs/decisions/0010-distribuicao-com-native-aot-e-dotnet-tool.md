# 10. Distribuição com Native AOT e dotnet tool

Data: 01/10/2026

Status: Aceito

## Contexto

Para usar o player, era preciso clonar o repositório e compilar com o SDK do .NET. A ideia é que qualquer pessoa consiga rodar com um download ou um comando, sem compilar nada.

## Opções consideradas

- **Executável nativo por sistema nas Releases**, compilado com Native AOT. Não precisa do .NET na máquina.
- **Executável único sem AOT** (`PublishSingleFile` com trimming). Funcionou sem avisos, mas fica com 14 MB, contra 4 MB do AOT.
- **`dotnet tool` no NuGet.** Instala com `dotnet tool install -g ascii-video-player`, ou roda sem instalar com `dnx ascii-video-player video.mp4` (novo no .NET 10). Precisa do .NET 10.
- **Gerenciadores de pacote** (winget, Homebrew, Scoop). Instalação mais natural em cada sistema, mas com um manifesto ou repositório para manter por gerenciador. Ficou para a v3.

## Decisão

Releases com Native AOT e `dotnet tool` no NuGet.

- O executável e o comando passam a se chamar `ascii-video-player`, o mesmo nome do repositório e do pacote.
- O workflow `release.yml` roda a cada tag `v*`:
  - Compila com Native AOT para `win-x64`, `linux-x64`, `linux-arm64`, `osx-arm64` e `osx-x64`, cada um no runner do próprio sistema (o `osx-x64` é compilado cruzado num Mac ARM).
  - Confere cada binário com `--version`, menos o `osx-x64`.
  - Anexa um `.zip` (Windows) ou `.tar.gz` (Linux e macOS) por sistema na release, com o executável, a biblioteca do OpenAL e a licença.
  - Publica o pacote no NuGet com *Trusted Publishing*: o GitHub prova a identidade do workflow e o NuGet devolve uma chave temporária, sem chave guardada no repositório.
- Na pull request que mexe no workflow ou no `.csproj`, os binários são compilados sem publicar nada.
- A versão vem da tag (`v2.0.0` → `2.0.0`).
- **Globalização invariante.** No Linux, o Native AOT exige a biblioteca ICU, que não vem em instalações mínimas, e o binário quebrava ao iniciar. Com `InvariantGlobalization`, ele não precisa dela. A interface fica fixa em pt-BR (`PredefinedCulturesOnly=false`), já que todo o texto do player é em português. Sem isso, as mensagens da biblioteca de linha de comando sairiam em inglês.

## Consequências

- Pacotes de 2,3 a 2,7 MB compactados. O executável tem cerca de 4 MB, e a biblioteca do OpenAL, cerca de 1 MB.
- O binário de Linux rodou num Debian mínimo, sem .NET e sem ICU, com a ajuda em português e o vídeo até o fim. O de Windows rodou com som.
- O pacote do `dotnet tool` leva as bibliotecas do OpenAL de todos os sistemas (4,7 MB), porque não é separado por sistema.
- O Silk.NET emite avisos de AOT ao procurar a biblioteca nativa pelos caminhos do .NET. Ele acaba achando a biblioteca na pasta do executável.
- O FFmpeg continua sendo pré-requisito em todas as formas de instalação.
- A publicação no NuGet depende de uma configuração única no nuget.org: a conta e a política de Trusted Publishing para este repositório e este workflow.
