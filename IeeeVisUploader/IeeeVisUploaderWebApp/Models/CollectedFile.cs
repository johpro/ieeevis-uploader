/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
namespace IeeeVisUploaderWebApp.Models
{
    public class CollectedFile
    {
        public CollectedFile(string parentUid, string fileTypeId, string name)
        {
            ParentUid = parentUid;
            FileTypeId = fileTypeId;
            Name = name;
        }

        public string ParentUid { get; set; }
        public bool IsPresent { get; set; }
        public string FileTypeId { get; set; }
        public string Name { get; set; }
        public string? FileName { get; set; }
        public long FileSize { get; set; }
        public string? DownloadUrl { get; set; }
        public string? Checksum { get;set; }
        public List<string>? Errors { get; set; }
        public List<string>? Warnings { get; set; }
        public DateTime? LastUploaded { get; set; }
        public DateTime? LastChecked { get; set; }
        public string? RawDownloadUrl { get; set; }


        public CollectedFile Clone()
        {
            return new CollectedFile(ParentUid, FileTypeId, Name)
            {
                FileName = FileName,
                FileSize = FileSize,
                DownloadUrl = DownloadUrl,
                RawDownloadUrl = RawDownloadUrl,
                Errors = Errors?.ToList(),
                Warnings = Warnings?.ToList(),
                Checksum = Checksum,
                IsPresent = IsPresent,
                LastUploaded = LastUploaded,
                LastChecked = LastChecked
            };
        }
    }
}
