using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubtitlesConverterApp
{
    public static class SubtitlesConverter
    {

        /*
         WEB VTT:

               WEBVTT
           
               00:01.000 --> 00:04.000
               - Never drink liquid nitrogen.
               
               00:05.000 --> 00:09.000
               - It will perforate your stomach.
               - You could die.
         */
        public static IEnumerable<string> ConvertSbvToVtt(IEnumerable<string> lines)
        {
            /*
             * SBV:
                0:00:00.000,0:00:06.000
                We present Vimo, a visualization tool to 
                analyze connectivity motifs and brain networks.

                0:00:06.960,0:00:15.180
                Users sketch a motif and analyze particular 
                motif instances in 3D. Our custom focus

                0:00:15.180,0:00:20.700
                and context method helps to understand how the 
                motif structure is embedded in a set of neurons.

             */
            var nextBlockUp = true;
            yield return "WEBVTT";
            yield return "";
            foreach (var l in lines)
            {

                if (nextBlockUp && l.Contains(':') && l.Contains(',') && char.IsAsciiDigit(l[0]))
                {
                    //time code
                    var splitIdx = l.IndexOf(',');
                    yield return l[..splitIdx] + " --> " + l[(splitIdx+1)..];
                    nextBlockUp = false;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(l))
                {
                    nextBlockUp = true;
                }

                yield return l;
            }
        }

        public static IEnumerable<string> ConvertSrtToVtt(IEnumerable<string> lines)
        {
            /*
             * SRT:
               1
               00:00:00,498 --> 00:00:02,827
               - Here's what I love most
               about food and diet.
               
               2
               00:00:02,827 --> 00:00:06,383
               We all eat several times a day,
               and we're totally in charge
               
               3
               00:00:06,383 --> 00:00:09,427
               of what goes on our plate
               and what stays off.

             */
            var nextBlockUp = true;
            yield return "WEBVTT";
            yield return "";
            foreach (var l in lines)
            {

                if (nextBlockUp && l.IndexOf("-->", StringComparison.Ordinal) != -1 && char.IsAsciiDigit(l[0]))
                {
                    //time code
                    yield return l.Replace(',', '.');
                    nextBlockUp = false;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(l))
                {
                    nextBlockUp = true;
                }

                yield return l;
            }
        }
    }
}
