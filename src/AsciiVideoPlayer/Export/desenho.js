// Desenha num canvas as células codificadas pelo CellEncoding (9 bytes cada: caractere, cor da letra,
// se tem fundo e cor do fundo). A página exportada e o player no navegador usam este mesmo arquivo.
//
// Com WebGL, a placa de vídeo pinta todas as células de uma vez: as letras ficam desenhadas uma única vez
// numa imagem (o atlas), e cada quadro só envia duas texturas pequenas com as cores e a letra de cada célula.
// Sem WebGL, cai para o canvas 2D, juntando as letras vizinhas da mesma cor num texto só.
const bytesPorCelula = 9;
const proporcaoDaLetra = 0.6;
const alturaDaLinha = 1.2;
const fonteMonoespacada = '"Cascadia Mono", Consolas, Menlo, "DejaVu Sans Mono", monospace';
const corDoFundo = [12, 12, 12];

// Braille: bit de cada ponto como [coluna, linha] dentro da célula de 2×4.
const posicoesBraille = [[0, 0], [0, 1], [0, 2], [1, 0], [1, 1], [1, 2], [0, 3], [1, 3]];

function criarDesenhista(tela) {
  const camada = document.createElement("canvas");
  camada.style.cssText = "grid-area: 1 / 1; pointer-events: none;";
  tela.style.gridArea = "1 / 1";
  tela.after(camada);
  const legendas = camada.getContext("2d");
  const gl = tela.getContext("webgl2", { alpha: false, antialias: false, depth: false });
  const pintor = gl ? criarPintorWebGl(gl) : criarPintor2d(tela.getContext("2d"));
  let medida = null, ultimaLegenda = null;

  return {
    camada,
    usaWebGl: !!gl,

    // Ajusta o canvas ao espaço disponível, mantendo a proporção da letra.
    ajustar(colunas, linhas, larguraDisponivel, alturaDisponivel) {
      const fonte = Math.max(1, Math.min(larguraDisponivel / (colunas * proporcaoDaLetra), alturaDisponivel / (linhas * alturaDaLinha)));
      const escala = devicePixelRatio || 1;
      const largura = fonte * proporcaoDaLetra, altura = fonte * alturaDaLinha;
      for (const canvas of [tela, camada]) {
        canvas.width = Math.max(1, Math.round(colunas * largura * escala));
        canvas.height = Math.max(1, Math.round(linhas * altura * escala));
        canvas.style.width = colunas * largura + "px";
        canvas.style.height = linhas * altura + "px";
      }
      medida = { colunas, linhas, largura, altura, fonte, escala };
      pintor.ajustar(medida);
      ultimaLegenda = null;
    },

    desenhar(celulas, inicio = 0) {
      if (medida) pintor.desenhar(celulas, inicio, medida);
    },

    legenda(texto) {
      texto = texto || "";
      if (!medida || texto === ultimaLegenda) return;
      ultimaLegenda = texto;
      const { colunas, linhas, largura, altura, escala } = medida;
      legendas.setTransform(escala, 0, 0, escala, 0, 0);
      legendas.clearRect(0, 0, colunas * largura, linhas * altura);
      if (!texto) return;
      const partes = texto.split("\n"), tamanho = Math.max(12, altura * 1.3);
      legendas.font = `bold ${tamanho}px system-ui, sans-serif`;
      legendas.textAlign = "center";
      legendas.textBaseline = "middle";
      partes.forEach((parte, i) => {
        const x = colunas * largura / 2, y = linhas * altura - (partes.length - i) * tamanho * 1.35;
        const caixa = legendas.measureText(parte).width + tamanho;
        legendas.fillStyle = "rgba(0, 0, 0, 0.75)";
        legendas.fillRect(x - caixa / 2, y - tamanho * 0.65, caixa, tamanho * 1.3);
        legendas.fillStyle = "#ffffff";
        legendas.fillText(parte, x, y);
      });
    },
  };
}

// Índice de cada caractere no atlas: ASCII visível, os 256 braille, os blocos e o que mais aparecer.
function criarTabelaDeLetras() {
  const letras = [];
  for (let codigo = 32; codigo < 127; codigo++) letras.push(codigo);
  for (let codigo = 0x2800; codigo <= 0x28FF; codigo++) letras.push(codigo);
  letras.push(0x2580, 0x2584, 0x2588);
  const extras = new Map(letras.slice(95 + 256).map((codigo, i) => [codigo, 95 + 256 + i]));
  let mudou = true;

  return {
    letras,
    get mudou() { return mudou; },
    desenhado() { mudou = false; },
    indice(codigo) {
      if (codigo >= 32 && codigo < 127) return codigo - 32;
      if (codigo >= 0x2800 && codigo <= 0x28FF) return 95 + codigo - 0x2800;
      let indice = extras.get(codigo);
      if (indice === undefined) {
        indice = letras.length;
        letras.push(codigo);
        extras.set(codigo, indice);
        mudou = true;
      }
      return indice;
    },
  };
}

function desenharLetra(contexto, codigo, x, y, largura, altura) {
  if (codigo === 32) return;
  if (codigo === 0x2580) contexto.fillRect(x, y, largura, altura / 2);
  else if (codigo === 0x2584) contexto.fillRect(x, y + altura / 2, largura, altura / 2);
  else if (codigo === 0x2588) contexto.fillRect(x, y, largura, altura);
  else if (codigo > 0x2800 && codigo <= 0x28FF) desenharPontos(contexto, codigo - 0x2800, x, y, largura, altura);
  else if (codigo !== 0x2800) contexto.fillText(String.fromCodePoint(codigo), x + largura / 2, y + altura / 2);
}

function desenharPontos(contexto, bits, x, y, largura, altura) {
  const raio = Math.max(0.5, Math.min(largura / 2, altura / 4) * 0.4);
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

function criarPintorWebGl(gl) {
  const vertices = `#version 300 es
    in vec2 posicao;
    out vec2 coordenada;
    void main() {
      coordenada = vec2(posicao.x + 1.0, 1.0 - posicao.y) * 0.5;
      gl_Position = vec4(posicao, 0.0, 1.0);
    }`;
  const fragmentos = `#version 300 es
    precision highp float;
    uniform sampler2D frente, fundo, atlas;
    uniform vec2 grade, gradeDoAtlas;
    uniform vec3 corDoFundo;
    in vec2 coordenada;
    out vec4 cor;
    void main() {
      vec2 posicao = coordenada * grade;
      ivec2 celula = ivec2(min(floor(posicao), grade - 1.0));
      vec4 letra = texelFetch(frente, celula, 0);
      vec4 base = texelFetch(fundo, celula, 0);
      float alto = floor(base.a * 255.0 + 0.5);
      float indice = floor(letra.a * 255.0 + 0.5) + mod(alto, 128.0) * 256.0;
      vec2 lugar = vec2(mod(indice, gradeDoAtlas.x), floor(indice / gradeDoAtlas.x));
      float tinta = texture(atlas, (lugar + fract(posicao)) / gradeDoAtlas).r;
      vec3 atras = alto >= 128.0 ? base.rgb : corDoFundo;
      cor = vec4(mix(atras, letra.rgb, tinta), 1.0);
    }`;

  const programa = gl.createProgram();
  for (const [tipo, codigo] of [[gl.VERTEX_SHADER, vertices], [gl.FRAGMENT_SHADER, fragmentos]]) {
    const shader = gl.createShader(tipo);
    gl.shaderSource(shader, codigo);
    gl.compileShader(shader);
    gl.attachShader(programa, shader);
  }
  gl.linkProgram(programa);
  gl.useProgram(programa);

  gl.bindBuffer(gl.ARRAY_BUFFER, gl.createBuffer());
  gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]), gl.STATIC_DRAW);
  const atributo = gl.getAttribLocation(programa, "posicao");
  gl.enableVertexAttribArray(atributo);
  gl.vertexAttribPointer(atributo, 2, gl.FLOAT, false, 0, 0);

  const uniforme = nome => gl.getUniformLocation(programa, nome);
  gl.uniform3f(uniforme("corDoFundo"), ...corDoFundo.map(valor => valor / 255));

  function textura(unidade, nome) {
    const tex = gl.createTexture();
    gl.activeTexture(gl.TEXTURE0 + unidade);
    gl.bindTexture(gl.TEXTURE_2D, tex);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    gl.uniform1i(uniforme(nome), unidade);
    return tex;
  }

  const texFrente = textura(0, "frente"), texFundo = textura(1, "fundo"), texAtlas = textura(2, "atlas");
  const tabela = criarTabelaDeLetras();
  const atlas = document.createElement("canvas");
  const contextoDoAtlas = atlas.getContext("2d");
  const colunasDoAtlas = 32;
  let frente = new Uint8Array(0), fundo = new Uint8Array(0), tile = { largura: 1, altura: 1 };

  function desenharAtlas() {
    const linhasDoAtlas = Math.ceil(tabela.letras.length / colunasDoAtlas);
    atlas.width = colunasDoAtlas * tile.largura;
    atlas.height = linhasDoAtlas * tile.altura;
    contextoDoAtlas.fillStyle = "#000";
    contextoDoAtlas.fillRect(0, 0, atlas.width, atlas.height);
    contextoDoAtlas.fillStyle = "#fff";
    contextoDoAtlas.font = `${tile.altura / alturaDaLinha}px ${fonteMonoespacada}`;
    contextoDoAtlas.textAlign = "center";
    contextoDoAtlas.textBaseline = "middle";
    tabela.letras.forEach((codigo, i) => {
      desenharLetra(contextoDoAtlas, codigo, (i % colunasDoAtlas) * tile.largura, Math.floor(i / colunasDoAtlas) * tile.altura, tile.largura, tile.altura);
    });
    gl.activeTexture(gl.TEXTURE2);
    gl.bindTexture(gl.TEXTURE_2D, texAtlas);
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, atlas);
    gl.uniform2f(uniforme("gradeDoAtlas"), colunasDoAtlas, linhasDoAtlas);
    tabela.desenhado();
  }

  return {
    ajustar({ colunas, linhas, largura, altura, escala }) {
      gl.viewport(0, 0, gl.drawingBufferWidth, gl.drawingBufferHeight);
      gl.uniform2f(uniforme("grade"), colunas, linhas);
      tile = { largura: Math.max(2, Math.round(largura * escala)), altura: Math.max(2, Math.round(altura * escala)) };
      frente = new Uint8Array(colunas * linhas * 4);
      fundo = new Uint8Array(colunas * linhas * 4);
      for (const [unidade, tex] of [[0, texFrente], [1, texFundo]]) {
        gl.activeTexture(gl.TEXTURE0 + unidade);
        gl.bindTexture(gl.TEXTURE_2D, tex);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, colunas, linhas, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
      }
      desenharAtlas();
    },

    desenhar(celulas, inicio, { colunas, linhas }) {
      for (let i = 0, c = inicio, total = colunas * linhas * 4; i < total; i += 4, c += bytesPorCelula) {
        const indice = tabela.indice(celulas[c] | celulas[c + 1] << 8);
        frente[i] = celulas[c + 2];
        frente[i + 1] = celulas[c + 3];
        frente[i + 2] = celulas[c + 4];
        frente[i + 3] = indice & 255;
        fundo[i] = celulas[c + 6];
        fundo[i + 1] = celulas[c + 7];
        fundo[i + 2] = celulas[c + 8];
        fundo[i + 3] = indice >> 8 | (celulas[c + 5] ? 128 : 0);
      }
      if (tabela.mudou) desenharAtlas();
      gl.activeTexture(gl.TEXTURE0);
      gl.bindTexture(gl.TEXTURE_2D, texFrente);
      gl.texSubImage2D(gl.TEXTURE_2D, 0, 0, 0, colunas, linhas, gl.RGBA, gl.UNSIGNED_BYTE, frente);
      gl.activeTexture(gl.TEXTURE1);
      gl.bindTexture(gl.TEXTURE_2D, texFundo);
      gl.texSubImage2D(gl.TEXTURE_2D, 0, 0, 0, colunas, linhas, gl.RGBA, gl.UNSIGNED_BYTE, fundo);
      gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
    },
  };
}

function criarPintor2d(contexto) {
  return {
    ajustar({ fonte, escala }) {
      contexto.setTransform(escala, 0, 0, escala, 0, 0);
      contexto.font = `${fonte}px ${fonteMonoespacada}`;
      contexto.textAlign = "center";
      contexto.textBaseline = "middle";
    },

    desenhar(celulas, inicio, { colunas, linhas, largura, altura }) {
      contexto.fillStyle = `rgb(${corDoFundo})`;
      contexto.fillRect(0, 0, colunas * largura, linhas * altura);
      let cor = -1;
      for (let linha = 0, c = inicio; linha < linhas; linha++) {
        for (let coluna = 0; coluna < colunas; coluna++, c += bytesPorCelula) {
          const x = coluna * largura, y = linha * altura;
          if (celulas[c + 5]) {
            contexto.fillStyle = `rgb(${celulas[c + 6]},${celulas[c + 7]},${celulas[c + 8]})`;
            contexto.fillRect(x, y, largura + 0.5, altura + 0.5);
            cor = -1;
          }
          const codigo = celulas[c] | celulas[c + 1] << 8;
          if (codigo === 32) continue;
          const nova = celulas[c + 2] << 16 | celulas[c + 3] << 8 | celulas[c + 4];
          if (nova !== cor) {
            cor = nova;
            contexto.fillStyle = `rgb(${celulas[c + 2]},${celulas[c + 3]},${celulas[c + 4]})`;
          }
          desenharLetra(contexto, codigo, x, y, largura + 0.5, altura + 0.5);
        }
      }
    },
  };
}
