/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
namespace IeeeVisUploaderWebApp.Models
{
    public class EventItem
    {
        public string EventId { get; set; }
        public List<string> FilesToCollect { get; set; }
        public List<string>? FilesBlockedForUpload { get; set; }
    }
    
}
