/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
namespace IeeeVisUploaderWebApp.Models
{
    public class GeneralSettings
    {
        public string BunnyStorageZoneName { get; set; }
        public string BunnyCdnRootUrl { get; set; }
        public string BunnyAccessKey { get; set; }
        public string BunnyUserApiKey { get; set; }
        public string BunnyBasePath { get; set; }
        public string BunnyTokenKey { get; set; }

        public string AuthSignaturePrivateKey { get; set; }
        public string FfprobePath { get; set; }
        public string FfmpegPath { get; set; }

    }
}
