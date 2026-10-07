import { dotnet } from "./_framework/dotnet.js";

const $ = id => document.getElementById(id);
const palco = $("palco"), inicio = $("inicio"), quadroDaTela = $("quadro"), tela = $("tela"), video = $("video");
const controles = $("controles");
const desenhista = criarDesenhista(tela);

// O quadro do vídeo é desenhado num canvas do tamanho certo para o estilo, e os pixels vão para o C#.
const amostra = document.createElement("canvas");
const contextoDaAmostra = amostra.getContext("2d", { willReadFrequently: true });

let quadro = null, ultimoTempo = -1, redesenhar = false, enderecoDoVideo = null;
let estilo = "ascii", comCor = true, aoVivo = false, cameras = [], cameraAtual = 0;

const runtime = await dotnet.create();
const nucleo = (await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName)).AsciiVideoPlayer.Web.WebPlayer;
$("carregando").hidden = true;

function formatar(segundos) {
  if (!Number.isFinite(segundos)) segundos = 0;
  const total = Math.floor(segundos), horas = Math.floor(total / 3600), minutos = Math.floor(total / 60) % 60;
  const resto = String(total % 60).padStart(2, "0");
  return horas > 0 ? `${horas}:${String(minutos).padStart(2, "0")}:${resto}` : `${minutos}:${resto}`;
}

// Os ícones e as barras passam pelo mesmo conversor do vídeo, no estilo escolhido.
const imagensDosIcones = new Map();

function imagemDoIcone(nome) {
  if (!imagensDosIcones.has(nome)) {
    // Traço mais grosso que o da barra de título: reduzido a poucas letras, o traço fino quase some.
    const forma = $(`i-${nome}`).innerHTML.replaceAll('stroke-width="2"', 'stroke-width="3.4"');
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="96" height="96"><g color="#fff" style="color:#fff">${forma}</g></svg>`;
    const imagem = new Image();
    imagem.src = "data:image/svg+xml;charset=utf-8," + encodeURIComponent(svg);
    imagensDosIcones.set(nome, imagem.decode().then(() => imagem));
  }
  return imagensDosIcones.get(nome);
}

function pixels(largura, altura, pintar) {
  const canvas = document.createElement("canvas");
  canvas.width = largura;
  canvas.height = altura;
  const contexto = canvas.getContext("2d", { willReadFrequently: true });
  contexto.fillStyle = "#000";
  contexto.fillRect(0, 0, largura, altura);
  pintar(contexto);
  return new Uint8Array(contexto.getImageData(0, 0, largura, altura).data.buffer);
}

async function desenharIcone(elemento) {
  const nome = elemento.dataset.icone;
  const imagem = await imagemDoIcone(nome);
  const colorido = nome === "cor" && comCor;
  const forma = document.createElement("canvas");
  forma.width = forma.height = 96;
  const contextoDaForma = forma.getContext("2d");
  contextoDaForma.drawImage(imagem, 0, 0, 96, 96);
  if (colorido) {
    const arco = contextoDaForma.createLinearGradient(0, 0, 96, 96);
    ["#ff5f5f", "#ffd75f", "#5fff87", "#5fd7ff", "#d75fff"].forEach((cor, i, cores) => arco.addColorStop(i / (cores.length - 1), cor));
    contextoDaForma.globalCompositeOperation = "source-atop";
    contextoDaForma.fillStyle = arco;
    contextoDaForma.fillRect(0, 0, 96, 96);
  }
  const rgba = pixels(96, 96, contexto => contexto.drawImage(forma, 0, 0));
  // Realça os meios-tons, para a borda do traço virar letra densa e não ponto.
  for (let i = 0; i < rgba.length; i++) rgba[i] = Math.round(Math.sqrt(rgba[i] / 255) * 255);
  const grande = elemento.classList.contains("grande");
  const [colunas, linhas, tamanho] = grande ? [14, 7, 60] : [12, 6, 48];
  const estiloDoIcone = elemento.dataset.estilo ?? estilo;
  const celulas = nucleo.ConvertImage(estiloDoIcone, colorido, rgba, 96, 96, colunas, linhas);
  let canvas = elemento.querySelector("canvas");
  if (!canvas) elemento.prepend(canvas = document.createElement("canvas"));
  desenharPequeno(canvas, celulas, colunas, linhas, tamanho, tamanho);
}

function desenharIcones() {
  for (const elemento of document.querySelectorAll("[data-icone]")) desenharIcone(elemento);
}

function trocarIcone(elemento, nome) {
  if (elemento.dataset.icone === nome) return;
  elemento.dataset.icone = nome;
  desenharIcone(elemento);
}

// Barra deslizante desenhada em caracteres: trilho fino, parte cheia mais grossa e o botão redondo.
function criarBarra(elemento, { min, max, valor, passo = 1, aoMudar }) {
  const canvas = document.createElement("canvas");
  elemento.append(canvas);
  elemento.tabIndex = 0;
  elemento.setAttribute("role", "slider");
  elemento.setAttribute("aria-valuemin", min);
  elemento.setAttribute("aria-valuemax", max);

  const barra = {
    valor,
    arrastando: false,
    definir(novo, avisar = false) {
      barra.valor = Math.min(max, Math.max(min, novo));
      elemento.setAttribute("aria-valuenow", Math.round(barra.valor));
      barra.desenhar();
      if (avisar) aoMudar?.(barra.valor);
    },
    desenhar() {
      const largura = elemento.clientWidth, altura = 14;
      if (!largura) return;
      const colunas = Math.max(4, Math.floor(largura / 6));
      const imagemLargura = colunas * 8, imagemAltura = 24;
      const fracao = (barra.valor - min) / (max - min || 1);
      const x = 6 + fracao * (imagemLargura - 12);
      const rgba = pixels(imagemLargura, imagemAltura, contexto => {
        contexto.fillStyle = "#555";
        contexto.fillRect(0, 10, imagemLargura, 4);
        contexto.fillStyle = "#fff";
        contexto.fillRect(0, 8, x, 8);
        contexto.beginPath();
        contexto.arc(x, 12, 10, 0, 2 * Math.PI);
        contexto.fill();
      });
      const celulas = nucleo.ConvertImage(estilo, false, rgba, imagemLargura, imagemAltura, colunas, 1);
      desenharPequeno(canvas, celulas, colunas, 1, colunas * 6, altura);
    },
  };

  const valorNoPonto = evento => {
    const caixa = elemento.getBoundingClientRect();
    return min + Math.min(1, Math.max(0, (evento.clientX - caixa.left) / caixa.width)) * (max - min);
  };
  elemento.addEventListener("pointerdown", evento => {
    barra.arrastando = true;
    elemento.setPointerCapture(evento.pointerId);
    barra.definir(valorNoPonto(evento), true);
  });
  elemento.addEventListener("pointermove", evento => { if (barra.arrastando) barra.definir(valorNoPonto(evento), true); });
  elemento.addEventListener("pointerup", () => { barra.arrastando = false; });
  elemento.addEventListener("keydown", evento => {
    const mudanca = { ArrowRight: passo, ArrowUp: passo, ArrowLeft: -passo, ArrowDown: -passo }[evento.key];
    if (mudanca === undefined) return;
    evento.preventDefault();
    evento.stopPropagation();
    barra.definir(barra.valor + mudanca, true);
  });
  new ResizeObserver(() => barra.desenhar()).observe(elemento);
  barra.definir(valor);
  return barra;
}

const posicao = criarBarra($("posicao"), {
  min: 0, max: 1000, valor: 0, passo: 10,
  aoMudar: valor => { if (video.duration) video.currentTime = valor / 1000 * video.duration; },
});

const volume = criarBarra($("volume"), {
  min: 0, max: 100, valor: 100, passo: 5,
  aoMudar: valor => {
    video.volume = valor / 100;
    video.muted = valor === 0;
    trocarIcone($("mudo"), video.muted ? "mudo" : "som");
  },
});

const colunas = criarBarra($("colunas"), {
  min: 1, max: 500, valor: innerWidth < 700 ? 80 : 120, passo: 1,
  aoMudar: valor => {
    $("colunas-valor").value = Math.round(valor);
    configurar();
  },
});
$("colunas-valor").value = Math.round(colunas.valor);

function desenharTudo() {
  desenharIcones();
  for (const barra of [posicao, volume, colunas]) barra.desenhar();
}

desenharTudo();

function marcar(botao, ligado) {
  botao.classList.toggle("ativo", ligado);
  botao.setAttribute("aria-pressed", ligado);
}

function configurar() {
  if (!video.videoWidth) return;
  const [cols, linhas, largura, altura] = nucleo.Configure(estilo, comCor, 1, video.videoWidth, video.videoHeight, Math.round(colunas.valor));
  amostra.width = largura;
  amostra.height = altura;
  quadro = { colunas: cols, linhas };
  ajustar();
}

function ajustar() {
  if (!quadro) return;
  desenhista.ajustar(quadro.colunas, quadro.linhas, palco.clientWidth - 16, palco.clientHeight - 16);
  redesenhar = true;
}

function desenhar() {
  if (quadro && (redesenhar || aoVivo || video.currentTime !== ultimoTempo)) {
    ultimoTempo = video.currentTime;
    redesenhar = false;
    contextoDaAmostra.imageSmoothingQuality = "high";
    // A câmera sai espelhada, como num espelho.
    contextoDaAmostra.setTransform(aoVivo ? -1 : 1, 0, 0, 1, aoVivo ? amostra.width : 0, 0);
    contextoDaAmostra.drawImage(video, 0, 0, amostra.width, amostra.height);
    const rgba = contextoDaAmostra.getImageData(0, 0, amostra.width, amostra.height).data;
    desenhista.desenhar(nucleo.Convert(new Uint8Array(rgba.buffer)));
    desenhista.legenda(aoVivo ? "" : nucleo.SubtitleAt(video.currentTime));
  }
  requestAnimationFrame(desenhar);
}

function mostrarPlayer(nome) {
  inicio.hidden = true;
  quadroDaTela.hidden = controles.hidden = $("fechar").hidden = false;
  $("titulo").textContent = `C:\\ascii-video-player\\ascii-video-player.exe ${nome}`;
  $("linha-tempo").hidden = aoVivo;
  for (const elemento of document.querySelectorAll(".so-arquivo")) elemento.hidden = aoVivo;
  marcar($("camera"), aoVivo);
  $("trocar-camera").hidden = !aoVivo || cameras.length < 2;
  desenharTudo();
}

function pararCamera() {
  for (const trilha of video.srcObject?.getTracks() ?? []) trilha.stop();
  video.srcObject = null;
  aoVivo = false;
}

function abrir(endereco, nome, exemplo) {
  pararCamera();
  if (enderecoDoVideo) URL.revokeObjectURL(enderecoDoVideo);
  enderecoDoVideo = exemplo ? null : endereco;
  video.loop = exemplo;
  video.muted = volume.valor === 0;
  marcar($("repetir"), exemplo);
  video.src = endereco;
  video.dataset.nome = nome;
  $("credito").hidden = !exemplo;
  nucleo.LoadSubtitles("");
}

function abrirArquivo(arquivo) {
  if (arquivo) abrir(URL.createObjectURL(arquivo), arquivo.name, false);
}

async function abrirCamera(indice = 0) {
  try {
    const pedido = cameras[indice] ? { deviceId: { exact: cameras[indice].deviceId } } : true;
    const fluxo = await navigator.mediaDevices.getUserMedia({ video: pedido, audio: false });
    if (enderecoDoVideo) URL.revokeObjectURL(enderecoDoVideo);
    enderecoDoVideo = null;
    pararCamera();
    cameras = (await navigator.mediaDevices.enumerateDevices()).filter(dispositivo => dispositivo.kind === "videoinput");
    const atual = fluxo.getVideoTracks()[0]?.getSettings().deviceId;
    cameraAtual = Math.max(0, cameras.findIndex(camera => camera.deviceId === atual));
    aoVivo = true;
    video.removeAttribute("src");
    video.loop = false;
    video.muted = true;
    video.dataset.nome = "--camera";
    video.srcObject = fluxo;
    $("credito").hidden = true;
  } catch {
    $("carregando").hidden = false;
    $("carregando").textContent = "Não foi possível abrir a câmera. Confira se o navegador tem permissão para usá-la.";
  }
}

function fechar() {
  pararCamera();
  video.pause();
  video.removeAttribute("src");
  video.load();
  quadro = null;
  quadroDaTela.hidden = controles.hidden = $("fechar").hidden = true;
  inicio.hidden = false;
  $("titulo").textContent = "C:\\ascii-video-player\\ascii-video-player.exe";
}

function alternar() {
  if (video.paused) video.play(); else video.pause();
}

function pular(segundos) {
  if (aoVivo) return;
  video.currentTime = Math.min(Math.max(0, video.currentTime + segundos), video.duration || 0);
}

function trocarSom() {
  if (aoVivo) return;
  video.muted = !video.muted;
  trocarIcone($("mudo"), video.muted || video.volume === 0 ? "mudo" : "som");
}

video.addEventListener("loadedmetadata", () => {
  $("carregando").hidden = true;
  mostrarPlayer(video.dataset.nome);
  $("duracao").textContent = formatar(video.duration);
  configurar();
  video.play();
});

video.addEventListener("error", () => {
  if (!video.getAttribute("src")) return;
  fechar();
  $("carregando").hidden = false;
  $("carregando").textContent = "O navegador não conseguiu abrir esse vídeo. Tente um MP4 (H.264) ou WebM.";
});

video.addEventListener("play", () => { trocarIcone($("tocar"), "pausar"); $("tocar").title = "Pausar (espaço)"; });
video.addEventListener("pause", () => { trocarIcone($("tocar"), "tocar"); $("tocar").title = "Tocar (espaço)"; redesenhar = true; });
video.addEventListener("seeked", () => { redesenhar = true; });
video.addEventListener("timeupdate", () => {
  $("tempo").textContent = formatar(video.currentTime);
  if (!posicao.arrastando && video.duration) posicao.definir(video.currentTime / video.duration * 1000);
});

$("arquivo").addEventListener("change", evento => { abrirArquivo(evento.target.files[0]); evento.target.value = ""; });
$("outro").addEventListener("change", evento => { abrirArquivo(evento.target.files[0]); evento.target.value = ""; });
$("exemplo").addEventListener("click", () => abrir("exemplo.mp4", "exemplo.mp4", true));
$("usar-camera").addEventListener("click", () => abrirCamera());
$("camera").addEventListener("click", () => aoVivo ? fechar() : abrirCamera());
$("trocar-camera").addEventListener("click", () => abrirCamera((cameraAtual + 1) % cameras.length));
$("fechar").addEventListener("click", fechar);
$("cheia").addEventListener("click", () => document.fullscreenElement ? document.exitFullscreen() : palco.requestFullscreen());

$("legenda").addEventListener("change", async evento => {
  const arquivo = evento.target.files[0];
  if (arquivo) nucleo.LoadSubtitles(await arquivo.text());
  evento.target.value = "";
  redesenhar = true;
});

$("tocar").addEventListener("click", alternar);
$("voltar").addEventListener("click", () => pular(-1));
$("avancar").addEventListener("click", () => pular(1));
$("repetir").addEventListener("click", () => { video.loop = !video.loop; marcar($("repetir"), video.loop); });
$("mudo").addEventListener("click", trocarSom);
tela.addEventListener("click", alternar);

for (const botao of document.querySelectorAll(".estilo")) {
  botao.addEventListener("click", () => {
    estilo = botao.dataset.estilo;
    for (const outro of document.querySelectorAll(".estilo")) outro.classList.toggle("ativo", outro === botao);
    configurar();
    desenharTudo();
  });
}

$("cor").addEventListener("click", () => {
  comCor = !comCor;
  marcar($("cor"), comCor);
  configurar();
  desenharIcone($("cor"));
});

addEventListener("resize", ajustar);
document.addEventListener("fullscreenchange", ajustar);

addEventListener("keydown", evento => {
  if (!quadro || evento.target.matches("input, [role=slider]")) return;
  const acoes = {
    Space: alternar,
    ArrowRight: () => pular(1),
    ArrowLeft: () => pular(-1),
    KeyF: () => $("cheia").click(),
    KeyM: trocarSom,
    KeyL: () => $("repetir").click(),
    KeyC: () => $("cor").click(),
  };
  if (acoes[evento.code]) { evento.preventDefault(); acoes[evento.code](); }
});

for (const tipo of ["dragenter", "dragover"])
  palco.addEventListener(tipo, evento => { evento.preventDefault(); palco.classList.add("arrastando"); });
palco.addEventListener("dragleave", () => palco.classList.remove("arrastando"));
palco.addEventListener("drop", evento => {
  evento.preventDefault();
  palco.classList.remove("arrastando");
  abrirArquivo([...evento.dataTransfer.files].find(item => item.type.startsWith("video/")));
});

requestAnimationFrame(desenhar);
