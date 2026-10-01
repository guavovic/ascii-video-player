# 5. Linha de comando com System.CommandLine

Data: 01/10/2026

Status: Aceito

## Contexto

O caminho do vídeo estava fixo no código, e a largura, o FPS e os caracteres eram constantes. Para tocar outro vídeo era preciso editar o código e compilar de novo.

## Opções consideradas

- **System.CommandLine** (2.0, estável desde o .NET 10). É da Microsoft e é a mesma que o `dotnet` usa. Traz ajuda, validação e mensagens de erro prontas, inclusive em pt-BR, e funciona com AOT.
- **ConsoleAppFramework**. Gera o código na compilação, sem reflection, e é a mais rápida. Menos conhecida.
- **Spectre.Console.Cli**. Ajuda mais bonita, mas ainda está em 0.x e usa reflection, o que atrapalha o AOT.
- **CommandLineParser**. Foi a mais popular, mas está sem versão nova desde 2022.
- **Ler `args` na mão**. Sem dependência, mas ajuda, validação e erros ficariam todos por nossa conta.

## Decisão

System.CommandLine, com:

- `video`: argumento obrigatório, e o arquivo precisa existir.
- `--width`, `-w`: largura em colunas. Padrão: metade da largura do vídeo, como antes.
- `--fps`, `-f`: quadros por segundo exibidos, até o FPS do vídeo. Padrão: metade do FPS do vídeo, como antes. Aceita só ponto como separador decimal (`29.97`), em qualquer idioma do sistema.
- `--palette`, `-p`: caracteres do mais escuro para o mais claro.
- `--no-audio`: toca só o vídeo.

As opções numéricas usam um parser próprio, que converte e valida no mesmo passo. Com um validador separado, um valor como `--width abc` derrubava o programa com exceção em vez de mostrar o erro.

O código de saída é 0 quando o vídeo toca até o fim ou quando a ajuda é pedida, e 1 em qualquer erro.

Na mesma mudança:

- O conversor passa a receber colunas e linhas em vez de passos em pixels, e a altura sai da largura e da proporção do vídeo. Com os valores padrão, um vídeo de tamanho par gera exatamente o mesmo texto de antes.
- O laço avançava três quadros por volta e esperava como se fossem dois, e o vídeo andava 1,5 vez mais rápido. Agora ele avança o número de quadros que o FPS pedido exige.
- Se o arquivo não informa o FPS, o player usa 30.

## Consequências

- Qualquer vídeo toca sem compilar de novo, e `--help` mostra como usar.
- A ajuda e os erros da própria biblioteca vêm em pt-BR. Duas frases dela continuam em inglês ("Description:" e a descrição do `--help`).
- A sincronia do áudio continua no item de áudio.
