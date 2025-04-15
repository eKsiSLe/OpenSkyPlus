using BepInEx.Logging;
using OpenSkyPlusApi;
using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OpenSkyPlus;

public delegate void NotificationApiLoaded();

public delegate void NotificationDeviceReady(ManualLogSource log);

public delegate void NotificationMonitorStatusChange();

public class OpenSkyPlusApi : AbstractOpenSkyPlusApi
{
    public static readonly string SupportedVersion = "4.4.7";
    private static ManualLogSource _logger;
    private static int _batteryLevel;
    private static Handedness _handedness = Handedness.Right;

    private static readonly Lazy<OpenSkyPlusApi> Instance = new(() => new OpenSkyPlusApi());
    private readonly OpenSkyPlusConfiguration.Configuration _config = OpenSkyPlusConfiguration.Config;

    private OpenSkyPlusApi()
    {
        DeviceControls.Initialize(_logger);
        DeviceControls.CheckAssemblyAndLicense();
        OpenSkyPlusApiInjector.MessageNewShot += ReceiveShot;
        OpenSkyPlusApiInjector.MessageMonitorConnected += Connected_;
        OpenSkyPlusApiInjector.MessageMonitorDisconnected += Disconnected;
        DeviceControls.MessageMonitorReady += Ready_;
        DeviceControls.MessageMonitorNotReady += NotReady;

        _logger.LogDebug($"{_config.AppSettings.LaunchMonitor} API is initialized");
    }

    private static bool Connected { get; set; }

    private static bool Ready { get; set; }

    private static int BatteryLevel
    {
        get => _batteryLevel;
        set => _batteryLevel = value > 100 ? 100 : value;
    }

    public bool Charging { get; set; } = false;

    private ShotMode ShotMode { get; set; } = ShotMode.Normal;

    private ShotData LastShot { get; set; } = new();

    protected override void Initialize()
    {
    }

    public static OpenSkyPlusApi GetInstance(ManualLogSource log = null)
    {
        if (log != null && !Instance.IsValueCreated) _logger = log;
        return Instance.Value;
    }

    public static string GetApiVersion()
    {
        return ApiVersion;
    }

    public static bool IsLoaded()
    {
        return Instance.IsValueCreated;
    }

    public override bool IsConnected()
    {
        return Connected;
    }

    public override bool IsReady()
    {
        return Ready;
    }

    public override bool ReadyForNextShot()
    {
        try
        {
            if (DeviceControls.Armed == true)
                return true;
            if (DeviceControls.ArmMonitor())
                Ready_();
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to re-arm the launch monitor: {ex}");
            return false;
        }

        return false;
    }

    public override bool SetPuttingMode()
    {
        return SetShotMode(ShotMode.Putting);
    }

    public override bool SetNormalMode()
    {
        return SetShotMode(ShotMode.Normal);
    }

    public bool SetShotMode(ShotMode mode)
    {
        OpenSkyPlusUi.SetShotMode(mode.ToString());

        var newMode = DeviceControls.SetShotMode(mode);
        ShotMode = newMode;
        _logger.LogDebug($"Changing shot mode to {mode}. Result: {ShotMode}");
        return mode == newMode;
    }

    public override ShotMode GetShotMode()
    {
        return ShotMode;
    }

    public void Disconnect()
    {
        DeviceControls.RefreshConnection(true);
    }

    public void ReplayLastShot()
    {
        _logger.LogDebug("*Replaying the last shot*");
        OnShot?.Invoke();
    }

    public override Handedness? GetHandedness()
    {
        return DeviceControls.GetHandedness();
    }

    public override Handedness? SetHandedness(Handedness handedness)
    {
        return DeviceControls.SetHandedness(handedness);
    }

    private void SetLastShot(ShotData shot)
    {
        LastShot = shot;
    }

    public override ShotData GetLastShot()
    {
        return LastShot;
    }

    private void UpdateBatteryLevel()
    {
        // Add hook in JBOLGHCECBN
        const int level = 0;
        BatteryLevel = level;
    }

    private void Connected_()
    {
        _logger.LogInfo("Device connected");

        if (Connected)
            return;

        if (DeviceControls.Armed is false)
            DeviceControls.DisarmMonitor();
        Connected = true;
        OnConnect?.Invoke();
    }

    private void Disconnected()
    {
        if (!Connected) return;

        _logger.LogInfo("Device disconnected");

        Connected = false;
        OnDisconnect?.Invoke();
    }

    private void Ready_()
    {
        if (Ready) return;

        Ready = true;
        OnReady?.Invoke();
    }

    private void NotReady()
    {
        if (!Ready) return;

        Ready = false;
        OnNotReady?.Invoke();
    }

    private void ReceiveShot(object launchMonitorShot)
    {
        var wasArmed = DeviceControls.Armed == true;
        try
        {
            DeviceControls.DisarmMonitor();
            ShotData shotData = new LaunchMonitorShotData();
            var shot = launchMonitorShot.GetType();

            var ballData = shot
                .GetField(BallPositionData.ContainerSymbol, BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(launchMonitorShot);
            var speedData = shot
                .GetField(LaunchMonitorShotData.LaunchMonitorClubData.ContainerSymbol,
                    BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(launchMonitorShot);
            var spinData = shot
                .GetField(LaunchMonitorShotData.LaunchMonitorSpinData.ContainerSymbol,
                    BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(launchMonitorShot);

            shotData.BallPosition = (BallPositions)((int?)ballData?.GetType()
                .GetField(BallPositionData.BallPositionSymbol,
                    BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(ballData) ?? 3);

            shotData.Club.HeadSpeed = (float)(speedData?
                .GetType()
                .GetField(LaunchMonitorShotData.LaunchMonitorClubData.HeadSpeedSymbol,
                    BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(speedData) ?? 0f);
            shotData.Club.HeadSpeedConfidence = (float)(speedData?
                .GetType()
                .GetField(LaunchMonitorShotData.LaunchMonitorClubData.HeadSpeedConfidenceSymbol,
                    BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(speedData) ?? 0f);

            shotData.Launch.HorizontalAngle = (float)(speedData?
                .GetType()
                .GetField(LaunchMonitorShotData.LaunchMonitorLaunchData.HorizontalAngleSymbol,
                    BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(speedData) ?? 0f);
            shotData.Launch.HorizontalAngleConfidence = (float)(speedData?
                .GetType()
                .GetField(LaunchMonitorShotData.LaunchMonitorLaunchData.HorizontalAngleConfidenceSymbol,
                    BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(speedData) ?? 0f);
            shotData.Launch.LaunchAngle = (float)(speedData?
                .GetType()
                .GetField(LaunchMonitorShotData.LaunchMonitorLaunchData.LaunchAngleSymbol,
                    BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(speedData) ?? 0f);
            shotData.Launch.LaunchAngleConfidence = (float)(speedData?
                .GetType()
                .GetField(LaunchMonitorShotData.LaunchMonitorLaunchData.LaunchAngleConfidenceSymbol,
                    BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(speedData) ?? 0f);
            shotData.Launch.TotalSpeed = (float)(speedData?
                .GetType()
                .GetField(LaunchMonitorShotData.LaunchMonitorLaunchData.TotalSpeedSymbol,
                    BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(speedData) ?? 0f);
            shotData.Launch.TotalSpeedConfidence = (float)(speedData?
                .GetType()
                .GetField(LaunchMonitorShotData.LaunchMonitorLaunchData.TotalSpeedConfidenceSymbol,
                    BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(speedData) ?? 0f);

            shotData.Spin.Backspin = (float)(spinData?
                .GetType()
                .GetField(LaunchMonitorShotData.LaunchMonitorSpinData.BackspinSymbol,
                    BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(spinData) ?? 0f);
            shotData.Spin.SideSpin = (float)(spinData?
                .GetType()
                .GetField(LaunchMonitorShotData.LaunchMonitorSpinData.SideSpinSymbol,
                    BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(spinData) ?? 0f);
            shotData.Spin.SpinAxis = (float)(spinData?
                .GetType()
                .GetField(LaunchMonitorShotData.LaunchMonitorSpinData.SpinAxisSymbol,
                    BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(spinData) ?? 0f);
            shotData.Spin.TotalSpin = (float)(spinData?
                .GetType()
                .GetField(LaunchMonitorShotData.LaunchMonitorSpinData.TotalSpinSymbol,
                    BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(spinData) ?? 0f);
            shotData.Spin.MeasurementConfidence = (float)(spinData?
                .GetType()
                .GetField(LaunchMonitorShotData.LaunchMonitorSpinData.MeasurementConfidenceSymbol,
                    BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(spinData) ?? 0f);

            // Optionally, if the launch monitor supplies a distance to the hole,
            // parse and set shotData.DistanceToHole here.
            // For example:
            // shotData.DistanceToHole = (float)(speedData?
            //     .GetType()
            //     .GetField("DistanceToHole", BindingFlags.Public | BindingFlags.Instance)?
            //     .GetValue(speedData) ?? 0f);

            LogShot(shotData);
            if (IsValidShot(shotData))
            {
                SetLastShot(shotData);
                OnShot?.Invoke();
            }
            else
            {
                _logger.LogInfo("Shot was read by launch monitor but will not be used due to confidence settings.\n");
                if (wasArmed)
                    DeviceControls.ArmMonitor();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed when parsing shot data from the launch monitor:\n{ex}");
        }
    }

    private bool IsValidShot(ShotData shot)
    {
        if (shot.Launch.TotalSpeed == 0)
            return false;

        float launchConfidence;

        var shotMode = GetInstance().GetShotMode();
        if (shotMode == ShotMode.Putting)
        {
            // Additional validation for Putting shots:
            // If the shot is a putting shot and its DistanceToHole is known and within the configured threshold,
            // ensure that the horizontal launch angle is within the optimal deviation.
            if (shot is LaunchMonitorShotData puttingShot &&
                puttingShot.DistanceToHole > 0 &&
                puttingShot.DistanceToHole <= _config.AppSettings.PuttingDistanceThreshold)
            {
                if (Math.Abs(shot.Launch.HorizontalAngle) > _config.AppSettings.PuttingOptimalMaxDeviation)
                {
                    _logger.LogWarning($"Putting shot: HorizontalAngle {shot.Launch.HorizontalAngle}° exceeds optimal deviation " +
                        $"({_config.AppSettings.PuttingOptimalMaxDeviation}°) at distance {puttingShot.DistanceToHole}ft. Clamping to 0.");
                    shot.Launch.HorizontalAngle = 0f;
                }
            }

            // In putting mode, club data is not returned.
            if (shot.Launch.LaunchAngleConfidence == 0 && shot.Launch.HorizontalAngleConfidence == 0)
                launchConfidence = 0;
            else if (shot.Launch.LaunchAngleConfidence == 0 || shot.Launch.HorizontalAngleConfidence == 0)
                launchConfidence = 0.25f;
            else if (shot.Launch.LaunchAngleConfidence < 1 || shot.Launch.HorizontalAngleConfidence < 1)
                launchConfidence = 0.5f;
            else if (shot.Launch.LaunchAngleConfidence.Equals(1f) && shot.Launch.HorizontalAngleConfidence.Equals(1f))
                launchConfidence = 1;
            else
                launchConfidence = 0;

            _logger.LogDebug($"[Putting] Confidence: launch={launchConfidence}. Mode: {_config.AppSettings.ShotConfidence}");

            return _config.AppSettings.ShotConfidence switch
            {
                "Forgiving" => launchConfidence >= 0.5f,
                "Strict" => launchConfidence > 0,
                _ => launchConfidence >= 0.25f
            };
        }

        // For normal (non-putting) shots:
        float clubConfidence;
        float spinConfidence;

        if (shot.Club.HeadSpeed == 0 || shot.Club.HeadSpeedConfidence == 0)
            clubConfidence = 0;
        else if (shot.Club.HeadSpeed != 0 && shot.Club.HeadSpeedConfidence > 0 && shot.Club.HeadSpeedConfidence < 1)
            clubConfidence = 0.5f;
        else if (shot.Club.HeadSpeed > 0 && shot.Club.HeadSpeedConfidence > 0.5f)
            clubConfidence = 1;
        else
            clubConfidence = 0;

        if ((shot.Launch.LaunchAngle == 0 || shot.Launch.HorizontalAngle == 0) &&
            (shot.Launch.LaunchAngleConfidence == 0 || shot.Launch.HorizontalAngleConfidence == 0))
            launchConfidence = 0;
        else if ((shot.Launch.LaunchAngleConfidence < 1 || shot.Launch.HorizontalAngleConfidence < 1) &&
                 shot.Launch.LaunchAngleConfidence > 0 && shot.Launch.HorizontalAngleConfidence > 0)
            launchConfidence = 0.5f;
        else if (shot.Launch.LaunchAngleConfidence.Equals(1) && shot.Launch.HorizontalAngleConfidence.Equals(1))
            launchConfidence = 1;
        else
            launchConfidence = 0;

        if (shot.Spin.TotalSpin == 0 && shot.Spin.MeasurementConfidence < 0.5f)
            spinConfidence = 0;
        else if (shot.Spin.MeasurementConfidence < 1 && shot.Spin.MeasurementConfidence > 0)
            spinConfidence = 0.5f;
        else if (shot.Spin.MeasurementConfidence.Equals(1))
            spinConfidence = 1;
        else
            spinConfidence = 0;

        float[] confidences = { clubConfidence, launchConfidence, spinConfidence };
        var averageConfidence = confidences.Average();

        _logger.LogDebug($"[{shotMode}] Confidence:: club={clubConfidence}, launch={launchConfidence}, spin={spinConfidence}. Avg:{averageConfidence}. " +
                         $"Confidence Mode: {_config.AppSettings.ShotConfidence}");

        return _config.AppSettings.ShotConfidence switch
        {
            "Forgiving" => averageConfidence >= 0.75f,
            "Strict" => averageConfidence > 0,
            _ => averageConfidence >= 0.25f
        };
    }

    private void LogShot(ShotData shot)
    {
        var sb = new StringBuilder($"{GetShotMode()} Shot Received:");
        sb.AppendLine("ClubData");
        sb.AppendLine("----");
        sb.AppendLine($"\tHead Speed: {shot.Club.HeadSpeed}");
        sb.AppendLine($"\tHead Speed Confidence: {shot.Club.HeadSpeedConfidence}");
        if (shot.Club is LaunchMonitorShotData.LaunchMonitorClubData clubData)
        {
            sb.AppendLine($"\tClub Path: {clubData.ClubPath}");
            sb.AppendLine($"\tFace To Path: {clubData.FaceToPath}");
            sb.AppendLine($"\tFace To Target: {clubData.FaceToTarget}");
            sb.AppendLine($"\tAngle Of Attack: {clubData.AngleOfAttack}");
        }
        sb.AppendLine("LaunchData");
        sb.AppendLine("------");
        sb.AppendLine($"\tHorizontal Angle: {shot.Launch.HorizontalAngle}");
        sb.AppendLine($"\tHorizontal Angle Confidence: {shot.Launch.HorizontalAngleConfidence}");
        sb.AppendLine($"\tVertical Angle: {shot.Launch.LaunchAngle}");
        sb.AppendLine($"\tVertical Angle Confidence: {shot.Launch.LaunchAngleConfidence}");
        sb.AppendLine($"\tTotal Speed: {shot.Launch.TotalSpeed}");
        sb.AppendLine($"\tTotal Speed Confidence: {shot.Launch.TotalSpeedConfidence}\n");
        sb.AppendLine("SpinData");
        sb.AppendLine("----");
        sb.AppendLine($"\tSpin Axis: {shot.Spin.SpinAxis}");
        sb.AppendLine($"\tBackspin: {shot.Spin.Backspin}");
        sb.AppendLine($"\tSide Spin: {shot.Spin.SideSpin}");
        sb.AppendLine($"\tTotal Spin: {shot.Spin.TotalSpin}");
        sb.AppendLine($"\tMeasurement Confidence: {shot.Spin.MeasurementConfidence}");
        _logger.LogInfo(sb.ToString());
    }

    public void LogToOpenSkyPlus(string message, object level)
    {
        try
        {
            switch ((LogLevels)level)
            {
                case LogLevels.Info:
                    _logger.LogInfo(message);
                    break;
                case LogLevels.Warning:
                    _logger.LogWarning(message);
                    break;
                case LogLevels.Debug:
                    _logger.LogDebug(message);
                    break;
                case LogLevels.Error:
                default:
                    _logger.LogError(message);
                    break;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to translate logging request to {_logger.GetType().Name}: {ex}");
        }
    }

    /// <summary>
    /// Logs all the raw data from a shot.
    /// </summary>
    private void ShotDump(object shot)
    {
        var Shot = shot.GetType();

        var FDDDFMHPPEP = Shot.GetField("FDDDFMHPPEP", BindingFlags.Public | BindingFlags.Instance).GetValue(shot);
        var GGEGJMKCNOP = Shot.GetField("GGEGJMKCNOP", BindingFlags.Public | BindingFlags.Instance).GetValue(shot);
        var MHLDAHLDJFE = Shot.GetField("MHLDAHLDJFE", BindingFlags.Public | BindingFlags.Instance).GetValue(shot);
        var LDLMHJGLBDF = Shot.GetField("LDLMHJGLBDF", BindingFlags.Public | BindingFlags.Instance).GetValue(shot);
        var DLIIJJNCIPP = Shot.GetField("DLIIJJNCIPP", BindingFlags.Public | BindingFlags.Instance).GetValue(shot);

        // Raw data logging omitted for brevity
    }

    internal void ToggleHandedness()
    {
        var newHandedness = _handedness == Handedness.Left ? Handedness.Right : Handedness.Left;
        var newMode = DeviceControls.SetHandedness(newHandedness);
        _handedness = newHandedness;
        _logger.LogDebug($"Changing handedness to {newMode}");
    }

    internal void SoftNetworkReset()
    {
        DeviceControls.SoftResetNetwork();
    }
}

public static class ApiSubscriber
{
    public static event NotificationApiLoaded MessageApiLoaded;

    public static void Initialize()
    {
        OpenSkyPlusApiInjector.MessageDeviceReady += log =>
        {
            try
            {
                OpenSkyPlusApi.GetInstance(log);
                MessageApiLoaded?.Invoke();
            }
            catch (OpenSkyPlusApiException ex)
            {
                OpenSkyPlus.Log.LogFatal(ex.Message);
            }
            catch (Exception ex)
            {
                OpenSkyPlus.Log.LogFatal($"Failed to create an Api singleton instance: {ex}");
            }
        };
    }
}
