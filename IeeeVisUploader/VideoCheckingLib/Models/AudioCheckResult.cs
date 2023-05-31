/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
using VideoCheckingLib.Utils;

namespace VideoCheckingLib.Models;

public class AudioCheckResult
{
    public Quality Volume { get; set; }
    public Quality Clipping { get; set; }
    public Quality BackgroundNoise { get; set; }



}