using CFIT.AppLogger;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;

namespace CFIT.Installer.UI
{
    /// <summary>
    /// Resolves user-facing english strings to a localized variant based on the
    /// currently selected language. Language files are embedded resources named
    /// "CFIT.Installer.Localization.{lang}.json" (each containing a top-level
    /// "Localization" object mapping english keys to translated values).
    /// </summary>
    public static class Localization
    {
        public const string ResourcePrefix = "CFIT.Installer.Localization.";
        public const string DefaultLanguage = "en";

        private static Dictionary<string, string> Translation { get; set; } = new Dictionary<string, string>();
        private static bool IsLoaded { get; set; } = false;
        private static Func<string> LanguageOverride { get; set; } = null;

        /// <summary>
        /// Optional hook a Product can set to provide the active language
        /// (e.g. read from a "--info" JSON or plugin configuration). Returning
        /// null/empty falls back to CurrentUICulture.
        /// </summary>
        public static void SetLanguageOverride(Func<string> overrideFunc)
        {
            LanguageOverride = overrideFunc;
        }

        public static string Translate(string text)
        {
            EnsureLoaded();

            if (string.IsNullOrWhiteSpace(text) || Translation.Count == 0)
                return text;

            int trimStartLen = text.Length - text.TrimStart().Length;
            int trimEndLen = text.Length - text.TrimEnd().Length;
            string leading = text.Substring(0, trimStartLen);
            string trailing = text.Substring(text.Length - trimEndLen, trimEndLen);
            string key = NormalizeKey(text);
            string suffix = "";

            if (!Translation.ContainsKey(key) && key.EndsWith(":"))
            {
                key = key.Substring(0, key.Length - 1).TrimEnd();
                suffix = ":";
            }

            if (Translation.TryGetValue(key, out string translated) && !string.IsNullOrWhiteSpace(translated))
                return leading + translated + suffix + trailing;

            return text;
        }

        public static string Translate(string text, params object[] args)
        {
            string template = Translate(text);
            if (args == null || args.Length == 0)
                return template;

            try
            {
                return string.Format(CultureInfo.CurrentCulture, template, args);
            }
            catch
            {
                return template;
            }
        }

        private static void EnsureLoaded()
        {
            if (IsLoaded)
                return;

            IsLoaded = true;
            foreach (string language in GetLanguageCandidates())
            {
                if (TryLoadFromResources(language))
                    return;
                if (TryLoadFromFile(language))
                    return;
            }
        }

        private static bool TryLoadFromResources(string language)
        {
            try
            {
                foreach (Assembly assembly in GetSearchAssemblies())
                {
                    using (Stream stream = assembly.GetManifestResourceStream(ResourcePrefix + language + ".json"))
                    {
                        if (stream != null && TryParse(stream, out Dictionary<string, string> result))
                        {
                            Translation.Clear();
                            foreach (var item in result)
                                Translation[item.Key] = item.Value;
                            Logger.Debug("Loaded Localization '" + language + "' from Resources of '" + assembly.GetName().Name + "'");
                            return Translation.Count > 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return false;
        }

        private static bool TryLoadFromFile(string language)
        {
            try
            {
                foreach (string directory in GetSearchDirectories())
                {
                    string path = Path.Combine(directory, language + ".json");
                    if (File.Exists(path) && TryParse(File.ReadAllText(path), out Dictionary<string, string> result))
                    {
                        Translation.Clear();
                        foreach (var item in result)
                            Translation[item.Key] = item.Value;
                        Logger.Debug("Loaded Localization '" + language + "' from File '" + path + "'");
                        return Translation.Count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return false;
        }

        private static bool TryParse(Stream stream, out Dictionary<string, string> result)
        {
            using (var reader = new StreamReader(stream))
                return TryParse(reader.ReadToEnd(), out result);
        }

        private static bool TryParse(string content, out Dictionary<string, string> result)
        {
            result = new Dictionary<string, string>();
            try
            {
                JsonNode root = JsonNode.Parse(content);
                JsonObject localization = root?["Localization"]?.AsObject();
                if (localization == null)
                    return false;

                foreach (var item in localization)
                {
                    if (item.Value != null)
                        result[NormalizeKey(item.Key)] = item.Value.GetValue<string>();
                }

                return result.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerable<Assembly> GetSearchAssemblies()
        {
            // EntryAssembly is the Product exe (e.g. PilotsDock-Installer-latest.exe),
            // which is where the embedded language json resources live.
            Assembly entry = null;
            try { entry = Assembly.GetEntryAssembly(); } catch { }
            if (entry != null)
                yield return entry;

            Assembly executing = null;
            try { executing = Assembly.GetExecutingAssembly(); } catch { }
            if (executing != null && executing != entry)
                yield return executing;
        }

        private static IEnumerable<string> GetLanguageCandidates()
        {
            string language = GetApplicationLanguage();
            language = string.IsNullOrWhiteSpace(language) ? CultureInfo.CurrentUICulture.Name : language;
            language = language.Replace('-', '_');

            List<string> candidates = new List<string>();
            AddCandidate(candidates, language);

            int separator = language.IndexOf('_');
            if (separator > 0)
                AddCandidate(candidates, language.Substring(0, separator));

            if (language.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                AddCandidate(candidates, "zh_CN");

            AddCandidate(candidates, DefaultLanguage);
            return candidates;
        }

        private static string GetApplicationLanguage()
        {
            try
            {
                string language = LanguageOverride?.Invoke();
                if (!string.IsNullOrWhiteSpace(language))
                    return language;
            }
            catch { }

            return "";
        }

        private static IEnumerable<string> GetSearchDirectories()
        {
            List<string> directories = new List<string>();
            AddDirectory(directories, AppDomain.CurrentDomain.BaseDirectory);
            AddDirectory(directories, Directory.GetCurrentDirectory());
            return directories;
        }

        private static void AddCandidate(List<string> candidates, string language)
        {
            if (!string.IsNullOrWhiteSpace(language) && !candidates.Contains(language))
                candidates.Add(language);
        }

        private static void AddDirectory(List<string> directories, string directory)
        {
            if (!string.IsNullOrWhiteSpace(directory) && !directories.Contains(directory))
                directories.Add(directory);
        }

        private static string NormalizeKey(string text)
        {
            return string.Join(" ", text.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
