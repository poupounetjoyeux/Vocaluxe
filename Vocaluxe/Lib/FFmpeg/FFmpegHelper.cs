using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.FFmpeg
{
    internal static class FFmpegHelper
    {
        public const int IOBufferSize = 4096;

        // https://ffmpeg.org/doxygen/trunk/codec_8h_source.html#l00420
        public const int AvCodecHwConfigMethodHwDeviceCtx = 0x01;

        private static readonly HashSet<AVHWDeviceType> _HardwareDeviceTypes = new();
        public static IReadOnlyCollection<AVHWDeviceType> HardwareDeviceTypes => _HardwareDeviceTypes;
        public static bool CanUseHardwareAcceleration => _HardwareDeviceTypes.Count > 0;

        internal static void PrepareFFmpeg()
        {
            var ffmpegPath = CConfig.Config.FFmpeg.FFmpegPath;
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                throw new ArgumentException("FFmpeg path was not provided in configuration");
            }

            ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), ffmpegPath);
            ffmpeg.RootPath = ffmpegPath;
            var version = _TryGetVersion();
            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentException($"Unable to find FFmpeg binaries in {ffmpegPath}");
            }

            CLog.Information($"FFmpeg binaries version {version} found in {ffmpegPath}");
            _RegisterHardwareAccelerationDeviceTypes();
        }

        private static void _RegisterHardwareAccelerationDeviceTypes()
        {
            if (CConfig.Config.FFmpeg.EnableHardwareAcceleration == EOffOn.TR_CONFIG_OFF)
            {
                CLog.Information("Hardware acceleration is disabled");
                return;
            }

            foreach (var hardwareType in _RetrieveAvailableHardwareTypes())
            {
                _HardwareDeviceTypes.Add(hardwareType);
                CLog.Information($"FFmpeg hardware type {ffmpeg.av_hwdevice_get_type_name(hardwareType)} is usable on your machine");
            }

            if (!CanUseHardwareAcceleration)
            {
                CLog.Information("Hardware acceleration is enabled, but there is no device available for that on your machine");
            }
        }

        private static IEnumerable<AVHWDeviceType> _RetrieveAvailableHardwareTypes()
        {
            var type = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE;
            while ((type = ffmpeg.av_hwdevice_iterate_types(type)) != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
            {
                yield return type;
            }
        }

        private static string _TryGetVersion()
        {
            try
            {
                return ffmpeg.av_version_info();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
