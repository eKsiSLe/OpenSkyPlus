using OpenSkyPlusApi;

namespace OpenSkyPlus
{
    public static class BallPositionData
    {
        public static string ContainerSymbol => "FDDDFMHPPEP";
        public static string BallPositionSymbol => "GGAEPNHCLEM";
    }

    public class LaunchMonitorShotData : ShotData
    {
        public override BallPositions BallPosition { get; set; } = BallPositions.Unknown;
        /// <summary>
        /// Distance from the ball to the hole, in feet.
        /// </summary>
        public float DistanceToHole { get; set; } = 0f;
        public override ClubData Club { get; set; } = new LaunchMonitorClubData();
        public override LaunchData Launch { get; set; } = new LaunchMonitorLaunchData();
        public override SpinData Spin { get; set; } = new LaunchMonitorSpinData();

        public sealed class LaunchMonitorClubData : ClubData
        {
            public static string HeadSpeedSymbol = "CEGFOPNGEKB";
            public static string HeadSpeedConfidenceSymbol = "EGHIKFPPDKJ";
            // Additional Club Data Fields
            public float ClubPath { get; set; }
            public float FaceToPath { get; set; }
            public float FaceToTarget { get; set; }
            public float AngleOfAttack { get; set; }

            public static string ContainerSymbol => "FDDDFMHPPEP";
            public override float HeadSpeed { get; set; }
            public override float HeadSpeedConfidence { get; set; }
        }

        public sealed class LaunchMonitorLaunchData : LaunchData
        {
            public LaunchMonitorLaunchData() : this(0f, 0f, 0f, 0f, 0f, 0f) { }

            public LaunchMonitorLaunchData(
                float horizontalAngle,
                float horizontalAngleConfidence,
                float launchAngle,
                float launchAngleConfidence,
                float totalSpeed,
                float totalSpeedConfidence)
            {
                HorizontalAngle = horizontalAngle;
                HorizontalAngleConfidence = horizontalAngleConfidence;
                LaunchAngle = launchAngle;
                LaunchAngleConfidence = launchAngleConfidence;
                TotalSpeed = totalSpeed;
                TotalSpeedConfidence = totalSpeedConfidence;
            }

            public static string HorizontalAngleSymbol => "GPJCPFANDGJ";
            public static string HorizontalAngleConfidenceSymbol => "DCCOCLOMLGA";
            public static string LaunchAngleSymbol => "EDNNKEPLHKC";
            public static string LaunchAngleConfidenceSymbol => "GMGMMCENIPA";
            public static string TotalSpeedSymbol => "BAFOBJEAGMN";
            public static string TotalSpeedConfidenceSymbol => "GMBPBDLKJEG";

            public override float HorizontalAngle { get; set; }
            public override float HorizontalAngleConfidence { get; set; }
            public override float LaunchAngle { get; set; }
            public override float LaunchAngleConfidence { get; set; }
            public override float TotalSpeed { get; set; }
            public override float TotalSpeedConfidence { get; set; }
        }

        public sealed class LaunchMonitorSpinData : SpinData
        {
            public LaunchMonitorSpinData() : this(0f, 0f, 0f, 0f, 0f) { }

            public LaunchMonitorSpinData(
                float backspin,
                float sideSpin,
                float totalSpin,
                float spinAxis,
                float measurementConfidence)
            {
                Backspin = backspin;
                SideSpin = sideSpin;
                TotalSpin = totalSpin;
                SpinAxis = spinAxis;
                MeasurementConfidence = measurementConfidence;
            }

            public static string MeasurementConfidenceSymbol = "AICPAFBKOPI";
            public static string ContainerSymbol => "GGEGJMKCNOP";
            public static string BackspinSymbol => "DHDLBIEFGMM";
            public static string SideSpinSymbol => "OFFCCJAAMLG";
            public static string TotalSpinSymbol => "BMINEIHGFLI";
            public static string SpinAxisSymbol => "MFLAAMECNIN";

            public override float Backspin { get; set; }
            public override float SideSpin { get; set; }
            public override float TotalSpin { get; set; }
            public override float SpinAxis { get; set; }
            public override float MeasurementConfidence { get; set; }
        }
    }
}
