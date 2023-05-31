/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
using VideoCheckingLib.Models;

namespace IeeeVisUploaderWebApp.Models
{
    public class FileTypeDescription
    {
        public string Id { get; set; }
        public string? Name { get; set; }
        public string? FileName { get; set; }
        public string[]? FileExtensions { get; set; }
        public FileType FileType { get; set; }
        public bool IsOptional { get; set; }
        public bool PerformChecks { get; set; }
        public FileCheckInfo? CheckInfo { get; set; }
        
    }

    public class FileCheckInfo
    {
        public long MinFileSize { get; set; }
        public long MaxFileSize { get; set; } = long.MaxValue;
        public VideoRequirements? VideoRequirements { get; set; }
        public FrameSize? ImageMaxSize { get; set; }

    }

    public enum FileType
    {
        Video, Subtitles, Pdf, Image, Text, Other
    }
}
