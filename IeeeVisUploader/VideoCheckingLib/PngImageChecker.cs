/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using VideoCheckingLib.Models;

namespace VideoCheckingLib
{
    public class PngImageChecker
    {

        private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4e, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public bool CheckImage(string path, FrameSize? maxSize, out string reason)
        {
            var len = new FileInfo(path).Length;
            if (len is < 33 or > 20 * 1024 * 1024)
            {
                reason = "the file is too small or too big";
                return false;
            }

            var buffer = new byte[33];
            using var stream = File.OpenRead(path);
            if (stream.Read(buffer) != buffer.Length)
            {
                reason = "the file could not be parsed";
                return false;
            }

            if (!buffer.AsSpan(0, PngHeader.Length).SequenceEqual(PngHeader))
            {
                reason = "the file is not a valid PNG image file";
                return false;
            }

            var widthSpan = buffer.AsSpan(16, 4);
            widthSpan.Reverse();
            var heightSpan = buffer.AsSpan(20, 4);
            heightSpan.Reverse();
            var width = BitConverter.ToInt32(widthSpan);
            var height = BitConverter.ToInt32(heightSpan);
            if (width <= 1 || height <= 1)
            {
                reason = "the width and/or height of the image is too small";
                return false;
            }

            if (maxSize != null)
            {
                if (width > maxSize.Width || height > maxSize.Height)
                {
                    reason = $"the dimensions of the image ({width} x {height}) are not as expected";
                    return false;
                }
            }

            reason = "";
            return true;
        }
    }
}
