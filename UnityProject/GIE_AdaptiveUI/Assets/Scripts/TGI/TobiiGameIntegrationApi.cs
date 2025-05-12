using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Tobii.GameIntegration.Net.Extensions;
using System.Numerics;

namespace Tobii.GameIntegration.Net
{
	using static ConfigurationHandler;

	namespace Extensions
	{
		internal static class IntPtrExtensions
		{
			public static bool IsZero(this IntPtr @this)
			{
				return @this.Equals(IntPtr.Zero);
			}

			public static bool IsNotZero(this IntPtr @this)
			{
				return !@this.IsZero();
			}

			public static IntPtr Add(this IntPtr pointer, int offset)
			{
				var pointerInt = IntPtr.Size == 8 ? pointer.ToInt64() : pointer.ToInt32();
				return new IntPtr(pointerInt + offset);
			}

			public static List<T> ToList<T>(this IntPtr pointerToFirstItem, int numberOfItems)
			{
				if(pointerToFirstItem.IsNotZero())
				{
					var items = new List<T>(numberOfItems);
					var itemSize = Marshal.SizeOf(typeof(T));

					for(var i = 0; i < numberOfItems; i++)
					{
						var itemPointer = pointerToFirstItem.Add((i * itemSize));
						items.Add((T) Marshal.PtrToStructure(itemPointer, typeof(T)));
					}

					return items;
				}

				return default(List<T>);
			}

			public static string ToAnsiString(this IntPtr pointerToAnsiString)
			{
				return pointerToAnsiString.IsNotZero() ? Marshal.PtrToStringAnsi(pointerToAnsiString) : default(string);
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct TobiiRectangle
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}

	public enum TrackerType
	{
		None = 0,
		PC,
		HeadMountedDisplay
	}

	public enum StreamType
	{
		Presence = 0,
		Head = 1,
		GazeOS = 2,
		Gaze = 3,
		Foveation = 4,
		EyeInfo = 5,
		HMD = 6,
		UnfilteredGaze = 7,
		Count = 8
	}

	[Flags]
	public enum CapabilityFlags
	{
		None = 0,
		Presence = 1 << StreamType.Presence,
		Head = 1 << StreamType.Head,
		GazeOS = 1 << StreamType.GazeOS,
		Gaze = 1 << StreamType.Gaze,
		Foveation = 1 << StreamType.Foveation,
		EyeInfo = 1 << StreamType.EyeInfo,
		HMD = 1 << StreamType.HMD,
		UnfilteredGaze = 1 << StreamType.UnfilteredGaze
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct WidthHeight
	{
		public int Width;
		public int Height;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct TrackerInfoInterop
	{
		public TrackerType Type;
		public CapabilityFlags Capabilities;
		public TobiiRectangle DisplayRectInOSCoordinates;
		public WidthHeight DisplaySizeMm;
		public IntPtr Url;
		public IntPtr FriendlyName;
		public IntPtr MonitorNameInOS;
		public IntPtr ModelName;
		public IntPtr Generation;
		public IntPtr SerialNumber;
		public IntPtr FirmwareVersion;
		public bool IsAttached;
	}

	public class TrackerInfo
	{
		internal TrackerInfo(TrackerInfoInterop trackerInfoInterop)
		{
			Type = trackerInfoInterop.Type;
			Capabilities = trackerInfoInterop.Capabilities;
			DisplayRectInOSCoordinates = trackerInfoInterop.DisplayRectInOSCoordinates;
			DisplaySizeMm = trackerInfoInterop.DisplaySizeMm;
			Url = trackerInfoInterop.Url.ToAnsiString();
			FriendlyName = trackerInfoInterop.FriendlyName.ToAnsiString();
			MonitorNameInOS = trackerInfoInterop.MonitorNameInOS.ToAnsiString();
			ModelName = trackerInfoInterop.ModelName.ToAnsiString();
			Generation = trackerInfoInterop.Generation.ToAnsiString();
			SerialNumber = trackerInfoInterop.SerialNumber.ToAnsiString();
			FirmwareVersion = trackerInfoInterop.FirmwareVersion.ToAnsiString();
			IsAttached = trackerInfoInterop.IsAttached;
		}

		internal static TrackerInfo Create(TrackerInfoInterop trackerInfoInterop)
		{
			return new TrackerInfo(trackerInfoInterop);
		}

		public TrackerType Type { get;  }
		public CapabilityFlags Capabilities { get; }
		public TobiiRectangle DisplayRectInOSCoordinates { get; }
		public WidthHeight DisplaySizeMm { get; }
		public string Url { get; }
		public string FriendlyName { get; }
		public string MonitorNameInOS { get; }
		public string ModelName { get; }
		public string Generation { get; }
		public string SerialNumber { get; }
		public string FirmwareVersion { get; }
		public bool IsAttached { get; }
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Rotation
	{
		public float YawDegrees;
		public float PitchDegrees;
		public float RollDegrees;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Position
	{
		public float X;
		public float Y;
		public float Z;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct HeadPose
	{
		public Rotation Rotation;
		public Position Position;
		public long TimeStampMicroSeconds;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct GazePoint
	{
		public long TimeStampMicroSeconds;
		public float X;
		public float Y;
	}

	public enum StatisticsLiteral
	{
		IsEnabled,
		IsDisabled,
		ChangedToEnabled,
		ChangedToDisabled,
		GameStarted,
		GameStopped,
		Separator
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Transformation
	{
		public Rotation Rotation;
		public Position Position;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct RangeBool
	{
		[MarshalAs(UnmanagedType.I1)]
		public bool Min;

		[MarshalAs(UnmanagedType.I1)]
		public bool Max;

		public RangeBool(bool min, bool max) 
		{
			Min = min;
			Max = max;
		}

		public RangeBool(RangeBool other)
		{
			Min = other.Min;
			Max = other.Max;
		}
    };

	[StructLayout(LayoutKind.Sequential)]
	public struct RangeFloat
	{
		public float Min;
		public float Max;

		public RangeFloat(float min, float max)
		{
			Min = min;
			Max = max;
		}

		public RangeFloat(RangeFloat other)
		{
			Min = other.Min;
			Max = other.Max;
		}
	};

	[StructLayout(LayoutKind.Sequential)]
	public struct SettingMetadataBool
	{
		[MarshalAs(UnmanagedType.I1)]
		readonly public bool Default;

		readonly public RangeBool MinMaxRange;

		public SettingMetadataBool(bool defaultValue, RangeBool minMaxRange)
		{
			Default = defaultValue;
			MinMaxRange = new RangeBool(minMaxRange);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SettingMetadataFloat
	{
		readonly public float Default;

		readonly public RangeFloat MinMaxRange;

		public SettingMetadataFloat(float defaultValue, RangeFloat minMaxRange)
		{
			Default = defaultValue;
			MinMaxRange = new RangeFloat(minMaxRange);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SettingBool
	{
		[MarshalAs(UnmanagedType.I1)]
		public bool Value;

		public SettingMetadataBool Metadata;

		public SettingBool(bool defaultValue, RangeBool minMaxRange) 
		{
			Value = defaultValue;
			Metadata = new SettingMetadataBool(defaultValue, minMaxRange);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SettingFloat
	{
		public float Value;
		public SettingMetadataFloat Metadata;

		public SettingFloat(float defaultValue, RangeFloat minMaxRange)
		{
			Value = defaultValue;
			Metadata = new SettingMetadataFloat(defaultValue, minMaxRange);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct AxisSettings
	{
		public SettingFloat Limit;
		public SettingFloat SensitivityScaling;
		public SettingFloat SCurveStrengthNorm;
		public SettingFloat SCurveMidPointNorm;
		public SettingFloat DeadZoneNorm;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct HeadViewAutoCenterSettings
	{
		[MarshalAs(UnmanagedType.I1)] public bool IsEnabled;
		public float NormalizeFasterGazeDeadZoneNormalized;
		public float ExtendedViewAngleFasterDeadZoneDegrees;
		public float MaxDistanceFromMasterCm;
		public float MaxAngularDistanceDegrees;
		public float FasterNormalizationFactor;
		public float PositionCompensationSpeed;
		public float RotationCompensationSpeed;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct HeadTrackingSettings
	{
		public SettingBool Enabled;
		public SettingBool AutoReset;
		public SettingFloat RotationResponsiveness;
		public SettingBool RotationRollEnabled;

		public SettingBool PositionEnabled;
		public SettingBool RelativeHeadPositionEnabled;
		public SettingBool RotateAxisSettingsWithHead;

		public AxisSettings YawRightDegrees;
		public AxisSettings YawLeftDegrees;
		public AxisSettings PitchUpDegrees;
		public AxisSettings PitchDownDegrees;
		public AxisSettings RollRightDegrees;
		public AxisSettings RollLeftDegrees;

		public AxisSettings XRightMm;
		public AxisSettings XLeftMm;
		public AxisSettings YUpMm;
		public AxisSettings YDownMm;
		public AxisSettings ZBackMm;
		public AxisSettings ZForwardMm;
	}

	struct HeadTrackingHelpFunctions
	{
		static public void SetHeadAllRotationAxisSettingsSensitivity(ref HeadTrackingSettings headTrackingSettings, float sensitivityScaling)
		{
			if (Is64BitProcess)
			{
				x64.SetHeadAllRotationAxisSettingsSensitivity(ref headTrackingSettings, sensitivityScaling);
			}
			else
			{
				x86.SetHeadAllRotationAxisSettingsSensitivity(ref headTrackingSettings, sensitivityScaling);
			}
		}

		static public void SetHeadAllRotationAxisSettingsLimitNormalized(ref HeadTrackingSettings headTrackingSettings, float limitNormalizedOnRange)
		{
			if (Is64BitProcess)
			{
				x64.SetHeadAllRotationAxisSettingsLimitNormalized(ref headTrackingSettings, limitNormalizedOnRange);
			}
			else
			{
				x86.SetHeadAllRotationAxisSettingsLimitNormalized(ref headTrackingSettings, limitNormalizedOnRange);
			}
		}

		static public void SetHeadAllPositionAxisSettingsSensitivity(ref HeadTrackingSettings headTrackingSettings, float sensitivityScaling)
		{
			if (Is64BitProcess)
			{
				x64.SetHeadAllPositionAxisSettingsSensitivity(ref headTrackingSettings, sensitivityScaling);
			}
			else
			{
				x86.SetHeadAllPositionAxisSettingsSensitivity(ref headTrackingSettings, sensitivityScaling);
			}
		}

		static public void SetHeadAllPositionAxisSettingsLimitNormalized(ref HeadTrackingSettings headTrackingSettings, float limitNormalizedOnRange)
		{
			if (Is64BitProcess)
			{
				x64.SetHeadAllPositionAxisSettingsLimitNormalized(ref headTrackingSettings, limitNormalizedOnRange);
			}
			else
			{
				x86.SetHeadAllPositionAxisSettingsLimitNormalized(ref headTrackingSettings, limitNormalizedOnRange);
			}
		}
		static public void SetCenterStabilization(ref HeadTrackingSettings headTrackingSettings, float centerStabilization)
		{
			if (Is64BitProcess)
			{
				x64.SetCenterStabilization(ref headTrackingSettings, centerStabilization);
			}
			else
			{
				x86.SetCenterStabilization(ref headTrackingSettings, centerStabilization);
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct CameraBoostSettings
	{
		public SettingBool Enabled;
		public SettingFloat GazeDeadZone;
		public SettingFloat Boost;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct GazeHeadMixSettings
	{
		public SettingBool	Enabled;
		public SettingFloat GazeResponsiveness;
		public SettingFloat GazeYawLimitDegrees;
		public SettingFloat GazePitchUpLimitDegrees;
		public SettingFloat GazePitchDownLimitDegrees;
	}

	struct GazeHeadMixHelpFunctions
	{
		static public void SetEyeHeadTrackingRatio(ref GazeHeadMixSettings gazeHeadMixSettings, ref HeadTrackingSettings headTrackingSettings, float eyeHeadTrackingRatio)
		{
			if (Is64BitProcess)
			{
				x64.SetEyeHeadTrackingRatio(ref gazeHeadMixSettings, ref headTrackingSettings, eyeHeadTrackingRatio);
			}
			else
			{
				x86.SetEyeHeadTrackingRatio(ref gazeHeadMixSettings, ref headTrackingSettings, eyeHeadTrackingRatio);
			}
		}

		static public void SetCameraMaxAngleYaw(ref GazeHeadMixSettings gazeHeadMixSettings, ref HeadTrackingSettings headTrackingSettings, float positiveYawLimitDegrees)
		{
			if (Is64BitProcess)
			{
				x64.SetCameraMaxAngleYaw(ref gazeHeadMixSettings, ref headTrackingSettings, positiveYawLimitDegrees);
			}
			else
			{
				x86.SetCameraMaxAngleYaw(ref gazeHeadMixSettings, ref headTrackingSettings, positiveYawLimitDegrees);
			}
		}

		static public void SetCameraMaxAnglePitchUp(ref GazeHeadMixSettings gazeHeadMixSettings, ref HeadTrackingSettings headTrackingSettings, float positivePitchUpLimitDegrees)
		{
			if (Is64BitProcess)
			{
				x64.SetCameraMaxAnglePitchUp(ref gazeHeadMixSettings, ref headTrackingSettings, positivePitchUpLimitDegrees);
			}
			else
			{
				x86.SetCameraMaxAnglePitchUp(ref gazeHeadMixSettings, ref headTrackingSettings, positivePitchUpLimitDegrees);
			}
		}

		static public void SetCameraMaxAnglePitchDown(ref GazeHeadMixSettings gazeHeadMixSettings, ref HeadTrackingSettings headTrackingSettings, float negativePitchDownLimitDegrees)
		{
			if (Is64BitProcess)
			{
				x64.SetCameraMaxAnglePitchDown(ref gazeHeadMixSettings, ref headTrackingSettings, negativePitchDownLimitDegrees);
			}
			else
			{
				x86.SetCameraMaxAnglePitchDown(ref gazeHeadMixSettings, ref headTrackingSettings, negativePitchDownLimitDegrees);
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct ExtendedViewSettings
	{
		public ExtendedViewSettings(bool getCDefaults = true)
		{
			if (getCDefaults) { this = TobiiGameIntegrationApi.GetDefaultExtendedViewSettings(); }
			else { this = default(ExtendedViewSettings); }
		}

		public HeadTrackingSettings HeadTracking;
		public CameraBoostSettings CameraBoost;
		public GazeHeadMixSettings GazeHeadMix;
	}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct TuningValue
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)]
		public string Name;
		public float MinValue;
		public float MaxValue;
		public float CurrentValue;
	}

	public static class TobiiGameIntegrationApi
	{
		public static string LoadedDll => Is64BitProcess ? x64.TobiiGameIntegrationDll : x86.TobiiGameIntegrationDll;

		public static void PrelinkAll(){
			if (Is64BitProcess)
			{
				Marshal.PrelinkAll(typeof(x64));
			} 
			else
			{
				Marshal.PrelinkAll(typeof(x86));
			}
		}

		public static void SetApplicationName(string fullApplicationName)
		{
			if (Is64BitProcess)
			{
				x64.SetApplicationName(fullApplicationName);
			}
			else
			{
				x86.SetApplicationName(fullApplicationName);
			}
		}

		#region ITobiiGameIntegrationApi

		public static bool IsApiInitialized()
		{
			if(Is64BitProcess)
			{
				return x64.IsApiInitialized();
			}
			else
			{
				return x86.IsApiInitialized();
			}
		}

		public static void Update()
		{
			if(Is64BitProcess)
			{
				x64.Update();
			}
			else
			{
				x86.Update();
			}
		}

		public static void Shutdown()
		{
			if(Is64BitProcess)
			{
				x64.Shutdown();
			}
			else
			{
				x86.Shutdown();
			}
		}

		#endregion


		#region ITrackerController

		public static TrackerInfo GetTrackerInfo()
		{
			TrackerInfoInterop trackerInfoInterop;
			bool isTrackerInfoAvialable;

			if (Is64BitProcess)
			{
				isTrackerInfoAvialable = x64.GetTrackerInfo(out trackerInfoInterop);
			}
			else
			{
				isTrackerInfoAvialable = x86.GetTrackerInfo(out trackerInfoInterop);
			}
			
			return isTrackerInfoAvialable ? new TrackerInfo(trackerInfoInterop) : default(TrackerInfo);
		}

		public static TrackerInfo GetTrackerInfo(string trackerUrl)
		{
			TrackerInfoInterop trackerInfoInterop;
			bool isTrackerInfoAvialable;

			if (Is64BitProcess)
			{
				isTrackerInfoAvialable = x64.GetTrackerInfoByUrl(trackerUrl, out trackerInfoInterop);
			}
			else
			{
				isTrackerInfoAvialable = x86.GetTrackerInfoByUrl(trackerUrl, out trackerInfoInterop);
			}

			return isTrackerInfoAvialable ? new TrackerInfo(trackerInfoInterop) : default(TrackerInfo);
		}

		public static void UpdateTrackerInfos()
		{
			if (Is64BitProcess)
			{
				x64.UpdateTrackerInfos();
			}
			else
			{
				x86.UpdateTrackerInfos();
			}
		}

		public static List<TrackerInfo> GetTrackerInfos()
		{
			IntPtr trackerInfoInteropsPointer;
			bool hasAsyncUpdateFinished;
			int numberOfTrackerInfos;

			if (Is64BitProcess)
			{
				hasAsyncUpdateFinished = x64.GetTrackerInfos(out trackerInfoInteropsPointer, out numberOfTrackerInfos);
			}
			else
			{
				hasAsyncUpdateFinished = x86.GetTrackerInfos(out trackerInfoInteropsPointer, out numberOfTrackerInfos);
			}

			return hasAsyncUpdateFinished ? trackerInfoInteropsPointer.ToList<TrackerInfoInterop>(numberOfTrackerInfos).Select(TrackerInfo.Create).ToList() : default(List<TrackerInfo>);
		}
		
		public static bool TrackHMD()
		{
			if(Is64BitProcess)
			{
				return x64.TrackHMD();
			}
			else
			{
				return x86.TrackHMD();
			}
		}

		public static bool TrackRectangle(TobiiRectangle rectangle)
		{
			if(Is64BitProcess)
			{
				return x64.TrackRectangle(rectangle);
			}
			else
			{
				return x86.TrackRectangle(rectangle);
			}
		}

		public static bool TrackWindow(IntPtr windowHandle)
		{
			if(Is64BitProcess)
			{
				return x64.TrackWindow(windowHandle);
			}
			else
			{
				return x86.TrackWindow(windowHandle);
			}
		}

		public static bool TrackTracker(string trackerUrl)
		{
			if (Is64BitProcess)
			{
				return x64.TrackTracker(trackerUrl);
			}
			else
			{
				return x86.TrackTracker(trackerUrl);
			}
		}


		public static bool IsTrackerConnected()
		{
			if(Is64BitProcess)
			{
				return x64.IsConnected();
			}
			else
			{
				return x86.IsConnected();
			}
		}

		public static bool IsTrackerEnabled()
		{
			if (Is64BitProcess)
			{
				return x64.IsDeviceEnabled();
			}
			else
			{
				return x86.IsDeviceEnabled();
			}
		}

		public static void StopTracking()
		{
			if(Is64BitProcess)
			{
				x64.StopTracking();
			}
			else
			{
				x86.StopTracking();
			}
		}

		#endregion


		#region IStreamsProvider

		public static bool TryGetLatestHeadPose(out HeadPose headPose)
		{
			if(Is64BitProcess)
			{
				return x64.GetLatestHeadPose(out headPose);
			}
			else
			{
				return x86.GetLatestHeadPose(out headPose);
			}
		}

		public static bool TryGetLatestGazePoint(out GazePoint gazePoint)
		{
			if(Is64BitProcess)
			{
				return x64.GetLatestGazePoint(out gazePoint);
			}
			else
			{
				return x86.GetLatestGazePoint(out gazePoint);
			}
		}

		public static List<GazePoint> GetGazePoints()
		{
			IntPtr gazePoints;
			int numberOfAvailableGazePoints;

			if(Is64BitProcess)
			{
				numberOfAvailableGazePoints = x64.GetGazePoints(out gazePoints);
			}
			else
			{
				numberOfAvailableGazePoints = x86.GetGazePoints(out gazePoints);
			}

			return gazePoints.ToList<GazePoint>(numberOfAvailableGazePoints);
		}

		public static List<HeadPose> GetHeadPoses()
		{
			IntPtr headPoses;
			int numberOfAvailableHeadPoses;

			if(Is64BitProcess)
			{
				numberOfAvailableHeadPoses = x64.GetHeadPoses(out headPoses);
			}
			else
			{
				numberOfAvailableHeadPoses = x86.GetHeadPoses(out headPoses);
			}

			return headPoses.ToList<HeadPose>(numberOfAvailableHeadPoses);
		}

		public static bool IsPresent()
		{
			if(Is64BitProcess)
			{
				return x64.IsPresent();
			}
			else
			{
				return x86.IsPresent();
			}
		}

		public static void SetAutoUnsubscribe(StreamType capability, float timeout)
		{
			if(Is64BitProcess)
			{
				x64.SetAutoUnsubscribe(capability, timeout);
			}
			else
			{
				x86.SetAutoUnsubscribe(capability, timeout);
			}
		}

		public static void UnsetAutoUnsubscribe(StreamType capability)
		{
			if(Is64BitProcess)
			{
				x64.UnsetAutoUnsubscribe(capability);
			}
			else
			{
				x86.UnsetAutoUnsubscribe(capability);
			}
		}

		#endregion


		#region IStatisticsRecorder

		public static string GetStatisticsLiteral(StatisticsLiteral statisticsLiteral)
		{
			IntPtr statisticsLiteralPtr;

			if(Is64BitProcess)
			{
				statisticsLiteralPtr = x64.GetStatisticsLiteral(statisticsLiteral);
			}
			else
			{
				statisticsLiteralPtr = x86.GetStatisticsLiteral(statisticsLiteral);
			}

			return Marshal.PtrToStringAnsi(statisticsLiteralPtr);
		}

		#endregion


		#region IExtendedView

		public static Transformation GetExtendedViewTransformation()
		{
			Transformation extendedViewTransformation;

			if(Is64BitProcess)
			{
				x64.GetExtendedViewTransformation(out extendedViewTransformation);
			}
			else
			{
				x86.GetExtendedViewTransformation(out extendedViewTransformation);
			}

			return extendedViewTransformation;
		}

		public static bool UpdateExtendedViewSettings(ExtendedViewSettings settings)
		{
			if(Is64BitProcess)
			{
				return x64.UpdateExtendedViewSettings(ref settings);
			}
			else
			{
				return x86.UpdateExtendedViewSettings(ref settings);
			}
		}

		public static void ResetExtendedViewDefaultHeadPose()
		{
			if(Is64BitProcess)
			{
				x64.ResetExtendedViewDefaultHeadPose();
			}
			else
			{
				x86.ResetExtendedViewDefaultHeadPose();
			}
		}

		public static void ExtendedViewPause(bool reCenter, float transitionDuration = 0.2f)
		{
			if(Is64BitProcess)
			{
				x64.ExtendedViewPause(reCenter, transitionDuration);
			}
			else
			{
				x86.ExtendedViewPause(reCenter, transitionDuration);
			}
		}

		public static void ExtendedViewUnPause(float transitionDuration = 0.2f)
		{
			if(Is64BitProcess)
			{
				x64.ExtendedViewUnPause(transitionDuration);
			}
			else
			{
				x86.ExtendedViewUnPause(transitionDuration);
			}
		}

		public static bool ExtendedViewIsPaused()
		{
			if(Is64BitProcess)
			{
				return x64.ExtendedViewIsPaused();
			}
			else
			{
				return x86.ExtendedViewIsPaused();
			}
		}

		public static ExtendedViewSettings GetDefaultExtendedViewSettings()
		{
			ExtendedViewSettings settings;
			if (Is64BitProcess)
			{
				x64.GetDefaultExtendedViewSettings(out settings);
			}
			else
			{
				x86.GetDefaultExtendedViewSettings(out settings);
			}
			return settings;
		}

		public static ExtendedViewSettings GetExtendedViewSettings()
		{
			ExtendedViewSettings settings;
			if(Is64BitProcess)
			{
				x64.GetExtendedViewSettings(out settings);
			}
			else
			{
				x86.GetExtendedViewSettings(out settings);
			}
			return settings;
		}

		public static int PlotCurvePoints([In, Out] Vector2[] allocatedPointsBuf, AxisSettings axisSettings)
		{
			int maxPoints = allocatedPointsBuf.Length;
			if(Is64BitProcess)
			{
				return x64.PlotCurvePoints(allocatedPointsBuf, maxPoints, ref axisSettings);
			}
			else
			{
				return x86.PlotCurvePoints(allocatedPointsBuf, maxPoints, ref axisSettings);
			}
		}

		#endregion


		#region Tuning Values

		public static List<TuningValue> GetTuningValues()
		{
			int numberOfTuningValues;
			IntPtr tuningValues;

			if (Is64BitProcess)
			{
				numberOfTuningValues = x64.GetTuningValues(out tuningValues);
			}
			else
			{
				numberOfTuningValues = x86.GetTuningValues(out tuningValues);
			}

			return tuningValues.ToList<TuningValue>(numberOfTuningValues);
		}

		public static void SetTuningValue(string name, float value)
		{
			if (Is64BitProcess)
			{
				x64.SetTuningValue(name, value);
			}
			else
			{
				x86.SetTuningValue(name, value);
			}
		}

		#endregion

	}

	internal static class ConfigurationHandler
	{
		public static bool Is64BitProcess => IntPtr.Size == 8;

		public static class x86
		{
#if DEBUG
			internal const string TobiiGameIntegrationDll = "tobii_gameintegration_x86_d.dll";
#else
			internal const string TobiiGameIntegrationDll = "tobii_gameintegration_x86.dll";
#endif
			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetApplicationName([MarshalAs(UnmanagedType.LPStr)] string fullApplicationName);

			#region ITobiiGameIntegrationApi

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsApiInitialized();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void Update();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void Shutdown();

			#endregion


			#region ITrackerController

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetTrackerInfo(out TrackerInfoInterop trackerInfoInterop);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetTrackerInfoByUrl([MarshalAs(UnmanagedType.LPStr)] string trackerUrl, out TrackerInfoInterop trackerInfoInterop);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void UpdateTrackerInfos();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetTrackerInfos(out IntPtr trackerInfosPointer, out int numberOfTrackerInfos);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool TrackHMD();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool TrackRectangle(TobiiRectangle rectangle);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool TrackWindow(IntPtr windowHandle);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool TrackTracker([MarshalAs(UnmanagedType.LPStr)] string trackerUrl);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void StopTracking();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsConnected();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsDeviceEnabled();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsStreamSupported(StreamType stream);

			#endregion


			#region IStreamsProvider

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetLatestGazePoint(out GazePoint gazePoint);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetLatestHeadPose(out HeadPose headPose);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int GetGazePoints(out IntPtr gazePoints);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int GetHeadPoses(out IntPtr headPoses);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsPresent();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetAutoUnsubscribe(StreamType capability, float timeout);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void UnsetAutoUnsubscribe(StreamType capability);

			// TODO: Implement
			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern bool GetLatestHMDGaze(out HMDGazePoint hmdGazePoint);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern int GetHMDGaze(out IntPtr hmdGazePoints);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern void ConvertGazePoint(GazePoint fromGazePoint, GazePoint toGazePoint, UnitType fromUnit, UnitType toUnit);

			#endregion


			#region IStatisticsRecorder

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr GetStatisticsLiteral(StatisticsLiteral statisticsLiteral);

			#endregion


			#region IExtendedView

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void GetExtendedViewTransformation(out Transformation transformation);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool UpdateExtendedViewSettings(ref ExtendedViewSettings settings);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void ResetExtendedViewDefaultHeadPose();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void ExtendedViewPause([MarshalAs(UnmanagedType.I1)] bool reCenter, float transitionDuration = 0.2f);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void ExtendedViewUnPause(float transitionDuration = 0.2f);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool ExtendedViewIsPaused();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void GetDefaultExtendedViewSettings(out ExtendedViewSettings settings);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void GetExtendedViewSettings(out ExtendedViewSettings settings);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int PlotCurvePoints([In, Out] Vector2[] allocatedPointsBuf, int maxPoints, ref AxisSettings axisSettings);

			#endregion


			#region ExtendedViewSettings help functions

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetHeadAllRotationAxisSettingsSensitivity(ref HeadTrackingSettings headTrackingSettings, float sensitivityScaling);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetHeadAllRotationAxisSettingsLimitNormalized(ref HeadTrackingSettings headTrackingSettings, float limitNormalizedOnRange);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetHeadAllPositionAxisSettingsSensitivity(ref HeadTrackingSettings headTrackingSettings, float sensitivityScaling);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetHeadAllPositionAxisSettingsLimitNormalized(ref HeadTrackingSettings headTrackingSettings, float limitNormalizedOnRange);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetCenterStabilization(ref HeadTrackingSettings headTrackingSettings, float centerStabilization);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetEyeHeadTrackingRatio(ref GazeHeadMixSettings gazeHeadMixSettings, ref HeadTrackingSettings headTrackingSettings, float eyeHeadTrackingRatio);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetCameraMaxAngleYaw(ref GazeHeadMixSettings gazeHeadMixSettings, ref HeadTrackingSettings headTrackingSettings, float positiveYawLimitDegrees);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetCameraMaxAnglePitchUp(ref GazeHeadMixSettings gazeHeadMixSettings, ref HeadTrackingSettings headTrackingSettings, float positivePitchUpLimitDegrees);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetCameraMaxAnglePitchDown(ref GazeHeadMixSettings gazeHeadMixSettings, ref HeadTrackingSettings headTrackingSettings, float negativePitchDownLimitDegrees);

			#endregion


			#region IFilters

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern ResponsiveFilterSettings GetResponsiveFilterSettings();

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern void SetResponsiveFilterSettings(ResponsiveFilterSettings responsiveFilterSettings);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern AimAtGazeFilterSettings GetAimAtGazeFilterSettings();

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static void SetAimAtGazeFilterSettings(AimAtGazeFilterSettings settings);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static void GetResponsiveFilterGazePoint(out GazePoint gazePoint);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static void GetAimAtGazeFilterGazePoint(out GazePoint gazePoint, out float gazePointStability);

			#endregion


			#region Tuning Values

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int GetTuningValues(out IntPtr tuningValues);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetTuningValue([MarshalAs(UnmanagedType.LPStr)] string name, float value);

			#endregion

		}

		public static class x64
		{
#if DEBUG
			internal const string TobiiGameIntegrationDll = "tobii_gameintegration_x64_d.dll";
#else
			internal const string TobiiGameIntegrationDll = "tobii_gameintegration_x64.dll";
#endif

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetApplicationName([MarshalAs(UnmanagedType.LPStr)] string fullApplicationName);

			#region ITobiiGameIntegrationApi

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsApiInitialized();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void Update();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void Shutdown();

			#endregion


			#region ITrackerController

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetTrackerInfo(out TrackerInfoInterop trackerInfoInterop);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetTrackerInfoByUrl([MarshalAs(UnmanagedType.LPStr)] string trackerUrl, out TrackerInfoInterop trackerInfoInterop);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void UpdateTrackerInfos();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetTrackerInfos(out IntPtr trackerInfosPointer, out int numberOfTrackerInfos);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool TrackHMD();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool TrackRectangle(TobiiRectangle rectangle);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool TrackWindow(IntPtr windowHandle);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool TrackTracker([MarshalAs(UnmanagedType.LPStr)] string trackerUrl);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void StopTracking();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsConnected();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsDeviceEnabled();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsStreamSupported(StreamType stream);

			#endregion


			#region IStreamsProvider

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetLatestGazePoint(out GazePoint gazePoint);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool GetLatestHeadPose(out HeadPose headPose);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int GetGazePoints(out IntPtr gazePoints);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int GetHeadPoses(out IntPtr headPoses);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool IsPresent();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetAutoUnsubscribe(StreamType capability, float timeout);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void UnsetAutoUnsubscribe(StreamType capability);

			// TODO: Implement
			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern bool GetLatestHMDGaze(out HMDGazePoint hmdGazePoint);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern int GetHMDGaze(out IntPtr hmdGazePoints);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern void ConvertGazePoint(GazePoint fromGazePoint, GazePoint toGazePoint, UnitType fromUnit, UnitType toUnit);

			#endregion


			#region IStatisticsRecorder

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr GetStatisticsLiteral(StatisticsLiteral statisticsLiteral);

			#endregion


			#region IExtendedView

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void GetExtendedViewTransformation(out Transformation transformation);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool UpdateExtendedViewSettings(ref ExtendedViewSettings settings);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void ResetExtendedViewDefaultHeadPose();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void ExtendedViewPause([MarshalAs(UnmanagedType.I1)] bool reCenter, float transitionDuration = 0.2f);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void ExtendedViewUnPause(float transitionDuration = 0.2f);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool ExtendedViewIsPaused();

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void GetDefaultExtendedViewSettings(out ExtendedViewSettings settings);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void GetExtendedViewSettings(out ExtendedViewSettings settings);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int PlotCurvePoints([In, Out] Vector2[] allocatedPointsBuf, int maxPoints, ref AxisSettings axisSettings);

			#endregion


			#region ExtendedViewSettings help functions

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetHeadAllRotationAxisSettingsSensitivity(ref HeadTrackingSettings headTrackingSettings, float sensitivityScaling);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetHeadAllRotationAxisSettingsLimitNormalized(ref HeadTrackingSettings headTrackingSettings, float limitNormalizedOnRange);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetHeadAllPositionAxisSettingsSensitivity(ref HeadTrackingSettings headTrackingSettings, float sensitivityScaling);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetHeadAllPositionAxisSettingsLimitNormalized(ref HeadTrackingSettings headTrackingSettings, float limitNormalizedOnRange);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetCenterStabilization(ref HeadTrackingSettings headTrackingSettings, float centerStabilization);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetEyeHeadTrackingRatio(ref GazeHeadMixSettings gazeHeadMixSettings, ref HeadTrackingSettings headTrackingSettings, float eyeHeadTrackingRatio);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetCameraMaxAngleYaw(ref GazeHeadMixSettings gazeHeadMixSettings, ref HeadTrackingSettings headTrackingSettings, float positiveYawLimitDegrees);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetCameraMaxAnglePitchUp(ref GazeHeadMixSettings gazeHeadMixSettings, ref HeadTrackingSettings headTrackingSettings, float positivePitchUpLimitDegrees);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetCameraMaxAnglePitchDown(ref GazeHeadMixSettings gazeHeadMixSettings, ref HeadTrackingSettings headTrackingSettings, float negativePitchDownLimitDegrees);


			#endregion


			#region IFilters

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern ResponsiveFilterSettings GetResponsiveFilterSettings();

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern void SetResponsiveFilterSettings(ResponsiveFilterSettings responsiveFilterSettings);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static extern AimAtGazeFilterSettings GetAimAtGazeFilterSettings();

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static void SetAimAtGazeFilterSettings(AimAtGazeFilterSettings settings);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static void GetResponsiveFilterGazePoint(out GazePoint gazePoint);

			//[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			//internal static void GetAimAtGazeFilterGazePoint(out GazePoint gazePoint, out float gazePointStability);

			#endregion


			#region Tuning Values

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern int GetTuningValues(out IntPtr tuningValues);

			[DllImport(TobiiGameIntegrationDll, CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SetTuningValue([MarshalAs(UnmanagedType.LPStr)] string name, float value);

			#endregion

		}
	}
}