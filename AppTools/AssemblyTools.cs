using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CFIT.AppTools
{
    public static class AssemblyTools
    {
        public static DateTime GetLinkerTime(this Assembly assembly)
        {
            try
            {
                const string BuildVersionMetadataPrefix = "+build";
                const string dateFormat = "yyyy.MM.dd.HHmm";

                var attribute = assembly
                  .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

                if (attribute?.InformationalVersion != null)
                {
                    var value = attribute.InformationalVersion;
                    var index = value.IndexOf(BuildVersionMetadataPrefix);
                    if (index > 0)
                    {
                        value = value.Substring(index + BuildVersionMetadataPrefix.Length);

                        // .NET SDK 在 publish 时会把 SourceRevisionId（如 git commit SHA）
                        // 追加到 InformationalVersion 末尾，使 value 形如
                        // "2026.07.02.1343.0d0d813c..."。用正则只提取时间戳部分，
                        // 否则 ParseExact 会因多余字符抛异常而回退到 default(DateTime.MinValue)。
                        var match = Regex.Match(value, @"\d{4}\.\d{2}\.\d{2}\.\d{4}");
                        if (match.Success)
                            value = match.Value;

                        return DateTime.ParseExact(
                            value,
                          dateFormat,
                          CultureInfo.InvariantCulture);
                    }
                }
            }
            catch { }

            return default;
        }

        public static Stream GetStreamFromAssembly(string name, bool executing = false)
        {
            try
            {
                if (!executing)
                    return Assembly.GetEntryAssembly().GetManifestResourceStream(name);
                else
                    return Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            }
            catch { }
            return null;
        }
    }
}
