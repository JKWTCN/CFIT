using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.Tasks;
using Localization = CFIT.Installer.UI.Localization;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static CFIT.Installer.LibFunc.FuncVersion;

namespace CFIT.Installer.LibFunc
{
    public static class FuncNet
    {
        public static readonly Regex netCore = new Regex(@"Microsoft.NETCore.App (\d+\.\d+\.\d+).+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex netDesktop = new Regex(@"Microsoft.WindowsDesktop.App (\d+\.\d+\.\d+).+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool CheckDotNet(string netVersion, bool desktop, bool majorEqual)
        {
            try
            {
                bool isNetInstalled = false;

                string cmd = "dotnet --list-runtimes";
                bool cmdResult = Sys.RunCommand(cmd, out string strOutput);
                if (!cmdResult)
                {
                    TaskStore.CurrentTask.SetError(Localization.Translate("The Command '{0}' returned a non-zero Exit Code!", cmd));
                    return false;
                }

                string[] output = strOutput.Split(Environment.NewLine.ToCharArray());
                Logger.Debug($"Received {output?.Length} Output Lines");
                foreach (var line in output)
                {
                    Match match;
                    if (desktop)
                        match = netDesktop.Match(line);
                    else
                        match = netCore.Match(line);

                    if (match?.Groups?.Count == 2 && !string.IsNullOrWhiteSpace(match?.Groups[1]?.Value))
                        isNetInstalled = CheckVersion(match.Groups[1].Value, VersionCompare.GREATER_EQUAL, netVersion, out bool compareable, majorEqual) && compareable;
                    if (isNetInstalled)
                        break;
                }

                return isNetInstalled;
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
                return false;
            }
        }

        public static Task<string> DownloadNetRuntime(CancellationToken token, string url, string file, int checkInterval = 250, string workdir = "")
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
            httpClient.DefaultRequestHeaders.Add("Referer", "\r\nhttps://dotnet.microsoft.com/");
            httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Microsoft Edge\";v=\"129\", \"Not=A?Brand\";v=\"8\", \"Chromium\";v=\"129\"");
            httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
            httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
            httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform-Version", "\"10.0.0\"");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-site");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36 Edg/129.0.0.0");

            return FuncIO.DownloadFile(token, url, file, httpClient, checkInterval, workdir);
        }
    }
}
