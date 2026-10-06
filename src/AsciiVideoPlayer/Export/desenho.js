// Desenha num canvas as células codificadas pelo CellEncoding (9 bytes cada: caractere, cor da letra,
// se tem fundo e cor do fundo). A página exportada e o player no navegador usam este mesmo arquivo.
const bytesPorCelula = 9;
const proporcaoDaLetra = 0.6;
const alturaDaLinha = 1.2;

// Braille: bit de cada ponto como [coluna, linha] dentro da célula de 2×4.
const posicoesBraille = [[0, 0], [0, 1], [0, 2], [1, 0], [1, 1], [1, 2], [0, 3], [1, 3]];

// Ajusta o canvas ao espaço disponível e devolve o tamanho de cada célula.
function ajustarCanvas(tela, contexto, colunas, linhas, larguraDisponivel, alturaDisponivel) {
  const fonte = Math.max(2, Math.min(larguraDisponivel / (colunas * proporcaoDaLetra), alturaDisponivel / (linhas * alturaDaLinha)));
  const escala = devicePixelRatio || 1;
  const largura = fonte * proporcaoDaLetra, altura = fonte * alturaDaLinha;
  tela.width = Math.round(colunas * largura * escala);
  tela.height = Math.round(linhas * altura * escala);
  tela.style.width = colunas * largura + "px";
  tela.style.height = linhas * altura + "px";
  contexto.setTransform(escala, 0, 0, escala, 0, 0);
  contexto.font = fonte + 'px "Cascadia Mono", Consolas, Menlo, "DejaVu Sans Mono", monospace';
  contexto.textBaseline = "middle";
  return { largura, altura };
}

function desenharCelulas(contexto, celulas, inicio, colunas, linhas, { largura, altura }) {
  contexto.fillStyle = "#0c0c0c";
  contexto.fillRect(0, 0, colunas * largura, linhas * altura);
  for (let linha = 0, c = inicio; linha < linhas; linha++) {
    for (let coluna = 0; coluna < colunas; coluna++, c += bytesPorCelula) {
      const x = coluna * largura, y = linha * altura;
      if (celulas[c + 5]) {
        contexto.fillStyle = `rgb(${celulas[c + 6]},${celulas[c + 7]},${celulas[c + 8]})`;
        contexto.fillRect(x, y, largura + 0.5, altura + 0.5);
      }
      const letra = celulas[c] | celulas[c + 1] << 8;
      if (letra === 32) continue;
      contexto.fillStyle = `rgb(${celulas[c + 2]},${celulas[c + 3]},${celulas[c + 4]})`;
      if (letra === 0x2580) contexto.fillRect(x, y, largura + 0.5, altura / 2 + 0.5);
      else if (letra === 0x2584) contexto.fillRect(x, y + altura / 2, largura + 0.5, altura / 2 + 0.5);
      else if (letra === 0x2588) contexto.fillRect(x, y, largura + 0.5, altura + 0.5);
      else if (letra > 0x2800 && letra <= 0x28FF) desenharPontos(contexto, letra - 0x2800, x, y, largura, altura);
      else contexto.fillText(String.fromCharCode(letra), x, y + altura / 2);
    }
  }
}

function desenharPontos(contexto, bits, x, y, largura, altura) {
  const raio = Math.min(largura / 2, altura / 4) * 0.4;
  contexto.beginPath();
  for (let bit = 0; bit < 8; bit++) {
    if (!(bits & 1 << bit)) continue;
    const [coluna, linha] = posicoesBraille[bit];
    const cx = x + (coluna + 0.5) * largura / 2, cy = y + (linha + 0.5) * altura / 4;
    contexto.moveTo(cx + raio, cy);
    contexto.arc(cx, cy, raio, 0, 2 * Math.PI);
  }
  contexto.fill();
}

function desenharLegenda(contexto, texto, colunas, linhas, { largura, altura }) {
  if (!texto) return;
  const partes = texto.split("\n"), tamanho = Math.max(12, altura * 1.3), fonte = contexto.font;
  contexto.font = `bold ${tamanho}px system-ui, sans-serif`;
  contexto.textAlign = "center";
  partes.forEach((parte, i) => {
    const x = colunas * largura / 2, y = linhas * altura - (partes.length - i) * tamanho * 1.35;
    const caixa = contexto.measureText(parte).width + tamanho;
    contexto.fillStyle = "rgba(0, 0, 0, 0.75)";
    contexto.fillRect(x - caixa / 2, y - tamanho * 0.65, caixa, tamanho * 1.3);
    contexto.fillStyle = "#ffffff";
    contexto.fillText(parte, x, y);
  });
  contexto.font = fonte;
  contexto.textAlign = "start";
}
