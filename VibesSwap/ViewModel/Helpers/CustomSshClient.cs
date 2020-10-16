using Renci.SshNet;
using Renci.SshNet.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace VibesSwap.ViewModel.Helpers
{
    /// <summary>
    /// Helper class with methods for evaluating shell results
    /// </summary>
    public static class StringExt
    {
        /// <summary>
        /// Returns the string found before the specified search pattern/regex
        /// </summary>
        /// <param name="str">The string to search</param>
        /// <param name="regex">The regex pattern to search for</param>
        /// <returns>The contents of the string prior to the specified pattern, or an empty string</returns>
        public static string StringBeforeLastRegEx(this string str, Regex regex)
        {
            var matches = regex.Matches(str);
            
            return matches.Count > 0 ? str.Substring(0, matches[matches.Count - 1].Index) : str;
        }

        /// <summary>
        /// Returns the string found after the specified search pattern/regex
        /// </summary>
        /// <param name="str">The string to search</param>
        /// <param name="regex">The regex pattern to search for</param>
        /// <returns>The contents of the string after the specified pattern, or an empty string</returns>
        public static bool EndsWithRegEx(this string str, Regex regex)
        {
            var matches = regex.Matches(str);

            return matches.Count > 0 && str.Length == (matches[matches.Count -1].Index + matches[matches.Count -1].Length);
        }

        /// <summary>
        /// Returns the remainder of string str after the specified substring, or an empty string
        /// </summary>
        /// <param name="str">The string to search</param>
        /// <param name="substring">The substring to search for</param>
        /// <returns>The contents of str after the specified substr, or an empty string</returns>
        public static string StringAfter(this string str, string substring)
        {
            var index = str.IndexOf(substring, StringComparison.Ordinal);

            return index >= 0 ? str.Substring(index + substring.Length) : "";
        }
    }

    public class CustomSshClient : IDisposable
    {
        SshClient sshClient;
        ShellStream shell;
        string lastCommand = string.Empty;
        string pwd = string.Empty;

        static readonly Regex prompt = new Regex("[a-zA-Z0-9_.-]*\\@[a-zA-Z0-9_.-]*\\:\\~[#$] ", RegexOptions.Compiled);
        static readonly Regex pwdPrompt = new Regex("password for .*\\:", RegexOptions.Compiled);
        static readonly Regex promptOrPwd = new Regex(prompt + "|" + pwdPrompt);


        /// <summary>
        /// Creates SSH session
        /// </summary>
        /// <param name="url"></param>
        /// <param name="port"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        public void Connect(string url, int port, string user, string password)
        {
            Log.Information($"Connect Ssh: {user}@{url}:{port}");

            var connectionInfo =
            new ConnectionInfo(
                url,
                port,
                user,
                new PasswordAuthenticationMethod(user, password));

            sshClient = new SshClient(connectionInfo);
            sshClient.Connect();

            var terminalMode = new Dictionary<TerminalModes, uint>
            {
                { TerminalModes.ECHO, 53 }
            };

            shell = sshClient.CreateShellStream("", 0, 0, 0, 0, 4096, terminalMode);

            try
            {
                Expect(prompt);
                Log.Information($"Connected at Ssh: {user}@{url}:{port}");
            }
            catch (Exception ex)
            {
                Log.Error("Exception - " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Destroys SSH session
        /// </summary>
        public void Disconnect()
        {
            Log.Information($"Ssh Disconnect");

            sshClient?.Disconnect();
            sshClient = null;
        }

        /// <summary>
        /// Writes to remote SSH shell
        /// </summary>
        /// <param name="commandLine"></param>
        void Write(string commandLine)
        {
            Log.Information("> " + commandLine);
            lastCommand = commandLine;

            shell.WriteLine(commandLine);
        }

        /// <summary>
        /// Awaits SSH result from STDOUT
        /// </summary>
        /// <param name="expect"></param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        string Expect(Regex expect, double timeoutSeconds = 5.0)
        {
            var result = shell.Expect(expect, TimeSpan.FromSeconds(timeoutSeconds));

            if (result == null)
            {
                throw new Exception($"Timeout {timeoutSeconds}s executing {lastCommand}");
            }

            result = result.Contains(lastCommand) ? result.StringAfter(lastCommand) : result;
            result = result.StringBeforeLastRegEx(prompt);
            result = result.Trim();

            foreach (string commandResult in result.Split( new[] { "\r\n|\r|\n" }, StringSplitOptions.None ))
            {
                Log.Information(commandResult);
            }

            return result;
        }

        /// <summary>
        /// Executes the provided SSH command
        /// </summary>
        /// <param name="commandLine"></param>
        /// <returns></returns>
        public string Execute(string commandLine)
        {
            Exception exception = null;
            var errorMessage = "failed";
            var errorCode = "exception";

            try
            {
                Write(commandLine);
                string result = Expect(promptOrPwd);

                if (result.EndsWithRegEx(pwdPrompt))
                {
                    Write(pwd);
                    Expect(prompt);
                }

                Write("echo $?");
                errorCode = Expect(prompt);

                if (errorCode == "0")
                {
                    return result;
                }
                else if (result.Length > 0)
                {
                    errorMessage = result;
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                errorMessage = ex.Message;
            }

            throw new Exception($"Ssh error: {errorMessage}, code: {errorCode}, command: {commandLine}", exception);
        }

        public void Dispose()
        {
            if (sshClient.IsConnected)
            {
                sshClient.Disconnect();
                sshClient = null;
            }
        }
    }
}
