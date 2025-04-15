using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace OpenSkyPlus
{
    public static class OpenSkyPlusConfiguration
    {
        public static Configuration Config;
        private static ManualLogSource _logger;

        public static void Initialize(ManualLogSource log)
        {
            _logger = log;
            Config = new Configuration();
        }

        public class Configuration : IOpenSkyPlusConfiguration
        {
            public Configuration()
            {
                _logger.LogInfo("Loading or creating the configuration file");

                ConfigFile configFile = new(Path.Combine(OpenSkyPlus.OpenSkyPlusPath, "settings.cfg"), true);

                AppSettings = new IOpenSkyPlusConfiguration.AppSettings(
                    configFile.Bind("AppSettings",
                        "LogPath",
                        OpenSkyPlus.OpenSkyPlusPath,
                        "The location to write OpenSkyPlus logs").Value,
                    configFile.Bind("AppSettings",
                        "LogLevel",
                        "Info",
                        "Logging level for OpenSkyPlus\r\nValid values: [None, Fatal, Error, Warning, Message, Info, Debug, All]").Value,
                    configFile.Bind("AppSettings",
                        "LaunchMonitor",
                        Path.GetFileName(Paths.GameRootPath),
                        "The brand name of the launch monitor in PascalCase").Value,
                    configFile.Bind("AppSettings",
                        "LaunchMonitorAppPath",
                        Paths.GameRootPath,
                        "The location of [LaunchMonitorApp].exe").Value,
                    configFile.Bind("AppSettings",
                        "PluginPath",
                        Path.Combine(OpenSkyPlus.OpenSkyPlusPath, "plugins"),
                        "The location of OpenSkyPlus Api plugins.").Value,
                    configFile.Bind("AppSettings",
                        "ShotConfidence",
                        "Normal",
                        "How strict the monitor is in reporting shots it is less confident about\r\nValid values: [Forgiving, Normal, Strict]").Value,
                    configFile.Bind("AppSettings",
                        "RefreshConnectionAfterModeSwitch",
                        true,
                        "Indicates if a connection refresh is required when switching modes (Normal, Putting)").Value,
                    configFile.Bind("Putting",
                        "DistanceThreshold",
                        7.0f,
                        "Distance threshold (ft) for putting shot validation").Value,
                    configFile.Bind("Putting",
                        "OptimalMaxDeviation",
                        2.0f,
                        "Optimal maximum allowed deviation (degrees) for putting shots").Value
                );

                _logger.LogInfo("Configuration file loaded");
            }

            public IOpenSkyPlusConfiguration.AppSettings AppSettings { get; }
        }
    }
}
