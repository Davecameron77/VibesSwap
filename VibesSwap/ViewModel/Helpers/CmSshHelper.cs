﻿using Renci.SshNet;
using Renci.SshNet.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VibesSwap.Model;

namespace VibesSwap.ViewModel.Helpers
{
    /// <summary>
    /// Helper class for SSH commands relating to CM status
    /// Singleton, not for instantiation
    /// </summary>
    public sealed class CmSshHelper
    {
        #region Constructors

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static CmSshHelper()
        {
        }

        private CmSshHelper()
        {
        }

        #endregion

        #region Members

        /// <summary>
        /// Internal storage for SSH password
        /// </summary>
        private string sshPassword { get; set; }

        /// <summary>
        /// Singleton instance
        /// </summary>
        private static readonly CmSshHelper instance = new CmSshHelper();

        /// <summary>
        /// Public facing singleton instance
        /// </summary>
        public static CmSshHelper Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// Delegate for PollComplete event
        /// </summary>
        /// <param name="sender">Null, not used from singleton class</param>
        /// <param name="e">CmHelperEventArgs to be passed to subscriber</param>
        public delegate void CmCommandCompleteEventHandler(object sender, CmHelperEventArgs e);
        /// <summary>
        /// Event to be fired when CheckCmStatus is completed
        /// </summary>
        public static event CmCommandCompleteEventHandler CmCommandComplete = delegate { };

        #endregion

        #region Methods

        /// <summary>
        /// Uses SSH command to start remote CM
        /// </summary>
        /// <param name="host">The VibesHost to connect to</param>
        /// <param name="cm">The VibesCm to start</param>
        /// /// <param name="hashCode">The hashcode of the sender to append to OnCommandComplete</param>
        /// <returns>Task<bool>, True if CM is running, otherwise false, though this is backup functionality, expected use is the event CmCommandComplete</returns>
        public static async Task<bool> StartCm(VibesHost host, VibesCm cm, int hashCode)
        {
            ValidateParameters(host, cm);
            string sshCommand = host.IndClustered ? $"echo '{host.SshPassword}\n' | sudo -S /usr/sbin/crm resource start {cm.CmResourceName}" : $"echo '{host.SshPassword}\n' | sudo -Su vibes sh {cm.CmCorePath}/bin/cm_start -i {cm.CmResourceName}";
            string sshResult = await ExecuteSshCommand(host, sshCommand);
                
            OnCmCommandComplete(cm, sshResult.Contains("running") ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable, hashCode: hashCode);
            return sshResult.Contains("running");
        }

        /// <summary>
        /// Uses SSH commands to start a collection of remote CM's
        /// </summary>
        /// <param name="host">The VibesHost to connect to</param>
        /// <param name="cm">The list of VibesCms to start</param>
        /// /// <param name="hashCode">The hashcode of the sender to append to OnCommandComplete</param>
        /// <returns>Task<bool>, True if CM is the command executes succesfully, otherwise false, though this is backup functionality, expected use is the event CmCommandComplete</returns>
        public static async Task<bool> StartCmMultiple(VibesHost host, List<VibesCm> cms, int hashCode)
        {
            List<(VibesCm, string)> commandsToSend = new List<(VibesCm, string)>();

            foreach (VibesCm cm in cms)
            {
                ValidateParameters(host, cm);
                string sshCommand = host.IndClustered 
                    ? $"echo '{host.SshPassword}\n' | sudo -S /usr/sbin/crm resource start {cm.CmResourceName}\n" 
                    : $"echo '{host.SshPassword}\n' | sudo -Su vibes sh {cm.CmCorePath}/bin/cm_start -i {cm.CmResourceName}\n";
                commandsToSend.Add((cm, sshCommand));
            }

            var sshResults = await ExecuteMultipleSshCommand(host, commandsToSend);
            foreach (var (cm, commandResult) in sshResults)
            {
                OnCmCommandComplete(cm, commandResult.Contains("running") ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable, hashCode: hashCode);
            }

            return true;
        }

        /// <summary>
        /// Uses SSH command to stop remote CM
        /// </summary>
        /// <param name="host">The VibesHost to connect to</param>
        /// <param name="cm">The VibesCm to stop</param>
        /// <param name="hashCode">The hascode of the sender</param>
        /// <returns>Task<bool>, True if CM is running, otherwise false, though this is backup functionality, expected use is the event CmCommandComplete</returns>        
        public static async Task<bool> StopCm(VibesHost host, VibesCm cm, int hashCode)
        {
            ValidateParameters(host, cm);
            string sshCommand = host.IndClustered ? $"echo '{host.SshPassword}\n' | sudo -S /usr/sbin/crm resource stop {cm.CmResourceName}" : $"echo '{host.SshPassword}\n' | sudo -Su vibes sh {cm.CmCorePath}/bin/cm_stop -i {cm.CmResourceName}";
            string sshResult = await ExecuteSshCommand(host, sshCommand);

            OnCmCommandComplete(cm, sshResult.Contains("running") ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable, hashCode: hashCode);
            return sshResult.Contains("running");
        }

        /// <summary>
        /// Uses SSH commands to stop a collection of remote CM's
        /// </summary>
        /// <param name="host">The VibesHost to connect to</param>
        /// <param name="cm">The list of VibesCms to stop</param>
        /// /// <param name="hashCode">The hashcode of the sender to append to OnCommandComplete</param>
        /// <returns>Task<bool>, True if CM is the command executes succesfully, otherwise false, though this is backup functionality, expected use is the event CmCommandComplete</returns>
        public static async Task<bool> StopCmMultiple(VibesHost host, List<VibesCm> cms, int hashCode)
        {
            List<(VibesCm, string)> commandsToSend = new List<(VibesCm, string)>();

            foreach (VibesCm cm in cms)
            {
                ValidateParameters(host, cm);
                string sshCommand = host.IndClustered 
                    ? $"echo '{host.SshPassword}\n' | sudo -S /usr/sbin/crm resource stop {cm.CmResourceName}\n" 
                    : $"echo '{host.SshPassword}\n' | sudo -Su vibes sh {cm.CmCorePath}/bin/cm_stop -i {cm.CmResourceName}\n";
                commandsToSend.Add((cm, sshCommand));
            }

            var sshResults = await ExecuteMultipleSshCommand(host, commandsToSend);
            foreach (var (cm, commandResult) in sshResults)
            {
                OnCmCommandComplete(cm, commandResult.Contains("running") ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable, hashCode: hashCode);
            }

            return true;
        }

        /// <summary>
        /// Uses bash sed command to edit deployment properties file
        /// </summary>
        /// <param name="host">The VibesHost to conenct to</param>
        /// <param name="cm">The VibesCm to start</param>
        /// <param name="paramToEdit">The text pattern to find</param>
        /// <param name="paramToReplace">The text pattern to replace with</param>
        public static async void AlterCm(VibesHost host, VibesCm cm, string paramToEdit, string paramToReplace, int hashCode)
        {
            ValidateParameters(host, cm);
            if (string.IsNullOrEmpty(paramToEdit) || string.IsNullOrEmpty(paramToReplace))
            {
                throw new ArgumentNullException($"Attempted to alter CM {cm.CmResourceName} without parameters to add/remove");
            }

            string sshCommand = $"echo '{host.SshPassword}\n' | sudo -S sed -i 's${paramToEdit}${paramToReplace}$' {cm.CmPath}/conf/deployment.properties";
            _ = await ExecuteSshCommand(host, sshCommand);

            OnCmCommandComplete(cm, HttpStatusCode.NoContent, hashCode: hashCode);
        }

        /// <summary>
        /// Uses SSH commands to alter a collection of remote CM's
        /// </summary>
        /// <param name="host">The VibesHost to connect to</param>
        /// <param name="commands">Tuple of (VibesCm, SearchTerm, ReplaceTerm) for which to build a sed command</param>
        /// <param name="hashCode">The hashcode of the sender to append to OnCommandComplete</param>
        /// <returns>Task<bool>, True if CM is the command executes succesfully, otherwise false, though this is backup functionality, expected use is the event CmCommandComplete</returns>
        public static async Task<bool> AlterCmMultiple(VibesHost host, List<(VibesCm cm, string paramToSearch, string paramToReplace)> commands, int hashCode)
        {
            List<(VibesCm, string)> commandsToSend = new List<(VibesCm cmToEdit, string sshCommand)>();

            foreach (var command in commands)
            {
                ValidateParameters(host, command.cm);
                string sshCommand = $"echo {host.SshPassword}\n | sudo -S sed -i s${command.paramToSearch}${command.paramToReplace}$ {command.cm.CmPath}/conf/deployment.properties";
                commandsToSend.Add((command.cm, sshCommand));
            }

            var sshResults = await ExecuteMultipleSshCommand(host, commandsToSend);
            foreach (var (cm, commandResult) in sshResults)
            {
                OnCmCommandComplete(cm, commandResult.Contains("running") ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable, hashCode: hashCode);
            }

            return true;
        }

        /// <summary>
        /// Gets deployment.properties from specified CM
        /// </summary>
        /// <param name="host">The host on which the CM is installed</param>
        /// <param name="cm">The CM to query</param>
        /// <returns>string containing contents of CM deployment.properties</returns>
        public static async Task<string> GetCmParams(VibesHost host, VibesCm cm, int hashCode)
        {
            ValidateParameters(host, cm);
            string sshCommand = $"cat {cm.CmPath}/conf/deployment.properties";
            string sshResult = await ExecuteSshCommand(host, sshCommand);

            OnCmCommandComplete(host, cm, sshResult, hashCode: hashCode);
            return sshResult;
        }

        /// <summary>
        /// Get hosts file from specified host
        /// </summary>
        /// <param name="host">The host to query</param>
        /// <returns>Output of cat /etc/hosts</returns>
        public static async Task<string> GetHostsFile(VibesHost host, int hashCode)
        {
            ValidateParameters(host);
            string sshCommand = $"cat /etc/hosts";
            string sshResult = await ExecuteSshCommand(host, sshCommand);

            OnCmCommandComplete(host, sshResult, hashCode: hashCode);
            return sshResult;
        }

        /// <summary>
        /// Switch hosts file on the specified host
        /// </summary>
        /// <param name="host">The host to swittch hosts file on</param>
        /// <param name="indMoveToProd">True if moving from hlcint to prod, false for opposite</param>
        public static async void SwitchHostsFile(VibesHost host, bool indMoveToProd, int hashCode)
        {
            ValidateParameters(host, null);
            string sshCommand = indMoveToProd ? $"echo '{ host.SshPassword }\n' | sudo -S cp /etc/hosts.prod /etc/hosts" : $"echo '{ host.SshPassword }\n' | sudo -S cp /etc/hosts.hlcint /etc/hosts";
            _ = await ExecuteSshCommand(host, sshCommand);
            // Get hostsfile after switch
            _ = await GetHostsFile(host, hashCode);
        }

        #region Event Methods

        /// <summary>
        /// Raises CmCommandComplete event for command resulting from an SSH command
        /// </summary>
        /// <param name="cmChanged">The CM which was changed</param>
        /// <param name="cmStatus">The status of the CM changed</param>
        /// /// <param name="hashCode">The hashcode of the sender</param>
        private static void OnCmCommandComplete(VibesCm cmChanged, HttpStatusCode cmStatus, int hashCode)
        {
            CmCommandComplete?.Invoke(null, new CmHelperEventArgs { CmChanged = cmChanged, CmStatus = cmStatus, SubscriberHashCode = hashCode });
        }

        /// <summary>
        /// Raises CmCommandComplete event for command resulting from an SSH command which fetches deployment.properties
        /// </summary>
        /// <param name="cmChanged">The CM which was changed</param>
        /// <param name="deploymentProperties">The deployment.properties fetched</param>
        /// /// <param name="hashCode">The hashcode of the sender</param>
        private static void OnCmCommandComplete(VibesHost targetHost, VibesCm targetCm, string deploymentProperties, int hashCode)
        {
            CmCommandComplete?.Invoke(null, new CmHelperEventArgs { Host = targetHost, CmChanged = targetCm, DeploymentProperties = deploymentProperties, SubscriberHashCode = hashCode });
        }

        /// <summary>
        /// Raises CmCommandComplete event for command resulting from an SSH command which fetches a hosts file
        /// </summary>
        /// <param name="hostFetched">The host that was queried</param>
        /// <param name="hostsFile">The hosts file fetched</param>
        /// /// <param name="hashCode">The hashcode of the sender</param>
        private static void OnCmCommandComplete(VibesHost hostFetched, string hostsFile, int hashCode)
        {
            CmCommandComplete?.Invoke(null, new CmHelperEventArgs { Host = hostFetched, HostsFile = hostsFile, SubscriberHashCode = hashCode });
        }

        #endregion

        #region helpers

        /// <summary>
        /// Provides Password to SSH client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void HandleKeyEvent(object sender, AuthenticationPromptEventArgs e)
        {
            foreach (AuthenticationPrompt prompt in e.Prompts)
            {
                if (prompt.Request.IndexOf("Password:", StringComparison.InvariantCultureIgnoreCase) != -1)
                {
                    prompt.Response = instance.sshPassword;
                }
            }
        }

        /// <summary>
        /// Validates host/cm parameters to ensure command success
        /// </summary>
        /// <param name="host">The host for which parameters need validating</param>
        /// <param name="cm">Optional, the CM for which parameters need validating</param>
        private static void ValidateParameters(VibesHost host, VibesCm cm = null)
        {
            if (string.IsNullOrEmpty(host.Url))
            {
                throw new ArgumentNullException("Attempted to command CM without URL");
            }
            if (string.IsNullOrEmpty(host.SshUsername) || string.IsNullOrEmpty(host.SshPassword))
            {
                throw new ArgumentNullException("Attempted to command CM without SSH credentials");
            }
            if (cm != null && (string.IsNullOrEmpty(cm.CmCorePath) || string.IsNullOrEmpty(cm.CmPath) || string.IsNullOrEmpty(cm.CmResourceName)))
            {
                throw new ArgumentNullException("Attempted to command CM without CM metadata");
            }
        }

        /// <summary>
        /// Executes supplied SSH command on the supplied host
        /// </summary>
        /// <param name="host">The host to send the command</param>
        /// <param name="sshCommand">The command to execute</param>
        /// <returns>Any console outtput resulting from the command</returns>
        private async static Task<string> ExecuteSshCommand(VibesHost host, string sshCommand)
        {
            string result = string.Empty;

            // Authentication
            instance.sshPassword = host.SshPassword;
            KeyboardInteractiveAuthenticationMethod keybAuth = new KeyboardInteractiveAuthenticationMethod(host.SshUsername);
            keybAuth.AuthenticationPrompt += new EventHandler<AuthenticationPromptEventArgs>(HandleKeyEvent);

            // Connection Info
            ConnectionInfo connInfo = new ConnectionInfo(host.Url, 22, host.SshUsername, keybAuth);

            using (SshClient client = new SshClient(connInfo))
            {
                client.Connect();
                Log.Debug($"SSH client connected to {host.Url} for single SSH command");

                var sshResult = await Task.Run(() => client.RunCommand(sshCommand));
                result = sshResult.Result;
                Log.Debug($"SSH command {sshCommand} resulted in {result}");

                client.Disconnect();
                Log.Debug($"SSH client disconnected");

                return result;
            }
        }

        /// <summary>
        /// Sends a list of commands to the specified host
        /// </summary>
        /// <param name="host"></param>
        /// <param name="commands"></param>
        /// <returns></returns>
        private async static Task<List<(VibesCm cm, string commandResult)>> ExecuteMultipleSshCommand(VibesHost host, List<(VibesCm cm, string sshCommand)> commands)
        {
            List<(VibesCm cmChanged, string commandResult)> results = new List<(VibesCm cmChanged, string commandResult)>();

            // Authentication
            instance.sshPassword = host.SshPassword;
            KeyboardInteractiveAuthenticationMethod keybAuth = new KeyboardInteractiveAuthenticationMethod(host.SshUsername);
            keybAuth.AuthenticationPrompt += new EventHandler<AuthenticationPromptEventArgs>(HandleKeyEvent);

            // Connection Info
            ConnectionInfo connInfo = new ConnectionInfo(host.Url, 22, host.SshUsername, keybAuth);

            try
            {
                using (SshClient client = new SshClient(connInfo))
                {
                    client.Connect();
                    ShellStream sshStream = client.CreateShellStream("", 80, 40, 80, 40, 1024);
                    if (sshStream.CanWrite)
                    {
                        Log.Debug($"SSH client connected to {host.Url} for multiple SSH commands");
                        await Task.Run(() =>
                        {
                            //TODO - Maybe this is removed, trying to flush stream
                            sshStream.Flush();
                            while (sshStream.Length > 0)
                            {
                                _ = sshStream.Read();
                            }

                            foreach (var (cm, sshCommand) in commands)
                            {
                                Log.Debug($"Sending command {sshCommand}");
                                sshStream.WriteLine(sshCommand);
                                sshStream.Flush();
                                StringBuilder resultBuilder = new StringBuilder();
                                while (sshStream.Length > 0)
                                {
                                    resultBuilder.Append(sshStream.ReadLine());
                                }
                                Log.Debug($"Received result {resultBuilder}");
                                string sshResult = resultBuilder.ToString();
                                results.Add((cm, sshResult));

                                Log.Debug($"SSH command {sshCommand} resulted in {sshResult}");
                                sshStream.Flush();
                            }
                        });
                    }
                    else
                    {
                        Log.Error($"SSH connect failed");
                    }
                    sshStream.Close();
                    Log.Debug($"SSH client disconnected from shell session");
                    client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error establishing SSH shell, error: {ex.Message}");
            }
            

            return results;
        }

        #endregion

        #endregion
    }
}
