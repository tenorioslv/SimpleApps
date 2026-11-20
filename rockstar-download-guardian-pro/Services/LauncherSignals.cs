using System.Globalization;
using System.Text;
namespace RockstarGuardian.Services;
public static class LauncherSignals
{
    public static string Normalize(string text)
    {
        var result = new StringBuilder();
        foreach (char c in text.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) result.Append(char.ToLowerInvariant(c));
        return System.Text.RegularExpressions.Regex.Replace(result.ToString(), @"\s+", " ").Trim().TrimEnd('.', '!', '…').Trim();
    }
    public static bool IsRetry(string label) => Normalize(label) is "tentar novamente" or "tente novamente" or "retry" or "try again" or "repetir tentativa" or "tentar outra vez" or "retry download" or "repetir download";
    public static bool IsResume(string label) => Normalize(label) is "continuar" or "resume" or "resume download" or "retomar" or "retomar download" or "continuar download";
    public static bool IsDownloadActive(string label) => Normalize(label) is "pausar download" or "pause download" or "baixando" or "downloading";
    public static bool ShouldRecover(bool error, bool retry, bool downloading, bool paused, System.TimeSpan inactivity)
        => retry || error || (downloading && !paused && inactivity >= System.TimeSpan.FromSeconds(25));
}
