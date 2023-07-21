/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCheckingLib.Models
{
    public class VideoCheckResult
    {
        public DurationResult Duration { get; set; }
        public bool IsVideoPackageFormatOk { get; set; }
        public bool IsVideoCodecOk { get; set; }
        public bool IsAudioCodecOk { get; set; }
        public bool IsFrameSizeOk { get; set; }
        public bool IsFrameRateOk { get; set; }
        public AspectRatioResult AspectRatio { get; set; }
        public bool HasExactlyOneVideoStream { get; set; }
        public bool HasAudioStream { get; set; }
        public bool HasTooManyAudioStreams { get; set; }
        public FfProbeOutput RawFfProbeOutput { get; set; }
        public Stream? RawVideoFfProbeOutput { get; set; }
        public Stream? RawAudioFfProbeOutput { get; set; }
    }

    public enum DurationResult
    {
        Ok, TooLong, TooShort, LongerThanRecommended
    }

    public enum AspectRatioResult
    {
        Ok, NotDefined, Different
    }
}
