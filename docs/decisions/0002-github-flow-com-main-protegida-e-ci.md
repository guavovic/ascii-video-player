# 2. GitHub Flow com main protegida e CI

Data: 01/10/2026

Status: Aceito

## Contexto

Toda mudança era commitada direto na `main`, sem nada conferindo se o código ainda compilava. O projeto tem uma pessoa só mantendo. A v1 é o player como estava até aqui, e as mudanças a partir desta decisão formam a v2.

## Opções consideradas

- **Git Flow**, com uma branch `develop` de vida longa, mais branches de release e hotfix. Feito para software com várias versões em suporte ao mesmo tempo e para times maiores.
- **GitHub Flow**: a `main` está sempre pronta, cada mudança vai numa branch curta e entra na `main` por pull request.
- **Trunk-based** com commit direto na `main`. Rápido, mas abre mão da PR como registro de cada mudança.

## Decisão

GitHub Flow.

- Toda mudança vai numa branch própria (`feat/...`, `fix/...`, `chore/...`) e entra na `main` por pull request.
- A `main` é protegida: pull request obrigatória, e o check `Build` precisa passar antes do merge. A regra vale também para administradores.
- O workflow de CI (`.github/workflows/ci.yml`) faz o restore e o build em toda pull request e em todo push na `main`. Aviso de compilação quebra o build (`-warnaserror`), para problemas como o do campo de áudio nunca atribuído não passarem de novo.
- O CI roda no Linux. O programa ainda só funciona no Windows, mas a compilação não depende do sistema. Quando o player for multiplataforma, o workflow ganha uma matriz com Windows, Linux e macOS.
- O Dependabot acompanha os pacotes NuGet e as actions do workflow uma vez por mês, com as atualizações agrupadas numa PR por ecossistema. As correções de segurança não esperam o mês: chegam pelos alertas do GitHub assim que saem. (Atualizado em 01/10/2026: era semanal, com uma PR por pacote, e gerava PRs demais para um projeto mantido por uma pessoa.)
- As versões são tags na `main` (`v1.0.0` marca o player antes destas mudanças, e a v2 sai como tag quando a trilha fechar), não uma branch `develop`.

## Consequências

- A `main` só recebe código que compila sem avisos.
- Cada mudança tem uma pull request explicando o que faz e como foi testada.
- Os testes entram no workflow conforme forem escritos.
