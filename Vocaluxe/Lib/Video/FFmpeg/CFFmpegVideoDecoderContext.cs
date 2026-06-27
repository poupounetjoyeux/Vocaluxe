using System.Linq;
using FFmpeg.AutoGen;
using Vocaluxe.Base;
using Vocaluxe.Lib.FFmpeg;
using VocaluxeLib;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Video.FFmpeg
{
    internal unsafe class CFFmpegVideoDecoderContext : CFFmpegDecoderContextBase
    {
        public const AVPixelFormat OutputFormat = AVPixelFormat.AV_PIX_FMT_BGRA;
        private SwsContext* _SwsContext;

        private AVCodecContext_get_format _GetFormatCallback;
        private AVPixelFormat _HardwarePixelFormat = AVPixelFormat.AV_PIX_FMT_NONE;
        private AVPixelFormat _HardwareOutputFormat = AVPixelFormat.AV_PIX_FMT_NONE;
        private AVBufferRef* _HardwareDeviceContext;

        private byte_ptrArray4 _Buffer;
        private int_array4 _BufferLineSize;

        public int FrameBufferSize { get; private set; }

        public byte* DecodedFrameBuffer => _Buffer[0];

        public int FrameWidth { get; private set; }

        public int FrameHeight { get; private set; }

        protected override AVMediaType _MediaType => AVMediaType.AVMEDIA_TYPE_VIDEO;

        private static (int, int) _ComputeFinalSize(AVStream* avStream)
        {
            var streamHeight = avStream->codecpar->height;
            if (CConfig.Config.FFmpeg.VideoDownscaleResolution == EVideoDownscaleResolution.TR_CONFIG_SCALING_DISABLED)
            {
                return (avStream->codecpar->width, streamHeight);
            }

            var maxHeight = (int)CConfig.Config.FFmpeg.VideoDownscaleResolution;
            decimal sizeFactor = 1;
            if (streamHeight > maxHeight)
            {
                sizeFactor = (decimal)maxHeight / streamHeight;
            }

            return ((int)(avStream->codecpar->width * sizeFactor), (int)(streamHeight * sizeFactor));
        }

        private static AVCodecHWConfig* _TryGetHardwareCodecConfig(AVCodec* codec)
        {
            var preferredType = CConfig.Config.FFmpeg.PreferredHardwareAccelerationType;
            AVCodecHWConfig* bestMatch = null;
            for (var i = 0; ; i++)
            {
                var config = ffmpeg.avcodec_get_hw_config(codec, i);
                if (config == null)
                {
                    break;
                }

                if ((config->methods & FFmpegHelper.AvCodecHwConfigMethodHwDeviceCtx) != FFmpegHelper.AvCodecHwConfigMethodHwDeviceCtx ||
                    !FFmpegHelper.HardwareDeviceTypes.Contains(config->device_type) || config->pix_fmt == AVPixelFormat.AV_PIX_FMT_NONE)
                {
                    continue;
                }

                if (preferredType != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE && config->device_type == preferredType)
                {
                    return config;
                }

                if (bestMatch == null)
                {
                    bestMatch = config;
                }
            }

            return bestMatch;
        }

        private AVPixelFormat _GetHardwareFormat(AVCodecContext* ctx, AVPixelFormat* pixFormats)
        {
            AVPixelFormat* p;
            for (p = pixFormats; *p != AVPixelFormat.AV_PIX_FMT_NONE; p++)
            {
                if (*p == _HardwarePixelFormat)
                {
                    return *p;
                }
            }

            CLog.Error("Failed to get HW surface format");
            return AVPixelFormat.AV_PIX_FMT_NONE;
        }

        private bool _InitHardwareAccelerationIfPossible(AVCodec* codec, AVCodecContext* codecContext)
        {
            if (!FFmpegHelper.CanUseHardwareAcceleration)
            {
                return true;
            }

            var hardwareCodecConfig = _TryGetHardwareCodecConfig(codec);
            if (hardwareCodecConfig == null)
            {
                CLog.Information("Codec doesn't support hardware decode on your machine");
                return true;
            }

            _HardwarePixelFormat = hardwareCodecConfig->pix_fmt;
            fixed (AVBufferRef** hwDeviceContext = &_HardwareDeviceContext)
            {
                if (ffmpeg.av_hwdevice_ctx_create(hwDeviceContext, hardwareCodecConfig->device_type,
                        null, null, 0) < 0)
                {
                    CLog.Error("Unable to init the HW device context");
                    return false;
                }
            }

            var hwFramesConstraints = ffmpeg.av_hwdevice_get_hwframe_constraints(_HardwareDeviceContext, null);
            if (hwFramesConstraints == null)
            {
                //CLog.Error("Unable to retrieve hardware pixel format constraints");
                //return false;
            }

            /*try
            {
                for (var p = hwFramesConstraints->valid_sw_formats;
                     *p != AVPixelFormat.AV_PIX_FMT_NONE; p++)
                {
                    if (*p == OutputFormat)
                    {
                        _HardwareOutputFormat = OutputFormat;
                        break;
                    }

                    if (_HardwareOutputFormat != AVPixelFormat.AV_PIX_FMT_NONE || ffmpeg.sws_isSupportedInput(*p) <= 0)
                    {
                        continue;
                    }

                    _HardwareOutputFormat = *p;
                }
            }
            finally
            {
                ffmpeg.av_hwframe_constraints_free(&hwFramesConstraints);
            }*/

            _HardwareOutputFormat = OutputFormat;
            if (_HardwareOutputFormat == AVPixelFormat.AV_PIX_FMT_NONE)
            {
                CLog.Error("Unable to get a valid hardware output format");
                return false;
            }

            codecContext->hw_device_ctx = ffmpeg.av_buffer_ref(_HardwareDeviceContext);

            _GetFormatCallback = _GetHardwareFormat;
            codecContext->get_format = _GetFormatCallback;
            return true;
        }

        protected override bool _CustomizeCodecContext(AVCodec* codec, AVCodecContext* codecContext)
        {
            return base._CustomizeCodecContext(codec, codecContext) && _InitHardwareAccelerationIfPossible(codec, codecContext);
        }

        protected override bool _InternalInit(AVStream* avStream, AVCodecContext* codecContext)
        {
            var (width, height) = _ComputeFinalSize(avStream);
            FrameWidth = width;
            FrameHeight = height;

            FrameBufferSize = ffmpeg.av_image_alloc(ref _Buffer, ref _BufferLineSize, width,
                height, OutputFormat, 1);
            if (FrameBufferSize < 0)
            {
                CLog.Error("Unable to alloc frame buffer");
                return false;
            }

            _SwsContext = ffmpeg.sws_getContext(codecContext->width, codecContext->height, _HardwareDeviceContext != null ? _HardwareOutputFormat : codecContext->pix_fmt,
                width, height, OutputFormat, (int)SwsFlags.SWS_BICUBIC, null, null, null);
            if (_SwsContext == null)
            {
                CLog.Error("Unable to alloc SWS context");
                return false;
            }

            return true;
        }

        protected override bool _ProcessFrame(AVFrame* frame)
        {
            AVFrame* hardwareFrame = null;
            AVPixelFormat* formats = null;
            try
            {
                AVFrame* frameToProcess;
                if (_HardwareDeviceContext != null && frame->format == (int)_HardwarePixelFormat)
                {
                    hardwareFrame = ffmpeg.av_frame_alloc();
                    if (hardwareFrame == null)
                    {
                        CLog.Error("Unable to alloc hardware frame");
                        return false;
                    }

                    var test = ffmpeg.av_hwframe_transfer_get_formats(_HardwareDeviceContext, AVHWFrameTransferDirection.AV_HWFRAME_TRANSFER_DIRECTION_FROM, &formats, 0);
                    hardwareFrame->format = (int)_HardwareOutputFormat;

                    if (ffmpeg.av_hwframe_transfer_data(hardwareFrame, frame, 0) < 0)
                    {
                        CLog.Error("Unable to retrieve the hardware frame");
                        return false;
                    }

                    frameToProcess = hardwareFrame;
                }
                else
                {
                    frameToProcess = frame;
                }

                if (ffmpeg.sws_scale(_SwsContext, frameToProcess->data,
                        frameToProcess->linesize,
                        0,
                        frameToProcess->height, _Buffer,
                        _BufferLineSize) < 0)
                {
                    CLog.Error("Error when scaling the video frame");
                    return false;
                }

                return true;
            }
            finally
            {
                if (hardwareFrame != null)
                {
                    ffmpeg.av_frame_free(&hardwareFrame);
                }

                if (formats != null)
                {
                    ffmpeg.av_freep(&formats);
                }
            }
        }

        protected override void _InternalFree()
        {
            if (_HardwareDeviceContext != null)
            {
                fixed (AVBufferRef** hwDeviceContext = &_HardwareDeviceContext)
                {
                    ffmpeg.av_buffer_unref(hwDeviceContext);
                }
            }

            ffmpeg.sws_freeContext(_SwsContext);
            _SwsContext = null;


            if (_Buffer[0] != null)
            {
                fixed (byte_ptrArray4* buffer = &_Buffer)
                {
                    ffmpeg.av_freep(buffer);
                }
            }
        }
    }
}
