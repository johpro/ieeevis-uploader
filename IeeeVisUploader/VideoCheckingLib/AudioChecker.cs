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
using VideoCheckingLib.Models;
using VideoCheckingLib.Utils;

namespace VideoCheckingLib
{
    public class AudioChecker
    {
        public AudioChecker(AudioAnalyzer audioAnalyzer)
        {
            AudioAnalyzer = audioAnalyzer;
        }

        public AudioAnalyzer AudioAnalyzer { get; }


        public AudioCheckResult CheckAudio(string path)
        {
            var (amplitudeHist, fftDbsHighAmp, fftDbsLowAmp) = AudioAnalyzer.GetAmplitudeHistogramAndFftSum(path);
            var amp = AudioAnalyzer.AnalyzeAmplitudeHistogram(amplitudeHist);
            var fft = AudioAnalyzer.AnalyzeFft(fftDbsHighAmp, fftDbsLowAmp);
            return DetermineQuality(amp, fft);
        }


        private static AudioCheckResult DetermineQuality(AmplitudeResult amp, FftResult fft)
        {
            var res = new AudioCheckResult();
            res.Volume = amp.AverageDb switch
            {
                < -28 => Quality.Bad,
                < -24 => Quality.Medium,
                _ => Quality.Good
            };
            res.Clipping = amp.ClippingPercentage switch
            {
                > 0.2 => Quality.Bad,
                > 0.05 => Quality.Medium,
                _ => Quality.Good
            };
            if (res.Volume != Quality.Bad && fft.SignalToNoise < 11)
                res.BackgroundNoise = Quality.Bad;
            if (amp.AverageDbWhenQuieter > -45 && amp.AboveNoisePercentage > 0.95 && fft.SignalToNoise < 20)
                res.BackgroundNoise = Quality.Medium;
            return res;
        }

        public void AddResultsToErrorsAndWarnings(AudioCheckResult result,
            List<string> errors, List<string> warnings)
        {
            switch (result.BackgroundNoise)
            {
                case Quality.Medium:
                    warnings.Add("the audio seems to have some background noise");
                    break;
                case Quality.Bad:
                    errors.Add("the audio has too much background noise");
                    break;
            }

            switch (result.Clipping)
            {
                case Quality.Medium:
                    warnings.Add("the audio signal sometimes oversteers");
                    break;
                case Quality.Bad:
                    errors.Add("the audio signal oversteers too often, try to stay below -5db");
                    break;
            }

            switch (result.Volume)
            {
                case Quality.Medium:
                    warnings.Add("the volume of the audio seems to be a bit low on average");
                    break;
                case Quality.Bad:
                    errors.Add("the volume of the audio is too low on average");
                    break;
            }
        }
    }
}
