# 7. Áudio como relógio mestre

Data: 01/10/2026

Status: Aceito

## Contexto

O tempo do vídeo vinha de uma espera fixa por quadro, e o áudio era empurrado em pedaços de 1 segundo a cada quadro para um buffer de 4 segundos. O buffer enchia, o excesso era descartado, e áudio e vídeo andavam sem saber um do outro.

## Opções consideradas

- **Áudio como relógio mestre.** O som toca sem interrupção, e o vídeo se ajusta a ele. É o padrão do ffplay, do VLC e do mpv, porque o ouvido percebe muito mais um corte no som do que um quadro a mais ou a menos.
- **Vídeo como mestre.** O áudio é esticado ou cortado para acompanhar. Exige reamostrar o som e soa pior quando corrige.
- **Relógio externo.** Um cronômetro manda nos dois. O áudio vai se desencontrando aos poucos, porque o relógio da placa de som não bate exatamente com o do sistema.

## Decisão

Áudio como relógio mestre.

- O leitor de áudio vai direto para o dispositivo (`MediaFoundationReader` → `WaveOutEvent`), sem o buffer intermediário.
- O relógio é a posição que o dispositivo já tocou (`WaveOutEvent.GetPosition()`), ou seja, o que se ouve, e não o que foi lido do arquivo.
- `IPlaybackClock` tem duas implementações: o `NAudioPlayer` e um `StopwatchClock`, usado sem áudio (`--no-audio`, fora do Windows ou quando o áudio não abre).
- Se o áudio acaba antes do vídeo, o relógio continua a partir da última posição com um cronômetro, para o vídeo não congelar.
- A cada volta, o player lê o relógio e mostra o quadro daquele instante. Se estiver adiantado, espera. Se estiver atrasado, pula os quadros que ficaram para trás sem converter nem desenhar.
- O padrão de FPS passa a ser o do vídeo. A metade só existia na v1 por desempenho, e o ADR 0006 mostrou que 30 fps sobra.

## Consequências

Medido com um vídeo de teste que tem um flash branco e um bipe juntos a cada segundo:

| Cenário | Desenhados | Pulados | Erro médio | Erro máximo |
| --- | --- | --- | --- | --- |
| Com áudio | 300 | 0 | 0,4 ms | 5 ms |
| Com áudio e terminal lento (+80 ms por quadro) | 118 | 182 | 17 ms | 33 ms |
| Sem áudio | 298 | 2 | 9 ms | 30 ms |
| 4 s de áudio num vídeo de 10 s | 300 | 0 | 0,3 ms | 13 ms |

- O erro fica abaixo de um quadro (33 ms a 30 fps), inclusive com o terminal lento.
- Num terminal lento, a imagem dá saltos, mas não perde a sincronia.
- O relógio do áudio fica no Windows enquanto o áudio for NAudio. O item de multiplataforma troca só a implementação do `IAudioPlayer`.
