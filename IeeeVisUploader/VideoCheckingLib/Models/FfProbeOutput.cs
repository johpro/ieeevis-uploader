/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace VideoCheckingLib.Models
{

    public class FfProbeOutput
    {
        public Stream[] streams { get; set; }
        public Format format { get; set; }
    }

    public class Format
    {
        public string filename { get; set; }
        public int nb_streams { get; set; }
        public int nb_programs { get; set; }
        public string format_name { get; set; }
        public string format_long_name { get; set; }
        public string start_time { get; set; }
        public string duration { get; set; }
        public string size { get; set; }
        public string bit_rate { get; set; }
        public int probe_score { get; set; }
        public Tags tags { get; set; }
    }

    public class Tags
    {
        public string major_brand { get; set; }
        public string minor_version { get; set; }
        public string compatible_brands { get; set; }
        public DateTime creation_time { get; set; }
    }

    public class Stream
    {
        public int index { get; set; }
        public string codec_name { get; set; }
        public string codec_long_name { get; set; }
        public string profile { get; set; }
        public string codec_type { get; set; }
        public string codec_tag_string { get; set; }
        public string codec_tag { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int coded_width { get; set; }
        public int coded_height { get; set; }
        public int closed_captions { get; set; }
        public int film_grain { get; set; }
        public int has_b_frames { get; set; }
        public string? sample_aspect_ratio { get; set; }
        public string? display_aspect_ratio { get; set; }
        public string pix_fmt { get; set; }
        public int level { get; set; }
        public string chroma_location { get; set; }
        public string field_order { get; set; }
        public int refs { get; set; }
        public string is_avc { get; set; }
        public string nal_length_size { get; set; }
        public string id { get; set; }
        public string r_frame_rate { get; set; }
        public string avg_frame_rate { get; set; }
        public string time_base { get; set; }
        public long start_pts { get; set; }
        public string start_time { get; set; }
        public long duration_ts { get; set; }
        public string duration { get; set; }
        public string bit_rate { get; set; }
        public string bits_per_raw_sample { get; set; }
        public string nb_frames { get; set; }
        public long extradata_size { get; set; }
        public Disposition disposition { get; set; }
        public StreamTags tags { get; set; }
        public string sample_fmt { get; set; }
        public string sample_rate { get; set; }
        public int channels { get; set; }
        public string channel_layout { get; set; }
        public int bits_per_sample { get; set; }
    }

    public class Disposition
    {
        public int _default { get; set; }
        public int dub { get; set; }
        public int original { get; set; }
        public int comment { get; set; }
        public int lyrics { get; set; }
        public int karaoke { get; set; }
        public int forced { get; set; }
        public int hearing_impaired { get; set; }
        public int visual_impaired { get; set; }
        public int clean_effects { get; set; }
        public int attached_pic { get; set; }
        public int timed_thumbnails { get; set; }
        public int captions { get; set; }
        public int descriptions { get; set; }
        public int metadata { get; set; }
        public int dependent { get; set; }
        public int still_image { get; set; }
    }

    public class StreamTags
    {
        public DateTime creation_time { get; set; }
        public string language { get; set; }
        public string handler_name { get; set; }
        public string vendor_id { get; set; }
        public string encoder { get; set; }
    }

}
