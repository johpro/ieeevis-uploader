/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
namespace IeeeVisUploaderWebApp.Models
{
    public class BunnyLoginResponse
    {
        public string? Token { get; set; }
        public string? Message { get; set; }
        public int? AuthStatus { get; set; }
    }
}
