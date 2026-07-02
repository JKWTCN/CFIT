using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.Tasks;
using Localization = CFIT.Installer.UI.Localization;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CFIT.Installer.LibFunc
{
    public static class FuncFsuipc7
    {
        public static string[] Fsuipc7RegPaths { get; } = new string[]
        {
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\FSUIPC7",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\FSUIPC7Both",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\FSUIPC72024",

        };

        public static string Fsuipc7RegValueVersion { get; } = "DisplayVersion";
        public static string Fsuipc7RegValueDirectory { get; } = "InstallDir";
        public static string Fsuipc7Binary { get; } = "FSUIPC7";
        public static string Fsuipc7BinaryExe { get; } = $"{Fsuipc7Binary}.exe";

        public static string GetRegValueFsuipc7(string key, object defaultValue = null)
        {
            string result = "";
            foreach (string path in Fsuipc7RegPaths)
            {
                result = Sys.GetRegistryValue<string>(path, key, defaultValue);
                Logger.Debug($"Result for Key '{key}' on Path '{path}' - '{result}'");
                if (!string.IsNullOrWhiteSpace(result))
                    break;
            }

            return result;
        }

        public static bool IsRunning()
        {
            return Sys.GetProcessRunning(Fsuipc7Binary);
        }

        public static bool CheckVersion(string targetVersion, bool allowBeta = false, FuncVersion.VersionCompare compare = FuncVersion.VersionCompare.GREATER_EQUAL)
        {
            bool result = false;

            if (string.IsNullOrWhiteSpace(targetVersion))
                return result;

            try
            {
                string regVersion = GetRegValueFsuipc7(Fsuipc7RegValueVersion);

                if (!string.IsNullOrWhiteSpace(regVersion))
                {
                    Logger.Debug($"Comparing installed '{regVersion}' vs target '{targetVersion}'");
                    result = FuncVersion.CheckVersion(regVersion, compare, targetVersion, out bool compareable) && compareable;
                    if (result && !allowBeta && regVersion?.Contains("beta") == true)
                        result = false;
                }
                else
                    Logger.Information("FSUIPC7 not installed - could not get Version!");
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
            }

            return result;
        }

        public static string GetPathFsuipc7()
        {
            string result = "";

            try
            {
                string path = GetRegValueFsuipc7(Fsuipc7RegValueDirectory);
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                    result = path;
                else
                    Logger.Information("FSUIPC7 not installed - could not get Directory!");
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
            }

            return result;
        }

        public static bool CheckSettingsForPumps()
        {
            bool result = false;

            try
            {
                string fileContent = GetIniFileContent(out _);
                if (fileContent != null && fileContent.Contains("NumberOfPumps=0"))
                    result = true;
                else if (fileContent == null)
                    result = true;
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
            }

            return result;
        }

        public static string GetIniFileContent(out string regPath)
        {
            regPath = GetRegValueFsuipc7(Fsuipc7RegValueDirectory) + "\\" + "FSUIPC7.ini";

            if (File.Exists(regPath))
                return File.ReadAllText(regPath, Encoding.Default);
            else
                return null;
        }

        public static bool AutoStartAddUpdate(string binPath, string binFile)
        {
            bool result = false;

            try
            {
                string programParam = "READY";
                if (CheckVersion("7.4.0", true))
                    programParam = "CONNECTED";
                Logger.Debug($"Using programParam: '{programParam}'");
                string fileContent = GetIniFileContent(out string iniPath);
                if (fileContent == null)
                {
                    Logger.Debug($"FileContent empty! ({iniPath})");
                    TaskStore.CurrentTask.SetError(Localization.Translate("The File FSUIPC7.ini is empty!"));
                    return false;
                }

                if (!fileContent.Contains("[Programs]"))
                {
                    Logger.Debug($"Does not contain Programs Section");
                    fileContent += $"\r\n[Programs]\r\nRunIf1={programParam},CLOSE,{binPath}";
                    File.WriteAllText(iniPath, fileContent, Encoding.Default);
                }
                else
                {
                    Logger.Debug($"Searching Programs Section ...");
                    RegexOptions regOptions = RegexOptions.Compiled | RegexOptions.Multiline;
                    var runMatches = Regex.Matches(fileContent, @"[;]{0,1}Run(\d+).*", regOptions);
                    int lastRun = 0;
                    if (runMatches.Count > 0 && runMatches[runMatches.Count - 1].Groups.Count == 2)
                        lastRun = Convert.ToInt32(runMatches[runMatches.Count - 1].Groups[1].Value);

                    var runIfMatches = Regex.Matches(fileContent, @"[;]{0,1}RunIf(\d+).*", regOptions);
                    int lastRunIf = 0;
                    if (runIfMatches.Count > 0 && runIfMatches[runIfMatches.Count - 1].Groups.Count == 2)
                        lastRunIf = Convert.ToInt32(runIfMatches[runIfMatches.Count - 1].Groups[1].Value);

                    if (Regex.IsMatch(fileContent, @"^[;]{0,1}Run(\d+).*" + binFile, regOptions))
                    {
                        Logger.Debug($"Migrating to RunIf");
                        fileContent = Regex.Replace(fileContent, @"^[;]{0,1}Run(\d+).*" + binFile, $"RunIf{lastRunIf + 1}={programParam},CLOSE,{binPath}", regOptions);
                        File.WriteAllText(iniPath, fileContent, Encoding.Default);
                    }
                    else if (Regex.IsMatch(fileContent, @"^[;]{0,1}RunIf(\d+).*" + binFile, regOptions))
                    {
                        Logger.Debug($"Updating RunIf");
                        fileContent = Regex.Replace(fileContent, @"^[;]{0,1}RunIf(\d+).*" + binFile, $"RunIf$1={programParam},CLOSE,{binPath}", regOptions);
                        File.WriteAllText(iniPath, fileContent, Encoding.Default);
                    }
                    else
                    {
                        Logger.Debug($"Getting highest Index ...");
                        int index = -1;
                        if (runIfMatches.Count > 0 && runMatches.Count > 0)
                        {
                            index = runIfMatches[runIfMatches.Count - 1].Index + runIfMatches[runIfMatches.Count - 1].Length;
                            if (runMatches[runMatches.Count - 1].Index > runIfMatches[runIfMatches.Count - 1].Index)
                                index = runMatches[runMatches.Count - 1].Index + runMatches[runMatches.Count - 1].Length;
                        }
                        else if (runIfMatches.Count > 0)
                            index = runIfMatches[runIfMatches.Count - 1].Index + runIfMatches[runIfMatches.Count - 1].Length;
                        else if (runMatches.Count > 0)
                            index = runMatches[runMatches.Count - 1].Index + runMatches[runMatches.Count - 1].Length;

                        if (index > 0)
                        {
                            Logger.Debug($"Adding RunIf Index #{index}");
                            fileContent = fileContent.Insert(index + 1, $"RunIf{lastRunIf + 1}={programParam},CLOSE,{binPath}\r\n");
                            File.WriteAllText(iniPath, fileContent, Encoding.Default);
                        }
                        else
                        {
                            Logger.Debug($"Adding to empty Programs Section");
                            fileContent = Regex.Replace(fileContent, @"^\[Programs\]\r\n", $"[Programs]\r\nRunIf{lastRunIf + 1}={programParam},CLOSE,{binPath}\r\n", regOptions);
                            File.WriteAllText(iniPath, fileContent, Encoding.Default);
                        }
                    }

                    fileContent = GetIniFileContent(out iniPath);
                    result = fileContent.Contains(binFile);
                }
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
            }

            return result;
        }

        public static bool AutoStartRemove(string binFile)
        {
            bool result = false;

            try
            {
                string fileContent = GetIniFileContent(out string iniPath);
                if (fileContent == null)
                {
                    Logger.Debug($"FileContent empty!");
                    return true;
                }

                if (!fileContent.Contains("[Programs]") || !fileContent.Contains(binFile))
                {
                    Logger.Debug($"Does not contain Programs Section or binFile '{binFile}'");
                    result = true;
                }
                else
                {
                    RegexOptions regOptions = RegexOptions.Compiled | RegexOptions.Multiline;
                    if (Regex.IsMatch(fileContent, @"^[;]{0,1}Run(\d+).*" + binFile, regOptions))
                    {
                        Logger.Debug($"Removing Run Entry");
                        fileContent = Regex.Replace(fileContent, @"^[;]{0,1}Run(\d+).*" + binFile, $"", regOptions);
                        File.WriteAllText(iniPath, fileContent, Encoding.Default);
                    }
                    else if (Regex.IsMatch(fileContent, @"^[;]{0,1}RunIf(\d+).*" + binFile, regOptions))
                    {
                        Logger.Debug($"Removing RunIf Entry");
                        fileContent = Regex.Replace(fileContent, @"^[;]{0,1}RunIf(\d+).*" + binFile, $"", regOptions);
                        File.WriteAllText(iniPath, fileContent, Encoding.Default);
                    }

                    fileContent = GetIniFileContent(out iniPath);
                    result = !fileContent.Contains(binFile);
                }
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
            }

            return result;
        }
    }
}
