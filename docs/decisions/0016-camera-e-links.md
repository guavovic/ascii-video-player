# 16. Câmera e links

Data: 06/10/2026

Status: Aceito

## Contexto

O player só abria arquivos. Faltavam a webcam ao vivo (#32) e links: um endereço direto de vídeo ou a página de um site de vídeo. Os três chegam ao FFmpeg de jeitos diferentes: arquivo e link direto vão no `-i`, a câmera precisa do formato de entrada de cada sistema, e a página de um site precisa ser resolvida antes.

Cuidado com a câmera: abrir uma câmera às cegas (a padrão, ou a primeira da lista) pode ligar uma câmera virtual, como a do celular pelo Link com o Windows, sem a pessoa pedir.

## Decisão

- **`MediaInput`**: o que o FFmpeg abre, com nome, endereço e argumentos de entrada. A leitura do vídeo, o áudio e o `ffprobe` recebem a entrada, e não mais um caminho.
- **Câmera pelo nome, nunca às cegas.** `--list-cameras` só lista os nomes, sem abrir nenhuma, e `--camera "nome"` abre exatamente aquela: DirectShow no Windows, AVFoundation no macOS e V4L2 no Linux (onde o nome é o `/dev/videoN`). A imagem sai espelhada, como num espelho, que é o que se espera de uma webcam.
- **Ao vivo não pausa nem pula.** Sem duração não há para onde pular, e pausar só acumularia atraso. Esc continua saindo. O `--export` também não aceita a câmera, porque ela não tem fim.
- **Link direto** (http, https, rtsp, rtmp) vai direto para o FFmpeg, inclusive com `--start` e as setas.
- **Site de vídeo pelo yt-dlp, se estiver instalado.** Quando o FFmpeg não abre o link, o yt-dlp devolve o título e os endereços diretos. Os sites costumam entregar imagem e som separados, então a entrada aceita um endereço só para o som. 480p já sobra, porque a imagem vira poucas colunas no terminal. Sem o yt-dlp, a mensagem de erro diz para instalar.

## Consequências

- O yt-dlp fica opcional: quem não usa links de sites não precisa instalar nada a mais.
- Os endereços do yt-dlp expiram depois de algumas horas, o que não atrapalha um vídeo sendo assistido.
