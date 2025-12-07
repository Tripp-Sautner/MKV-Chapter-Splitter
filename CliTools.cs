using System.Diagnostics;

namespace Chapter_Splitter
{
    internal static class CliTools
    {
        public static string RunAndReturnOutput(string command, string arguments, bool ignoreOutput = false)
        {
            Process process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;       // Must be false to redirect streams
            process.StartInfo.RedirectStandardOutput = true; // Redirect standard output
            process.StartInfo.RedirectStandardError = true;  // Optionally redirect standard error
            process.StartInfo.CreateNoWindow = true;         // Do not create a new window for the process

            try
            {
                process.Start();
                if (ignoreOutput)
                    return "";

                // Read both streams asynchronously to avoid deadlocks
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                // Wait for process exit and the async reads to complete
                process.WaitForExit();
                Task.WaitAll(outputTask, errorTask);

                string output = outputTask.Result;
                string error = errorTask.Result;

                return output;
            }
            catch (Exception E)
            {
                return $"Error: {E.Message}";
            }
        }
    }
}
