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
    public class VideoRequirements
    {

        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public TimeSpan? MaxRecommendedDuration { get; set; }
        public string[]? PackageFormat { get; set; }
        public string[]? VideoCodecs { get; set; }
        public string[]? AudioCodecs { get; set; }
        public string[]? FrameRates { get; set; }
        public FrameSize[]? FrameSizes { get; set; }
        public int MaxNumAudioChannels { get; set; }
        public string? AspectRatio { get; set; }
        public bool CheckVoiceRecording { get; set; }
        public static VideoRequirements Default()
        {
            return new VideoRequirements
            {
                MinDuration = TimeSpan.FromSeconds(5),
                MaxDuration = TimeSpan.FromHours(5),
                AspectRatio = "16:9",
                FrameRates =new []{"30/1"},
                FrameSizes = new []{ new FrameSize(1920, 1080)},
                MaxNumAudioChannels = 1,
                VideoCodecs = new[] { "h264" },
                AudioCodecs = new[] { "aac" },
                PackageFormat = new[] { "mp4" },
                CheckVoiceRecording = true
            };
        }
    }

    public class FrameSize
    {
        public FrameSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; set; }
        public int Height { get; set; }
    }
}

