using VibesSwap.Model;

namespace VibesSwap.ViewModel.Helpers
{
    internal static class SshCommands
    {
        /// <summary>
        /// Command to start a remote CM
        /// </summary>
        /// <param name="host">The host the CM sits on, for password and/or clustering</param>
        /// <param name="cm">The CM to start</param>
        /// <returns>SSH command for this host/CM</returns>
        internal static string StartRemoteCmCommand(VibesHost host, VibesCm cm)
        {
            return host.IndClustered ? $"echo '{host.SshPassword}' | sudo -S /usr/sbin/crm resource start {cm.CmResourceName}" : $"echo '{host.SshPassword}' | sudo -Su vibes sh {cm.CmCorePath}/bin/cm_start -i {cm.CmResourceName}";
        }

        /// <summary>
        /// Command to stop a remote CM
        /// </summary>
        /// <param name="host">The host the CM sits on, for password and/or clustering</param>
        /// <param name="cm">The CM to stop</param>
        /// <returns>SSH command for this host/CM</returns>
        internal static string StopRemoteCmCommand(VibesHost host, VibesCm cm)
        {
            return host.IndClustered ? $"echo '{host.SshPassword}' | sudo -S /usr/sbin/crm resource stop {cm.CmResourceName}" : $"echo '{host.SshPassword}' | sudo -Su vibes sh {cm.CmCorePath}/bin/cm_stop -i {cm.CmResourceName}";
        }

        /// <summary>
        /// Command to alter a remote CM
        /// </summary>
        /// <param name="host">The host the CM sits on, for password and/or clustering</param>
        /// <param name="cm">The CM to alter</param>
        /// <param name="paramToEdit">The sed search pattern</param>
        /// <param name="paramToReplace">The sed replace pattern</param>
        /// <returns>SSH command for this host/CM</returns>
        internal static string AlterRemoteCmCommand(VibesHost host, VibesCm cm, string paramToEdit, string paramToReplace)
        {
            return $"echo '{host.SshPassword}' | sudo -S sed -i 's${paramToEdit}${paramToReplace}$' {cm.CmPath}/conf/deployment.properties";
        }

        /// <summary>
        /// Command to fetch properties from a remote CM
        /// </summary>
        /// <param name="cm">The CM to start</param>
        /// <returns>SSH command for this host/CM</returns>
        internal static string EchoCmProperties(VibesCm cm)
        {
            return $"cat {cm.CmPath}/conf/deployment.properties";
        }

        /// <summary>
        /// Command to echo remote hosts file
        /// </summary>
        /// <returns>SSH command to echo remote hosts file</returns>
        internal static string EchoRemoteHostsFile()
        {
            return $"cat /etc/hosts";
        }

        /// <summary>
        /// Command to swtich remote hosts file
        /// </summary>
        /// <param name="host">The host to target, for password</param>
        /// <param name="indMoveToProd">True if moving to yvr.com DNS, otherwise false</param>
        /// <returns>SSH command to switch remote hosts file</returns>
        internal static string SwitchRemoteHostsFile(VibesHost host, bool indMoveToProd)
        {
            return indMoveToProd ? $"echo '{ host.SshPassword }' | sudo -S cp /etc/hosts.prod /etc/hosts" : $"echo '{ host.SshPassword }' | sudo -S cp /etc/hosts.hlcint /etc/hosts";
        }
    }
}
