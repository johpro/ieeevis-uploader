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
using System.Threading.Tasks;

namespace VideoCheckingLib.Utils
{
    internal static class HelperMethods
    {
        public static string RunExternalExe(string filename, string? arguments = null)
        {
            var process = new Process();

            process.StartInfo.FileName = filename;
            if (!string.IsNullOrEmpty(arguments))
            {
                process.StartInfo.Arguments = arguments;
            }

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;

            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            var stdOutput = new StringBuilder();
            process.OutputDataReceived += (sender, args) => stdOutput.AppendLine(args.Data); // Use AppendLine rather than Append since args.Data is one line of output, not including the newline character.

            string stdError;
            try
            {
                process.Start();
                process.BeginOutputReadLine();
                stdError = process.StandardError.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                throw new Exception("OS error while executing " + Format(filename, arguments) + ": " + e.Message, e);
            }

            if (process.ExitCode == 0)
            {
                return stdOutput.ToString();
            }
            else
            {
                var message = new StringBuilder();

                if (!string.IsNullOrEmpty(stdError))
                {
                    message.AppendLine(stdError);
                }

                if (stdOutput.Length != 0)
                {
                    message.AppendLine("Std output:");
                    message.AppendLine(stdOutput.ToString());
                }

                throw new Exception(Format(filename, arguments) + " finished with exit code = " + process.ExitCode + ": " + message);
            }
        }
        public static void RunExternalExe(string filename, string? arguments, Action<StreamReader> onStream)
        {
            var process = new Process();

            process.StartInfo.FileName = filename;
            if (!string.IsNullOrEmpty(arguments))
            {
                process.StartInfo.Arguments = arguments;
            }

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;

            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            var sbErrors = new StringBuilder();
            process.ErrorDataReceived += (sender, args) => sbErrors.AppendLine(args.Data ?? "");
            
            try
            {
                process.Start();
                process.BeginErrorReadLine();
                Exception? onStreamEx = null;
                try
                {

                    onStream(process.StandardOutput);
                }
                catch (Exception e)
                {
                    onStreamEx = e;
                }
                if (!process.WaitForExit(10_000))
                {
                    process.Kill(true);
                    if (onStreamEx != null)
                        throw onStreamEx;
                    throw new Exception("process did not respond");
                }

                if (onStreamEx != null)
                    throw onStreamEx;

            }
            catch (Exception e)
            {
                throw new Exception("OS error while executing " + Format(filename, arguments) + ": " + e.Message, e);
            }

            if (process.ExitCode == 0) return;
            
            
            throw new Exception(Format(filename, arguments) + " finished with exit code = " + process.ExitCode + ": " + sbErrors);
        }

        private static string Format(string filename, string arguments)
        {
            return $"'{filename}{(string.IsNullOrEmpty(arguments) ? string.Empty : " " + arguments)}'";
        }
    }
}
