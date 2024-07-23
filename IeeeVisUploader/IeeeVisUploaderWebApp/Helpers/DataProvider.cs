/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
using System.Reflection;
using System.Text;
using System.Text.Json;
using IeeeVisUploaderWebApp.Models;
using VideoCheckingLib.Models;

namespace IeeeVisUploaderWebApp.Helpers
{
    public class DataProvider
    {

        public static GeneralSettings Settings { get; set; }
        public static Dictionary<string, EventItem> Events { get; set; }
        public static Dictionary<string, FileTypeDescription> FileTypes { get; set; }
        public static string ConfigPath { get; set; }
        public static CollectedFilesStore CollectedFiles { get; set; }


        public static void FillDefaults()
        {
            var types = new Dictionary<string, FileTypeDescription>();
            types.Add("video-full", VideoBaseDescription());
            var vbd = VideoBaseDescription();
            vbd.Id = "video-short";
            vbd.CheckInfo.VideoRequirements.MaxRecommendedDuration = TimeSpan.FromMinutes(7);
            vbd.CheckInfo.VideoRequirements.MaxDuration = TimeSpan.FromMinutes(9);
            types.Add(vbd.Id, vbd);

            vbd = VideoBaseDescription();
            vbd.Id = "video-other";
            vbd.CheckInfo.VideoRequirements.MaxRecommendedDuration = null;
            vbd.CheckInfo.VideoRequirements.MaxDuration = TimeSpan.FromMinutes(60);
            types.Add(vbd.Id, vbd);

            vbd = VideoBaseDescription();
            vbd.FileName = "Preview";
            vbd.Name = "Video Preview";
            vbd.Id = "video-ff";
            vbd.CheckInfo.MaxFileSize = 30 * 1024 * 1024;
            vbd.CheckInfo.VideoRequirements.MaxRecommendedDuration = null;
            vbd.CheckInfo.VideoRequirements.MaxDuration = TimeSpan.FromSeconds(26);
            vbd.CheckInfo.VideoRequirements.MinDuration = TimeSpan.FromSeconds(15);
            types.Add(vbd.Id, vbd);

            vbd = new FileTypeDescription
            {
                Id = "video-full-subs",
                Name = "Presentation Video Subtitles",
                FileName = "Presentation",
                FileExtensions = new[] { "srt", "sbv" },
                FileType = FileType.Subtitles,
                PerformChecks = true,
                CheckInfo = new FileCheckInfo
                {
                    MinFileSize = 10,
                    MaxFileSize = 2 * 1024 * 1024
                }
            };

            types.Add(vbd.Id, vbd);

            vbd = new FileTypeDescription
            {
                Id = "video-ff-subs",
                Name = "Video Preview Subtitles",
                FileName = "Preview",
                FileExtensions = new[] { "srt", "sbv" },
                FileType = FileType.Subtitles,
                PerformChecks = true,
                CheckInfo = new FileCheckInfo
                {
                    MinFileSize = 10,
                    MaxFileSize = 2 * 1024 * 1024
                }
            };

            types.Add(vbd.Id, vbd);

            vbd = new FileTypeDescription
            {
                Id = "image",
                Name = "Representative Image",
                FileName = "Image",
                FileExtensions = new[] { "png" },
                FileType = FileType.Image,
                PerformChecks = true,
                CheckInfo = new FileCheckInfo
                {
                    MinFileSize = 10,
                    MaxFileSize = 5 * 1024 * 1024,
                    ImageMaxSize = new FrameSize(1920, 1080)
                }
            };

            types.Add(vbd.Id, vbd);

            vbd = new FileTypeDescription
            {
                Id = "image-caption",
                Name = "Representative Image Caption",
                FileName = "Image",
                FileExtensions = new[] { "txt" },
                FileType = FileType.Text,
                PerformChecks = true,
                CheckInfo = new FileCheckInfo
                {
                    MinFileSize = 10,
                    MaxFileSize = 100 * 1024
                }
            };

            types.Add(vbd.Id, vbd);


            var events = new Dictionary<string, EventItem>();
            var it = new EventItem
            {
                EventId = "v-full",
                FilesToCollect = new()
                {
                    "video-full",
                    "video-full-subs",
                    "video-ff",
                    "video-ff-subs",
                    "image",
                    "image-caption"
                }
            };
            events.Add(it.EventId, it);

            it = new EventItem
            {
                EventId = "v-tvcg",
                FilesToCollect = new()
                {
                    "video-full",
                    "video-full-subs",
                    "video-ff",
                    "video-ff-subs",
                    "image",
                    "image-caption"
                }
            };
            events.Add(it.EventId, it);

            it = new EventItem
            {
                EventId = "v-cga",
                FilesToCollect = new()
                {
                    "video-full",
                    "video-full-subs",
                    "video-ff",
                    "video-ff-subs",
                    "image",
                    "image-caption"
                }
            };
            events.Add(it.EventId, it);

            it = new EventItem
            {
                EventId = "v-vr",
                FilesToCollect = new()
                {
                    "video-full",
                    "video-full-subs",
                    "video-ff",
                    "video-ff-subs",
                    "image",
                    "image-caption"
                }
            };
            events.Add(it.EventId, it);

            it = new EventItem
            {
                EventId = "v-short",
                FilesToCollect = new()
                {
                    "video-short",
                    "video-short-subs",
                    "video-ff",
                    "video-ff-subs",
                    "image",
                    "image-caption"
                }
            };
            events.Add(it.EventId, it);

            it = new EventItem
            {
                EventId = "v-test",
                FilesToCollect = new()
                {
                    "video-full",
                    "video-full-subs",
                    "video-ff",
                    "video-ff-subs",
                    "image",
                    "image-caption"
                }
            };
            events.Add(it.EventId, it);
        }

        public static FileTypeDescription VideoBaseDescription()
        {
            return new FileTypeDescription
            {
                FileName = "Presentation",
                Name = "Presentation Video",
                FileExtensions = new[] { "mp4" },
                FileType = FileType.Video,
                Id = "video-full",
                PerformChecks = true,
                CheckInfo = new FileCheckInfo
                {
                    MaxFileSize = 500 * 1024 * 1024,
                    MinFileSize = 1024,
                    VideoRequirements = new VideoRequirements
                    {
                        VideoCodecs = new[] { "h264" },
                        AudioCodecs = new[] { "aac" },
                        PackageFormat = new[] { "mp4" },
                        MaxDuration = TimeSpan.FromMinutes(12),
                        MaxRecommendedDuration = TimeSpan.FromMinutes(9),
                        MinDuration = TimeSpan.FromMinutes(1),
                        AspectRatio = "16:9",
                        FrameRates = new[] { "30/1" },
                        FrameSizes = new[] { new FrameSize(1920, 1080) },
                        MaxNumAudioChannels = 1,
                        CheckVoiceRecording = true
                    }
                }
            };
        }

        /*MinDuration = TimeSpan.FromSeconds(5),
                MaxDuration = TimeSpan.FromHours(5),
                AspectRatio = "16:9",
                FrameRate = 30,
                Width = 1920,
                Height = 1080,
                MaxFileSizeBytes = 500 * 1024 * 1024,
                MaxNumAudioChannels = 1,
                VideoCodecs = new[] { "h264" },
                AudioCodecs = new[] { "aac" },
                PackageFormat = new[] { "mp4" },
                VideoFileExtensions = new[] { "mp4" },
                CheckVoiceRecording = true*/

        public static void Initialize()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            path = Path.Combine(path, "config");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            ConfigPath = path;
            if (Settings == null)
            {
                foreach (var name in Assembly.GetExecutingAssembly().GetManifestResourceNames())
                {
                    Console.WriteLine(name);
                }
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("IeeeVisUploaderWebApp.settings.json");
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    Settings = JsonSerializer.Deserialize<GeneralSettings>(reader.ReadToEnd());
                }
            }

            if (Events == null)
            {
                Events = new Dictionary<string, EventItem>();
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("IeeeVisUploaderWebApp.events.json");
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    var events = JsonSerializer.Deserialize<EventItem[]>(reader.ReadToEnd());
                    foreach (var item in events)
                    {
                        Events[item.EventId] = item;
                    }
                }
            }

            if (FileTypes == null)
            {
                FileTypes = new Dictionary<string, FileTypeDescription>();

                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("IeeeVisUploaderWebApp.fileTypes.json");
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    var items = JsonSerializer.Deserialize<FileTypeDescription[]>(reader.ReadToEnd());
                    foreach (var item in items)
                    {
                        FileTypes[item.Id] = item;
                    }
                }
            }

            if (CollectedFiles == null)
            {
                // Might need to fix this too
                var fn = Path.Combine(ConfigPath, "collectedFiles.json");
                CollectedFiles = new(fn);
            }
        }


    }
}
