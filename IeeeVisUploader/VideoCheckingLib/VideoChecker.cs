/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Globalization;
using VideoCheckingLib.Models;
using VideoCheckingLib.Utils;

namespace VideoCheckingLib
{
    public class VideoChecker
    {
        public VideoChecker(FfProbe ffProbe)
        {
            FfProbe = ffProbe;
        }

        public FfProbe FfProbe { get; }

        public VideoCheckResult CheckVideo(string path, VideoRequirements requirements)
        {

            
            var ff = FfProbe.RunFfProbe(path);
            
            if (!double.TryParse(ff.format.duration, NumberStyles.Any, CultureInfo.InvariantCulture,
                    out var durationSec))
                durationSec = -1;

            var res = new VideoCheckResult{RawFfProbeOutput = ff};

            if (durationSec < requirements.MinDuration.TotalSeconds)
            {
                res.Duration = DurationResult.TooShort;
            }
            else if (durationSec > requirements.MaxDuration.TotalSeconds)
            {
                res.Duration = DurationResult.TooLong;
            }
            else if (requirements.MaxRecommendedDuration != null &&
                     durationSec > requirements.MaxRecommendedDuration.GetValueOrDefault().TotalSeconds)
            {
                res.Duration = DurationResult.LongerThanRecommended;
            }
            else
            {
                res.Duration = DurationResult.Ok;
            }


            res.IsVideoPackageFormatOk = !(requirements.PackageFormat is { Length: > 0 } &&
                                           ff.format.format_name.Split(',')
                                               .All(s => requirements.PackageFormat
                                                   .All(s2 => !string.Equals(s, s2,
                                                       StringComparison.InvariantCultureIgnoreCase))));
            var videoStreams = ff.streams.Where(v => v.codec_type == "video").ToArray();
            if (videoStreams.Length == 0)
                return res;
            res.HasExactlyOneVideoStream = videoStreams.Length == 1;
            var videoS = videoStreams[0];
            res.RawVideoFfProbeOutput = videoS;
            res.IsVideoCodecOk = !(requirements.VideoCodecs is { Length: > 0 } &&
                                   requirements.VideoCodecs
                                       .All(s2 => !string.Equals(videoS.codec_name, s2,
                                           StringComparison.InvariantCultureIgnoreCase)));
            res.IsFrameRateOk = !(requirements.FrameRates is { Length: > 0 } &&
                                   requirements.FrameRates
                                       .All(s2 => s2 != videoS.r_frame_rate));
            res.AspectRatio = AspectRatioResult.Ok;
            if (!string.IsNullOrEmpty(requirements.AspectRatio))
            {
                if (string.IsNullOrEmpty(videoS.display_aspect_ratio) ||
                    videoS.display_aspect_ratio.ToLowerInvariant().Trim() == "n/a")
                {
                    res.AspectRatio = AspectRatioResult.NotDefined;
                }
                else if (videoS.display_aspect_ratio != requirements.AspectRatio)
                {
                    res.AspectRatio = AspectRatioResult.Different;
                }
            }

            res.IsFrameSizeOk = true;
            if (requirements.FrameSizes is { Length: > 0 })
            {
                res.IsFrameSizeOk = false;
                foreach (var size in requirements.FrameSizes)
                {
                    if (videoS.width == size.Width && videoS.height == size.Height)
                    {
                        res.IsFrameSizeOk = true;
                        break;
                    }
                }
            }

            var audioStreams = ff.streams.Where(v => v.codec_type == "audio").ToArray();
            res.HasTooManyAudioStreams = audioStreams.Length > requirements.MaxNumAudioChannels;
            if (audioStreams.Length != 0)
            {
                res.HasAudioStream = true;
                res.IsAudioCodecOk = true;
                res.RawAudioFfProbeOutput = audioStreams[0];
                
                if (requirements.AudioCodecs is { Length: > 0 })
                {
                    foreach (var audioStream in audioStreams)
                    {
                        var codec = audioStream.codec_name;
                        if (requirements.AudioCodecs.All(s =>
                                !string.Equals(codec, s, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            res.IsAudioCodecOk = false;
                            break;
                        }
                    }
                }
            }

            return res;
        }

        public void AddResultsToErrorsAndWarnings(VideoCheckResult result, VideoRequirements req,
            List<string> errors, List<string> warnings)
        {
            var ff = result.RawFfProbeOutput;
            if (!result.IsVideoPackageFormatOk)
                errors.Add($"the video package format ({ff.format?.format_name}) is not as expected (expected {ArrayToString(req.PackageFormat)})");
            if (!result.IsVideoCodecOk)
                errors.Add($"the video codec ({result.RawVideoFfProbeOutput?.codec_name}) is not as expected (expected {ArrayToString(req.VideoCodecs)})");
            if(!result.HasExactlyOneVideoStream)
                errors.Add("the file has either none or too many video streams");
            if (!result.IsFrameSizeOk)
                errors.Add($"the width and/or height of the video are not as expected (expected {ArrayToString(req.FrameSizes?.Select(f => $"{f.Width}x{f.Height}").ToArray())})");
            if (!result.IsFrameRateOk)
                errors.Add($"the frame rate ({result.RawVideoFfProbeOutput?.r_frame_rate}) is not as expected (expected {ArrayToString(req.FrameRates)})");
            if (!result.HasAudioStream)
                errors.Add("the file does not have an audio stream");
            if (result.HasTooManyAudioStreams)
                errors.Add("the file has too many audio streams");
            if (!result.IsAudioCodecOk)
                errors.Add($"the audio codec ({result.RawAudioFfProbeOutput?.codec_name}) is not as expected (expected {ArrayToString(req.AudioCodecs)})");
            switch (result.AspectRatio)
            {
                case AspectRatioResult.NotDefined:
                    warnings.Add($"the aspect ratio of the video is not explicitly defined");
                    break;
                case AspectRatioResult.Different:
                    errors.Add($"the defined aspect ratio ({result.RawVideoFfProbeOutput?.display_aspect_ratio}) is not as expected (expected {req.AspectRatio})");
                    break;
            }

            switch (result.Duration)
            {
                case DurationResult.TooLong:
                    errors.Add($"the duration of the video ({ff.format?.duration}s) is too long");
                    break;
                case DurationResult.TooShort:
                    errors.Add($"the duration of the video ({ff.format?.duration}s) is too short");
                    break;
                case DurationResult.LongerThanRecommended:
                    warnings.Add($"the duration of the video ({ff.format?.duration}s) is longer than recommended");
                    break;
            }
        }

        public string ArrayToString(string[]? arr)
        {
            if (arr == null || arr.Length == 0)
                return "";
            return string.Join(", or ", arr);
        }

    }
}
