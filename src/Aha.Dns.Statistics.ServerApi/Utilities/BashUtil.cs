﻿using Serilog;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Aha.Dns.Statistics.ServerApi.Utilities
{
    public class BashUtil : IBashUtil
    {
        private readonly ILogger _logger;

        public BashUtil()
        {
            _logger = Log.ForContext("SourceContext", nameof(BashUtil));
        }

        /// <summary>
        /// Execute any string as a bash command
        /// and return the standard output as an enumerable of strings
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> ExecuteBash(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            _logger.Verbose("Executing command '/bin/bash -c {Command}'", escapedArgs);

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            var result = new List<string>();

            process.Start();

            while (!process.StandardOutput.EndOfStream)
                result.Add(await process.StandardOutput.ReadLineAsync());

            process.WaitForExit();

            return result;
        }
    }
}
