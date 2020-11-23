using Renci.SshNet;
using Renci.SshNet.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using VibesSwap.Model;
using VibesSwap.Model.Dimensional;

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
        private string SshPassword { get; set; }

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
        /// Will send back OnCmCommandComplete to all subscribed VM's
        /// </summary>
        /// <param name="host">The VibesHost to connect to</param>
        /// <param name="cm">The VibesCm to start</param>
        /// <param name="hashCode">The hash of the sending VM</param>
        public static void StartCm(VibesHost host, VibesCm cm, int hashCode)
        {
            ValidateParameters(host, cm);
            string sshCommand = SshCommands.StartRemoteCmCommand(host, cm);
            string sshResult = ExecuteSshCommand(host, sshCommand);

            OnCmCommandComplete(cm, sshResult.Contains("running") ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable, hashCode: hashCode);
        }

        /// <summary>
        /// USes SSH commands to start multiple remote CM's
        /// Will send back OnCmCommandCOmplete to all subscribed VM's for each applicable CM
        /// </summary>
        /// <param name="host">The VibesHost to connect to</param>
        /// <param name="cms">List of VibesCm's to start</param>
        /// <param name="hashCode">The hash of the sending VM</param>
        public static void StartCmMultiple(VibesHost host, List<VibesCm> cms, int hashCode)
        {
            ValidateParameters(host);
            List<(VibesCm, string command)> commands = new List<(VibesCm, string)>();
            foreach (VibesCm cm in cms)
            {
                string sshCommand = SshCommands.StartRemoteCmCommand(host, cm);
                commands.Add((cm, sshCommand));
            }
            var results = Task.Run(() => ExecuteMultipleSshCommand(host, commands));
            foreach (var result in results.Result)
            {
                OnCmCommandComplete(result.cm, result.result.Contains("running") ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable, hashCode: hashCode);
            }
        }

        /// <summary>
        /// Uses SSH command to stop remote CM
        /// Will send back OnCmCommandComplete to all subscribed VM's
        /// </summary>
        /// <param name="host">The VibesHost to connect to</param>
        /// <param name="cm">The VibesCm to stop</param>
        /// <param name="hashCode">The hash of the sending VM</param>    
        public static void StopCm(VibesHost host, VibesCm cm, int hashCode)
        {
            ValidateParameters(host, cm);
            string sshCommand = SshCommands.StopRemoteCmCommand(host, cm);
            string sshResult = ExecuteSshCommand(host, sshCommand);

            OnCmCommandComplete(cm, sshResult.Contains("running") ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable, hashCode: hashCode);
        }

        /// <summary>
        /// Uses SSH commands to stop multiple remote CM's
        /// Will send back OnCmCommandCOmplete to all subscribed VM's for each applicable CM
        /// </summary>
        /// <param name="host">The VibesHost to connect to</param>
        /// <param name="cms">List of VibesCm's to stop</param>
        /// <param name="hashCode">The hash of the sending VM</param>
        public static void StopCmMultiple(VibesHost host, List<VibesCm> cms, int hashCode)
        {
            ValidateParameters(host);
            List<(VibesCm, string command)> commands = new List<(VibesCm, string)>();
            foreach (VibesCm cm in cms)
            {
                string sshCommand = SshCommands.StopRemoteCmCommand(host, cm);
                commands.Add((cm, sshCommand));
            }
            var results = Task.Run(() => ExecuteMultipleSshCommand(host, commands));
            foreach (var result in results.Result)
            {
                OnCmCommandComplete(result.cm, result.result.Contains("running") ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable, hashCode: hashCode);
            }
        }

        /// <summary>
        /// Uses bash sed command to edit deployment properties file
        /// Will send back OnCmCommandCOmplete to all subscribed VM's
        /// </summary>
        /// <param name="host">The VibesHost to conenct to</param>
        /// <param name="cm">The VibesCm to edit</param>
        /// <param name="paramToEdit">The text pattern to find</param>
        /// <param name="paramToReplace">The text pattern to replace with</param>
        /// <param name="hashCode">The hash of the sending VM</param>
        public static void AlterCm(VibesHost host, VibesCm cm, string paramToEdit, string paramToReplace, int hashCode)
        {
            ValidateParameters(host, cm);
            if (string.IsNullOrEmpty(paramToEdit) || string.IsNullOrEmpty(paramToReplace))
            {
                throw new ArgumentNullException($"Attempted to alter CM {cm.CmResourceName} without parameters to add/remove");
            }

            string sshCommand = SshCommands.AlterRemoteCmCommand(host, cm, paramToEdit, paramToReplace);
            ExecuteSshCommand(host, sshCommand);

            OnCmCommandComplete(cm, HttpStatusCode.NoContent, hashCode: hashCode);
        }

        /// <summary>
        /// Uses bash sed command to edit multiple deployment properties files
        /// Will send back OnCmCommandCOmplete to all subscribed VM's for each applicable CM
        /// </summary>
        /// <param name="host">The VibesHost to conenct to</param>
        /// <param name="cm">The VibesCm to edit</param>
        /// <param name="paramToEdit">The text pattern to find</param>
        /// <param name="paramToReplace">The text pattern to replace with</param>
        /// <param name="hashCode">The hash of the sending VM</param>
        public static void AlterCmMultiple(VibesHost host, List<VibesCm> cms, int hashCode)
        {
            ValidateParameters(host);
            List<(VibesCm, string command)> commands = new List<(VibesCm, string)>();
            foreach (VibesCm cm in cms)
            {
                foreach (DeploymentProperty propertyToChange in cm.DeploymentProperties)
                {
                    if (string.IsNullOrEmpty(propertyToChange.SearchPattern) || string.IsNullOrEmpty(propertyToChange.ReplacePattern))
                    {
                        continue;
                    }
                    string sshCommand = SshCommands.AlterRemoteCmCommand(host, cm, propertyToChange.SearchPattern, propertyToChange.ReplacePattern);
                    commands.Add((cm, sshCommand));
                }
            }
            var results = Task.Run(() => ExecuteMultipleSshCommand(host, commands));
            foreach (var result in results.Result)
            {
                OnCmCommandComplete(result.cm, HttpStatusCode.NoContent, hashCode: hashCode);
            }
        }

        /// <summary>
        /// Gets deployment.properties from specified CM
        /// </summary>
        /// <param name="host">The host on which the CM is installed</param>
        /// <param name="cm">The CM to query</param>
        /// <param name="hashCode">The hash of the sending VM</param>
        public static void GetCmParams(VibesHost host, VibesCm cm, int hashCode)
        {
            ValidateParameters(host, cm);
            string sshCommand = SshCommands.EchoCmProperties(cm);
            Task<string> sshResult = Task.Run(() => ExecuteSshCommand(host, sshCommand));

            OnCmCommandComplete(host, cm, sshResult.Result, hashCode: hashCode);
        }

        /// <summary>
        /// Gets deployment.properties for a list of supplied CM's
        /// </summary>
        /// <param name="host">The host on which the CM is installed</param>
        /// <param name="cms">The CM to query</param>
        /// <param name="hashCode">The has of the sending VM</param>
        public static void GetAllCmParams(VibesHost host, List<VibesCm> cms, int hashCode)
        {
            ValidateParameters(host);
            List<(VibesCm, string command)> commands = new List<(VibesCm, string)>();
            foreach (VibesCm cm in cms)
            {
                string sshCommand = SshCommands.EchoCmProperties(cm);
                commands.Add((cm, sshCommand));
            }
            var results = Task.Run(() => ExecuteMultipleSshCommand(host, commands));
            foreach (var result in results.Result)
            {
                OnCmCommandComplete(result.host, result.cm, result.result, hashCode: hashCode);
            }
        }

        /// <summary>
        /// Get hosts file from specified host
        /// </summary>
        /// <param name="host">The host to query</param>
        /// <param name="hashCode">The hash of the sending VM</param>
        public static void GetHostsFile(VibesHost host, int hashCode)
        {
            ValidateParameters(host);
            string sshCommand = SshCommands.EchoRemoteHostsFile();
            Task<string> sshResult = Task.Run(() => ExecuteSshCommand(host, sshCommand));

            OnCmCommandComplete(host, sshResult.Result, hashCode: hashCode);
        }

        /// <summary>
        /// Switch hosts file on the specified host
        /// </summary>
        /// <param name="host">The host to swittch hosts file on</param>
        /// <param name="indMoveToProd">True if moving from hlcint to prod, false for opposite</param>
        public static void SwitchHostsFile(VibesHost host, bool indMoveToProd, int hashCode)
        {
            ValidateParameters(host, null);
            string sshCommand = SshCommands.SwitchRemoteHostsFile(host, indMoveToProd);
            Task.Run(() =>
            {
                ExecuteSshCommand(host, sshCommand);
                // Get hostsfile after switch
                GetHostsFile(host, hashCode);
            });
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
            CmCommandComplete?.Invoke(null, new CmHelperEventArgs { Host = targetHost, CmChanged = targetCm, CmStatus = HttpStatusCode.Continue, DeploymentProperties = deploymentProperties, SubscriberHashCode = hashCode });
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
                    prompt.Response = instance.SshPassword;
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
        private static string ExecuteSshCommand(VibesHost host, string sshCommand)
        {
            string result = string.Empty;

            // Authentication
            instance.SshPassword = host.SshPassword;
            KeyboardInteractiveAuthenticationMethod keybAuth = new KeyboardInteractiveAuthenticationMethod(host.SshUsername);
            keybAuth.AuthenticationPrompt += new EventHandler<AuthenticationPromptEventArgs>(HandleKeyEvent);

            // Connection Info
            ConnectionInfo connInfo = new ConnectionInfo(host.Url, 22, host.SshUsername, keybAuth);

            try
            {
                using (SshClient client = new SshClient(connInfo))
                {
                    client.Connect();

                    if (client.IsConnected)
                    {
                        SshCommand command = client.CreateCommand(sshCommand);
                        Log.Debug($"Sending single SSH command {command.CommandText} to {host.Name}");
                        result = command.Execute();
                        Log.Debug($"Received single SSH result {result}");
                    }
                    else
                    {
                        Log.Error($"Error sending SSH command, client not connected");
                    }

                    client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error sending SSH command, error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Sends a list of commands to the specified host
        /// </summary>
        /// <param name="host"></param>
        /// <param name="commands"></param>
        /// <returns></returns>
        private static List<(VibesHost host, VibesCm cm, string result)> ExecuteMultipleSshCommand(VibesHost host, List<(VibesCm cm, string command)> commands)
        {
            List<(VibesHost host, VibesCm cm, string result)> results = new List<(VibesHost host, VibesCm cm, string result)>();

            // Authentication
            instance.SshPassword = host.SshPassword;
            KeyboardInteractiveAuthenticationMethod keybAuth = new KeyboardInteractiveAuthenticationMethod(host.SshUsername);
            keybAuth.AuthenticationPrompt += new EventHandler<AuthenticationPromptEventArgs>(HandleKeyEvent);

            // Connection Info
            ConnectionInfo connInfo = new ConnectionInfo(host.Url, 22, host.SshUsername, keybAuth);

            try
            {
                using (SshClient client = new SshClient(connInfo))
                {
                    client.Connect();

                    if (client.IsConnected)
                    {
                        foreach (var sshCommand in commands)
                        {
                            SshCommand command = client.CreateCommand(sshCommand.command);
                            Log.Debug($"Sending multiple SSH command {command.CommandText} to {host.Name}");
                            string result = command.Execute();

                            results.Add((host, sshCommand.cm, result));
                            Log.Debug($"Received multiple SSH result {result}");
                        }
                    }

                    client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error sending SSH multiple commands, error: {ex.Message}");
            }
            return results;
        }

        #endregion

        #endregion
    }
}
