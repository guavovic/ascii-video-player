# 13. Menos bytes de cor

Data: 06/10/2026

Status: Aceito

## Contexto

O código de cor (`ESC[38;2;r;g;bm`, cerca de 18 bytes) só deixava de ser enviado quando a cor era idêntica à anterior. Num vídeo real, quase toda célula muda um pouco. Com a cor no brilho máximo (ADR 0012), isso piorou: cada célula leva o código. O meio bloco manda ainda a cor de fundo.

Medido com os 10 segundos do Big Buck Bunny em 100×28, os mesmos 300 quadros em cada linha:

| Estilo | Tolerância | Passos | Bytes por quadro |
| --- | --- | --- | --- |
| ascii | 0 | 1 | 53,4 KB |
| ascii | 16 | 1 | 29,6 KB |
| ascii | 32 | 1 | 18,8 KB |
| ascii | 0 | 16 | 37,8 KB |
| ascii | 16 | 16 | 24,5 KB |
| blocks | 0 | 1 | 100,4 KB |
| blocks | 16 | 1 | 62,0 KB |
| blocks | 32 | 1 | 39,7 KB |
| blocks | 0 | 16 | 74,9 KB |

O tempo de desenho ficou entre 0,2 e 0,6 ms por quadro em todos os casos. O custo está no terminal, que precisa interpretar os bytes.

## Opções consideradas

- **Tolerância** (#42): reaproveitar a cor anterior quando a nova difere pouco (soma das diferenças de R, G e B).
- **Passos de cor** (#43): arredondar cada canal para um múltiplo de um passo, para vizinhas ficarem iguais.
- **As duas juntas.**

## Decisão

- **Tolerância 16 por padrão** (`--color-tolerance`, e `0` volta ao comportamento antigo). Corta 45% dos bytes no ascii e 38% no meio bloco sem diferença visível lado a lado. No ascii, nem 48 aparece; no meio bloco, 32 já mostra faixas horizontais nas áreas de degradê, então o padrão fica no valor seguro para os dois.
- **Sem cor da letra no espaço.** O espaço não mostra a letra, então a cor dele não é enviada.
- **Passos desligados por padrão** (`--color-steps`, `1` desliga). Economizam menos que a tolerância com a mesma perda e, junto dela, quase nada a mais. Ficam como opção, que também serve para um visual com poucas cores, e valem também para a página exportada.
- O "sem fundo" nunca é tratado como próximo de um fundo escuro, para a célula não herdar fundo errado.

## Consequências

- Terminais lentos recebem bem menos dados por quadro, sem mudar o que se vê.
- A tolerância só existe no terminal. Na página exportada, o gzip já cuida do tamanho.
