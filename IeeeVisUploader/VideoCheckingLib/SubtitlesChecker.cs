/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace VideoCheckingLib
{
    public class SubtitlesChecker
    {
        public bool CheckSbvSubtitles(string path, out string reason)
        {
            /*
                0:00:00.000,0:00:07.000
                >> TIM: So its 1976 I'm coming to the end
                of my career at Oxford learning physics -

                0:00:08.950,0:00:15.950
                I really don't know anybody who's done physics
                at a PhD level so I don't have a role model            
             */
            var len = new FileInfo(path).Length;
            if (len is < 20 or > 10 * 1024 * 1024)
            {
                reason = "the file is too small or too big";
                return false;
            }
            var lines = File.ReadAllLines(path);
            if (lines.Length < 2)
            {
                reason = "we expect at least one subtitle line";
                return false;
            }
            if (lines[0].Length is < 23 or > 30)
            {
                reason = "the time stamps are not in the right format";
                return false;
            }
            if (lines[0].Count(c => c == ':') != 4 || lines[0].Count(c => c == ',') != 1)
            {
                reason = "the time stamps are not in the right format";
                return false;
            }

            reason = "";
            return true;
        }

        public bool CheckSrtSubtitles(string path, out string reason)
        {
            /*
                1
                00:02:16,612 --> 00:02:19,376
                Senator, we're making
                our final approach into Coruscant.

                2
                00:02:19,482 --> 00:02:21,609
                Very good, Lieutenant.
             */
            
            var len = new FileInfo(path).Length;
            if (len is < 20 or > 10 * 1024 * 1024)
            {
                reason = "the file is too small or too big";
                return false;
            }

            var lines = File.ReadAllLines(path);
            if (lines.Length < 3)
            {
                reason = "we expect at least one subtitle line";
                return false;
            }

            if (lines[0] != "1")
            {
                reason = "the first subtitle line has to be marked as 1";
                return false;
            }
            if (lines[1].IndexOf(" --> ", StringComparison.Ordinal) == -1)
            {
                reason = "the time stamps are not in the right format";
                return false;
            }
            if (lines[1].Length < 25 || lines[1].Count(c => c == ':') < 4 || lines[1].Count(c => c == ',') < 2)
            {
                reason = "the time has to be specified in the hours:minutes:seconds,milliseconds (00:00:00,000) format";
                return false;
            }
            
            reason = "";
            return true;
        }


    }
}
