using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.LibFunc;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using Localization = CFIT.Installer.UI.Localization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CFIT.Installer.LibWorker
{
    public class WorkerPackagePaths<C> : TaskWorker<C> where C : ConfigBase
    {
        public List<Simulator> SearchSimulators { get; set; } = new List<Simulator>();

        public WorkerPackagePaths(C config, string title = "Package Paths", string message = "") : base(config, Localization.Translate(title), Localization.Translate(message))
        {
            Model.DisplayInSummary = false;
            SetPropertyFromOption<List<Simulator>>(ConfigBase.OptionSearchSimulators);
        }

        public static void ParseSimArguments(string[] args, ConfigBase config)
        {
            var list = new List<Simulator>();

            if (Sys.HasArgument(args, "--2020"))
            {
                list.Add(Simulator.MSFS2020);
                Logger.Information("Argument '--2020' passed!");
            }
            if (Sys.HasArgument(args, "--2024"))
            {
                list.Add(Simulator.MSFS2024);
                Logger.Information("Argument '--2024' passed!");
            }

            if (list.Count == 0)
            {
                list.Add(Simulator.MSFS2020);
                list.Add(Simulator.MSFS2024);
            }
            config.SetOption(ConfigBase.OptionSearchSimulators, list);
        }

        protected void CheckSim(Simulator sim, Dictionary<Simulator, string[]> dict)
        {
            if (SearchSimulators?.Contains(sim) == true)
            {
                Model.Message = Localization.Translate("Searching Package Path for {0} ...", sim);
                if (FuncMsfs.CheckInstalledMsfs(sim, SimulatorStore.All, out Dictionary<SimulatorStore, string> paths))
                {
                    dict.Add(sim, paths.Values.ToArray());
                    Logger.Debug($"Added {paths?.Values?.Count} Paths for Simulator {sim}");
                }
            }
        }

        protected override Task<bool> DoRun()
        {
            var packagePaths = new Dictionary<Simulator, string[]>();
            foreach (var sim in SearchSimulators)
                CheckSim(sim, packagePaths);

            if (packagePaths.Any(kv => kv.Value.Length > 0))
            {
                Config.SetOption(ConfigBase.OptionPackagePaths, packagePaths);
                Model.SetSuccess(Localization.Translate("Found {0} Package Paths!", packagePaths.Sum(kv => kv.Value.Length)));
                return Task.FromResult(true);
            }
            else
            {
                Model.SetError(Localization.Translate("No Package Paths found!"));
                return Task.FromResult(false);
            }
        }
    }
}
