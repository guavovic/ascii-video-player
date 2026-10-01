using System.Globalization;
using AsciiVideoPlayer.Cli;

CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");

return PlayerCommand.Create().Parse(args).Invoke();
