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
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VideoCheckingLib.Models;

namespace VideoCheckingLib.Utils
{

    public enum Quality { Good = 0, Medium = 5, Bad = 10}

    public class AmplitudeResult
    {
        public double ClippingPercentage { get; set; }
        public double BestRangePercentage { get; set; }
        public double UnderRangePercentage { get; set; }
        public double AboveNoisePercentage { get; set; }
        public long NumSamples { get; set; }
        public double NoiseOnlyPercentage { get; set; }
        public double AverageDb { get; set; }
        public double AverageDbWhenQuieter { get; set; }
    }

    

    public class FftResult
    {
        public double SignalToNoise { get; set; }
        public double LowFreqMaxDb { get; set; }
        public double HighFreqMaxDb { get; set; }
    }
    public class AudioAnalyzer
    {
        public string PathToFfMpeg { get; }

        public AudioAnalyzer(string pathToFfMpeg = "ffmpeg.exe")
        {
            PathToFfMpeg = pathToFfMpeg;
            if (!File.Exists(pathToFfMpeg))
                throw new Exception("could not find ffmpeg");
        }

        
        public static AmplitudeResult AnalyzeAmplitudeHistogram(long[] hist)
        {
            //voice -10db max, -18db best, -24db min
            var clippingNum = hist[..4].Sum();
            var bestRangeNum = hist[8..26].Sum();
            var underRangeNum = hist[26..].Sum();
            const int noiseDbTh = 40; //55;
            const int silenceDbTh = 60; //55;
            var aboveNoiseNum = hist[..noiseDbTh].Sum();
            var noiseNum = hist[noiseDbTh..silenceDbTh].Sum();
            var numSamples = hist.Sum();
            var quot = (double)Math.Max(1, numSamples);
            var avgDb = 0d;
            var avgQuot = (double)Math.Max(1, aboveNoiseNum);
            var noiseAvgDb = 0d;
            var noiseAvgQuot = (double)Math.Max(1, noiseNum);
            for (int i = 0; i < noiseDbTh; i++)
            {
                avgDb += -i * hist[i] / avgQuot;
            }
            for (int i = noiseDbTh; i < silenceDbTh; i++)
            {
                noiseAvgDb += -i * hist[i] / noiseAvgQuot;
            }
            return new AmplitudeResult
            {
                BestRangePercentage = bestRangeNum / quot,
                AboveNoisePercentage = aboveNoiseNum / quot,
                NumSamples = numSamples,
                ClippingPercentage = clippingNum / quot,
                UnderRangePercentage = underRangeNum / quot,
                NoiseOnlyPercentage = noiseNum / quot,
                AverageDb = avgDb,
                AverageDbWhenQuieter = noiseAvgDb
            };

        }

        public static FftResult AnalyzeFft(double[] fftDbsHighAmp, double[] fftDbsLowAmp)
        {

            const int lowRangeLength = 50;

            var lowRange = fftDbsHighAmp.AsSpan(0, lowRangeLength);
            var highRange = fftDbsHighAmp.AsSpan(lowRangeLength);
            var srRatio = 0d;
            for (int i = 4; i < lowRangeLength; i++)
            {
                srRatio += fftDbsHighAmp[i] - fftDbsLowAmp[i];
            }

            srRatio /= lowRangeLength;
            var maxDbLowRange = -100d;
            var maxDbHighRange = -100d;
            foreach (var v in lowRange)
            {
                if (v > maxDbLowRange)
                    maxDbLowRange = v;
            }
            foreach (var v in highRange)
            {
                if (v > maxDbHighRange)
                    maxDbHighRange = v;
            }

            return new FftResult
            {
                HighFreqMaxDb = maxDbHighRange,
                LowFreqMaxDb = maxDbLowRange,
                SignalToNoise = srRatio
            };

        }

        public long[] GetAmplitudeHistogram(string path)
        {
            //ffmpeg -hide_banner -i "<path>" -vn -ar 44100 -ac 1 -f f32le -

            //var totCount = 0L;
            const int bufferSize = 2048;
            var histogram = new long[101];
            void OnStream(StreamReader reader)
            {
                const int maxBytesToRead = bufferSize * sizeof(float);
                var buffer = new byte[maxBytesToRead];
                var stream = reader.BaseStream;
                var bytesToRead = maxBytesToRead;
                var offset = 0;
                int len;
                while ((len = stream.Read(buffer, offset, bytesToRead)) != 0)
                {
                    bytesToRead -= len;
                    offset += len;
                    if (bytesToRead > 0)
                    {
                        continue;
                    }

                    offset = 0;
                    bytesToRead = maxBytesToRead;
                    //buffer is complete
                    var floatSpan = MemoryMarshal.Cast<byte, float>(buffer.AsSpan());

                    var maxVal = 0f;
                    for (int i = 0; i < floatSpan.Length; i++)
                    {
                        var val = Math.Abs(floatSpan[i]);
                        if (val > maxVal)
                            maxVal = val;
                    }
                    var db = 20 * Math.Log10(Math.Max(float.Epsilon, maxVal));
                    histogram[Math.Min(histogram.Length - 1, (int)Math.Abs(db))]++;
                }
            }

            HelperMethods.RunExternalExe(PathToFfMpeg,
                $"-hide_banner -i \"{path}\" " +
                $"-vn -ar 44100 -ac 1 -f f32le -", OnStream);
            return histogram;
        }

        public (long[] amplitudeHist, double[] fftDbsHighAmp, double[] fftDbsLowAmp) GetAmplitudeHistogramAndFftSum(string path)
        {
            //ffmpeg -hide_banner -i "<path>" -vn -ar 44100 -ac 1 -f f32le -

            //var totCount = 0L;
            const int bufferSize = 2048;
            var histogram = new long[101];
            var fftSumHighAmp = new double[bufferSize/2];
            var fftSumLowAmp = new double[bufferSize / 2];
            var fft = new Fft(bufferSize);
            void OnStream(StreamReader reader)
            {
                var buffer = new byte[bufferSize * sizeof(float)];
                var complexBuffer = new Complex[bufferSize];
                const int maxBytesToRead = bufferSize * sizeof(float);
                var stream = reader.BaseStream;
                var bytesToRead = maxBytesToRead;
                var offset = 0;
                int len;
                var weightSum = 0d;
                var weightSumLow = 0d;
                var maxMag = 0d;
                while ((len = stream.Read(buffer, offset, bytesToRead)) != 0)
                {
                    bytesToRead -= len;
                    offset += len;
                    if (bytesToRead > 0)
                    {
                        continue;
                    }
                    offset = 0;
                    bytesToRead = maxBytesToRead;
                    //buffer is complete
                    var floatSpan = MemoryMarshal.Cast<byte, float>(buffer.AsSpan());

                    var maxVal = 0f;
                    for (int i = 0; i < floatSpan.Length; i++)
                    {
                        var val = floatSpan[i];
                        complexBuffer[i] = val;
                        val = Math.Abs(val);
                        if (val > maxVal)
                            maxVal = val;
                    }
                    var db = 20 * Math.Log10(Math.Max(float.Epsilon, maxVal));
                    var absDb = Math.Min(histogram.Length - 1, (int)Math.Abs(db));
                    histogram[absDb]++;

                    fft.Compute(complexBuffer);
                    for (var i = 0; i < fftSumHighAmp.Length; i++)
                    {
                        var mag = complexBuffer[i].Magnitude/(bufferSize/2);
                        if (mag > maxMag)
                            maxMag = mag;
                        var bdb = Math.Max(-100, 20 * Math.Log10(Math.Max(float.Epsilon, mag)));
                        floatSpan[i] = (float)bdb;
                    }

                    var weight = 1d/(1+ absDb);
                    var weightLow = 1d / (91 - 3*Math.Min(30, absDb));
                    for (var i = 0; i < fftSumHighAmp.Length; i++)
                    {
                        fftSumHighAmp[i] += floatSpan[i]*weight;
                        fftSumLowAmp[i] += floatSpan[i] * weightLow;
                    }

                    weightSum += weight;
                    weightSumLow += weightLow;
                }

                if (weightSum <= 0) return;

                for (int i = 0; i < fftSumHighAmp.Length; i++)
                {
                    fftSumHighAmp[i] /= weightSum;
                    fftSumLowAmp[i] /= weightSumLow;
                }
                //Trace.WriteLine("max mag: " + maxMag);

            }

            HelperMethods.RunExternalExe(PathToFfMpeg,
                $"-hide_banner -i \"{path}\" " +
                $"-vn -ar 44100 -ac 1 -f f32le -", OnStream);
            return (histogram, fftSumHighAmp, fftSumLowAmp);
        }
    }
}

