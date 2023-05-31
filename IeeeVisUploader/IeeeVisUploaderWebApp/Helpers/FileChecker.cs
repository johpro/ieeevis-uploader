/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
using IeeeVisUploaderWebApp.Models;
using VideoCheckingLib;
using VideoCheckingLib.Utils;

namespace IeeeVisUploaderWebApp.Helpers
{
    public class FileChecker
    {
        private readonly FfProbe _ffProbe;
        private readonly AudioAnalyzer _audioAnalyzer;
        private readonly VideoChecker _videoChecker;
        private readonly AudioChecker _audioChecker;
        private readonly PngImageChecker _imageChecker;
        private readonly SubtitlesChecker _subtitlesChecker;

        public FileChecker()
        {
            _ffProbe = new FfProbe(DataProvider.Settings.FfprobePath);
            _audioAnalyzer = new AudioAnalyzer(DataProvider.Settings.FfmpegPath);
            _videoChecker = new VideoChecker(_ffProbe);
            _audioChecker = new AudioChecker(_audioAnalyzer);
            _imageChecker = new PngImageChecker();
            _subtitlesChecker = new SubtitlesChecker();
        }

        public void PerformChecks(string path, CollectedFile file, FileTypeDescription fileTypeDescription)
        {
            file.Errors ??= new();
            file.Warnings ??= new();
            var videoReq = fileTypeDescription.CheckInfo?.VideoRequirements;
            if (videoReq != null &&
                fileTypeDescription.FileType == FileType.Video)
            {
                var res = _videoChecker.CheckVideo(path, videoReq);
                _videoChecker.AddResultsToErrorsAndWarnings(res, videoReq, file.Errors, file.Warnings);
                var ares = _audioChecker.CheckAudio(path);
                _audioChecker.AddResultsToErrorsAndWarnings(ares, file.Errors, file.Warnings);
                return;
            }

            string reason = "";
            switch (fileTypeDescription.FileType)
            {
                case FileType.Subtitles:
                    if (file.FileName.EndsWith(".sbv", StringComparison.OrdinalIgnoreCase))
                    {
                        if(!_subtitlesChecker.CheckSbvSubtitles(path, out reason))
                            file.Errors.Add(reason);
                    }
                    else
                    {
                        if (!_subtitlesChecker.CheckSrtSubtitles(path, out reason))
                            file.Errors.Add(reason);
                    }
                    break;
                case FileType.Pdf:
                    break;
                case FileType.Image:
                    if(!_imageChecker.CheckImage(path, fileTypeDescription.CheckInfo?.ImageMaxSize, out reason))
                        file.Errors.Add(reason);
                    break;
                case FileType.Text:
                    break;
                case FileType.Other:
                    break;
            }
        }

    }
}
