/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using VideoCheckingLib.Models;

namespace VideoCheckingLib.Utils
{
    public class FfProbe
    {
        private readonly string _ffprobePath;

        public FfProbe() : this("ffprobe.exe"){}

        public FfProbe(string ffprobePath)
        {
            _ffprobePath = ffprobePath;
        }

        public FfProbeOutput RunFfProbe(string filename)
        {
            var json = HelperMethods.RunExternalExe(_ffprobePath,
                $"-v quiet -hide_banner -print_format json -show_format -show_streams \"{filename}\"");
            return JsonSerializer.Deserialize<FfProbeOutput>(json) ?? throw new Exception("ffprobe did not return json");
        }

        
    }
}
