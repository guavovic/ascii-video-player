import { dotnet } from "./_framework/dotnet.js";

const $ = id => document.getElementById(id);
const palco = $("palco"), inicio = $("inicio"), tela = $("tela"), video = $("video");
const controles = $("controles"), colunas = $("colunas"), posicao = $("posicao"), volume = $("volume");
const contexto = tela.getContext("2d");

// O quadro do vídeo é desenhado num canvas do tamanho certo para o estilo, e os pixels vão para o C#.
const amostra = document.createElement("canvas");
const contextoDaAmostra = amostra.getContext("2d", { willReadFrequently: true });

let nucleo, quadro = null, celula, ultimoTempo = -1, redesenhar = false, enderecoDoVideo = null, arrastandoPosicao = false;
let estilo = "ascii", comCor = true;

const runtime = await dotnet.create();
nucleo = (await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName)).AsciiVideoPlayer.Web.WebPlayer;
$("carregando").hidden = true;

// Em tela estreita, 120 colunas ficam miúdas demais.
if (innerWidth < 700) colunas.value = 80;

function formatar(segundos) {
  if (!Number.isFinite(segundos)) segundos = 0;
  const total = Math.floor(segundos), horas = Math.floor(total / 3600), minutos = Math.floor(total / 60) % 60;
  const resto = String(total % 60).padStart(2, "0");
  return horas > 0 ? `${horas}:${String(minutos).padStart(2, "0")}:${resto}` : `${minutos}:${resto}`;
}

function icone(botao, nome) {
  botao.querySelector("use").setAttribute("href", `#i-${nome}`);
}

function marcar(botao, ligado) {
  botao.classList.toggle("ativo", ligado);
  botao.setAttribute("aria-pressed", ligado);
}

function lerColunas() {
  const valor = Math.min(1000, Math.max(1, Math.round(+colunas.value) || 120));
  colunas.value = valor;
  return valor;
}

function configurar() {
  if (!video.videoWidth) return;
  const [cols, linhas, largura, altura] = nucleo.Configure(estilo, comCor, 1, video.videoWidth, video.videoHeight, lerColunas());
  amostra.width = largura;
  amostra.height = altura;
  quadro = { colunas: cols, linhas };
  ajustar();
}

function ajustar() {
  if (!quadro) return;
  celula = ajustarCanvas(tela, contexto, quadro.colunas, quadro.linhas, palco.clientWidth - 16, palco.clientHeight - 16);
  redesenhar = true;
}

function desenhar() {
  if (quadro && (redesenhar || video.currentTime !== ultimoTempo)) {
    ultimoTempo = video.currentTime;
    redesenhar = false;
    contextoDaAmostra.imageSmoothingQuality = "high";
    contextoDaAmostra.drawImage(video, 0, 0, amostra.width, amostra.height);
    const rgba = contextoDaAmostra.getImageData(0, 0, amostra.width, amostra.height).data;
    const celulas = nucleo.Convert(new Uint8Array(rgba.buffer));
    desenharCelulas(contexto, celulas, 0, quadro.colunas, quadro.linhas, celula);
    desenharLegenda(contexto, nucleo.SubtitleAt(video.currentTime), quadro.colunas, quadro.linhas, celula);
  }
  requestAnimationFrame(desenhar);
}

function abrir(endereco, nome, exemplo) {
  if (enderecoDoVideo) URL.revokeObjectURL(enderecoDoVideo);
  enderecoDoVideo = exemplo ? null : endereco;
  video.loop = exemplo;
  marcar($("repetir"), exemplo);
  video.src = endereco;
  $("titulo").textContent = `C:\\ascii-video-player\\ascii-video-player.exe ${nome}`;
  $("credito").hidden = !exemplo;
  nucleo.LoadSubtitles("");
}

function abrirArquivo(arquivo) {
  if (arquivo) abrir(URL.createObjectURL(arquivo), arquivo.name, false);
}

function fechar() {
  video.pause();
  video.removeAttribute("src");
  video.load();
  quadro = null;
  tela.hidden = controles.hidden = $("fechar").hidden = true;
  inicio.hidden = false;
  $("titulo").textContent = "C:\\ascii-video-player\\ascii-video-player.exe";
}

function alternar() {
  if (video.paused) video.play(); else video.pause();
}

function pular(segundos) {
  video.currentTime = Math.min(Math.max(0, video.currentTime + segundos), video.duration || 0);
}

function trocarSom() {
  video.muted = !video.muted;
  icone($("mudo"), video.muted || video.volume === 0 ? "mudo" : "som");
}

video.addEventListener("loadedmetadata", () => {
  inicio.hidden = true;
  tela.hidden = controles.hidden = $("fechar").hidden = false;
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

video.addEventListener("play", () => { icone($("tocar"), "pausar"); $("tocar").title = "Pausar (espaço)"; });
video.addEventListener("pause", () => { icone($("tocar"), "tocar"); $("tocar").title = "Tocar (espaço)"; redesenhar = true; });
video.addEventListener("seeked", () => { redesenhar = true; });
video.addEventListener("timeupdate", () => {
  $("tempo").textContent = formatar(video.currentTime);
  if (!arrastandoPosicao && video.duration) posicao.value = Math.round(video.currentTime / video.duration * 1000);
});

$("arquivo").addEventListener("change", evento => { abrirArquivo(evento.target.files[0]); evento.target.value = ""; });
$("outro").addEventListener("change", evento => { abrirArquivo(evento.target.files[0]); evento.target.value = ""; });
$("exemplo").addEventListener("click", () => abrir("exemplo.mp4", "exemplo.mp4", true));
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

volume.addEventListener("input", () => {
  video.volume = volume.value / 100;
  video.muted = video.volume === 0;
  icone($("mudo"), video.muted ? "mudo" : "som");
});

for (const botao of document.querySelectorAll(".estilo")) {
  botao.addEventListener("click", () => {
    estilo = botao.dataset.estilo;
    for (const outro of document.querySelectorAll(".estilo")) outro.classList.toggle("ativo", outro === botao);
    configurar();
  });
}

$("cor").addEventListener("click", () => { comCor = !comCor; marcar($("cor"), comCor); configurar(); });
colunas.addEventListener("change", configurar);

posicao.addEventListener("input", () => {
  arrastandoPosicao = true;
  if (video.duration) video.currentTime = posicao.value / 1000 * video.duration;
});
posicao.addEventListener("change", () => { arrastandoPosicao = false; });

addEventListener("resize", ajustar);
document.addEventListener("fullscreenchange", ajustar);

addEventListener("keydown", evento => {
  if (!quadro || evento.target.matches("input")) return;
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
