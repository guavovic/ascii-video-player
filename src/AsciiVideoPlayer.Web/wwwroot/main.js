import { dotnet } from "./_framework/dotnet.js";

const $ = id => document.getElementById(id);
const palco = $("palco"), inicio = $("inicio"), tela = $("tela"), video = $("video");
const controles = $("controles"), estilo = $("estilo"), colunas = $("colunas"), cor = $("cor");
const posicao = $("posicao"), tempo = $("tempo"), tocar = $("tocar");
const contexto = tela.getContext("2d");

// O quadro do vídeo é desenhado num canvas do tamanho certo para o estilo, e os pixels vão para o C#.
const amostra = document.createElement("canvas");
const contextoDaAmostra = amostra.getContext("2d", { willReadFrequently: true });

let nucleo, quadro = null, celula, ultimoTempo = -1, redesenhar = false, enderecoDoVideo = null, arrastandoPosicao = false;

const runtime = await dotnet.create();
nucleo = (await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName)).AsciiVideoPlayer.Web.WebPlayer;
$("carregando").hidden = true;

// Em tela estreita, 120 colunas ficam miúdas demais.
if (innerWidth < 700) colunas.value = $("colunas-valor").value = 80;

function formatar(segundos) {
  if (!Number.isFinite(segundos)) segundos = 0;
  const total = Math.floor(segundos), horas = Math.floor(total / 3600), minutos = Math.floor(total / 60) % 60;
  const resto = String(total % 60).padStart(2, "0");
  return horas > 0 ? `${horas}:${String(minutos).padStart(2, "0")}:${resto}` : `${minutos}:${resto}`;
}

function configurar() {
  if (!video.videoWidth) return;
  const [cols, linhas, largura, altura] = nucleo.Configure(estilo.value, cor.checked, 1, video.videoWidth, video.videoHeight, +colunas.value);
  amostra.width = largura;
  amostra.height = altura;
  quadro = { colunas: cols, linhas };
  ajustar();
}

function ajustar() {
  if (!quadro) return;
  const cheia = document.fullscreenElement === palco;
  const largura = cheia ? innerWidth : palco.clientWidth;
  const abaixo = controles.offsetHeight + $("credito").offsetHeight + 56;
  const altura = cheia ? innerHeight : Math.max(200, innerHeight - palco.getBoundingClientRect().top - abaixo);
  celula = ajustarCanvas(tela, contexto, quadro.colunas, quadro.linhas, largura, altura);
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

function abrir(endereco, exemplo) {
  if (enderecoDoVideo) URL.revokeObjectURL(enderecoDoVideo);
  enderecoDoVideo = exemplo ? null : endereco;
  video.loop = exemplo;
  video.src = endereco;
  $("credito").hidden = !exemplo;
  nucleo.LoadSubtitles("");
}

function fechar() {
  video.pause();
  video.removeAttribute("src");
  video.load();
  quadro = null;
  tela.hidden = controles.hidden = true;
  inicio.hidden = false;
  palco.classList.remove("tocando");
  $("credito").hidden = true;
}

function alternar() {
  if (video.paused) video.play(); else video.pause();
}

function pular(segundos) {
  video.currentTime = Math.min(Math.max(0, video.currentTime + segundos), video.duration || 0);
}

video.addEventListener("loadedmetadata", () => {
  inicio.hidden = true;
  tela.hidden = controles.hidden = false;
  palco.classList.add("tocando");
  configurar();
  video.play();
});

video.addEventListener("error", () => {
  if (!video.getAttribute("src")) return;
  fechar();
  $("carregando").hidden = false;
  $("carregando").textContent = "O navegador não conseguiu abrir esse vídeo. Tente um MP4 (H.264) ou WebM.";
});

video.addEventListener("play", () => { tocar.textContent = "pausar"; });
video.addEventListener("pause", () => { tocar.textContent = "tocar"; redesenhar = true; });
video.addEventListener("seeked", () => { redesenhar = true; });
video.addEventListener("timeupdate", () => {
  tempo.textContent = `${formatar(video.currentTime)} / ${formatar(video.duration)}`;
  if (!arrastandoPosicao && video.duration) posicao.value = Math.round(video.currentTime / video.duration * 1000);
});

$("arquivo").addEventListener("change", evento => {
  const arquivo = evento.target.files[0];
  if (arquivo) abrir(URL.createObjectURL(arquivo), false);
  evento.target.value = "";
});

$("exemplo").addEventListener("click", () => abrir("exemplo.mp4", true));
$("trocar").addEventListener("click", fechar);
$("cheia").addEventListener("click", () => document.fullscreenElement ? document.exitFullscreen() : palco.requestFullscreen());

$("legenda").addEventListener("change", async evento => {
  const arquivo = evento.target.files[0];
  if (arquivo) nucleo.LoadSubtitles(await arquivo.text());
  evento.target.value = "";
  redesenhar = true;
});

tocar.addEventListener("click", alternar);
tela.addEventListener("click", alternar);
estilo.addEventListener("change", configurar);
cor.addEventListener("change", configurar);
colunas.addEventListener("input", () => { $("colunas-valor").value = colunas.value; configurar(); });

posicao.addEventListener("input", () => {
  arrastandoPosicao = true;
  if (video.duration) video.currentTime = posicao.value / 1000 * video.duration;
});
posicao.addEventListener("change", () => { arrastandoPosicao = false; });

addEventListener("resize", ajustar);
document.addEventListener("fullscreenchange", ajustar);

addEventListener("keydown", evento => {
  if (!quadro || evento.target.matches("input[type=range], select")) return;
  if (evento.code === "Space") { evento.preventDefault(); alternar(); }
  else if (evento.code === "ArrowRight") pular(5);
  else if (evento.code === "ArrowLeft") pular(-5);
  else if (evento.code === "KeyF") $("cheia").click();
});

for (const tipo of ["dragenter", "dragover"])
  palco.addEventListener(tipo, evento => { evento.preventDefault(); palco.classList.add("arrastando"); });
palco.addEventListener("dragleave", () => palco.classList.remove("arrastando"));
palco.addEventListener("drop", evento => {
  evento.preventDefault();
  palco.classList.remove("arrastando");
  const arquivo = [...evento.dataTransfer.files].find(item => item.type.startsWith("video/"));
  if (arquivo) abrir(URL.createObjectURL(arquivo), false);
});

requestAnimationFrame(desenhar);
