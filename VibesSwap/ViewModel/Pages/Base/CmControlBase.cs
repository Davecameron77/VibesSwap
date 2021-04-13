using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using VibesSwap.Model;
using VibesSwap.Model.Dimensional;
using VibesSwap.View.Dialogs;
using VibesSwap.ViewModel.Helpers;

namespace VibesSwap.ViewModel.Pages.Base
{
    /// <summary>
    /// Viewmodel base class for all VM's which control CM's
    /// Contains common properties/methods used by all such VM's
    /// </summary>
    internal abstract partial class CmControlBase : VmBase
    {
        #region Called on setup

        /// <summary>
        /// Loads stored CM's into provided collection
        /// Called by LoadData() during GUI setup
        /// </summary>
        /// <param name="cmCollection">The collection to load</param>
        /// <param name="hostType">The type of host from which to fetch CM's</param>
        internal void LoadCmForSwap(ICollection<VibesCm> cmCollection, HostTypes hostType, int hostId = 0)
        {
            // CmsDisplay<Host>, clear before populating
            cmCollection.Clear();
            using (DataContext context = new DataContext())
            {
                if (context.EnvironmentHosts.Any(h => h.HostType == hostType))
                {
                    // HostId sent but not found, return
                    if (hostId != 0 && !context.EnvironmentHosts.Any(h => h.Id == hostId)) return;
                    // Find any configured host(s) in database where the hostid is the same
                    var hosts = hostId != 0 ? context.EnvironmentHosts.Where(h => h.Id == hostId).OrderBy(h => h.Name) : context.EnvironmentHosts.Where(h => h.HostType == hostType).OrderBy(h => h.Name);

                    // Load all CM's for this host
                    foreach (VibesHost host in hosts)
                    {
                        foreach (VibesCm cm in context.HostCms.Where(c => c.VibesHostId == host.Id).Include(c => c.DeploymentProperties).Include(c => c.VibesHost).OrderBy(c => c.CmResourceName))
                        {
                            var newCm = cm.DeepCopy();
                            newCm.PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);

                            foreach (DeploymentProperty prop in newCm.DeploymentProperties)
                            {
                                prop.PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                            }
                            cmCollection.Add(newCm);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Polls CM asyncronously from code
        /// An event is expected back on poll complete, which requires subscribing to CmHttpHelper.PollComplete, in order to update the GUI
        /// </summary>
        /// <param name="cmToPoll">Reference to CM to be polled</param>
        internal void PollCmAsync(VibesCm cmToPoll)
        {
            using (DataContext context = new DataContext())
            {
                VibesHost hostToPoll = context.EnvironmentHosts.Single(h => h.Id == cmToPoll.VibesHostId);
                Task.Run(() => CmHttpHelper.CheckCmStatus(hostToPoll, cmToPoll, GetHashCode()));
                cmToPoll.CmStatus = CmStates.Polling;
            }
        }

        #endregion

        #region Called on event

        /// <summary>
        /// Stores deployment properties if returned by SSH command
        /// Used to populate VibesCmSwapView and EcSwapView
        /// </summary>
        /// <param name="cmChanged">The CM which was queried</param>
        /// <param name="deploymentProperties">The deployment properties file fetched</param>
        internal void StoreDeploymentProperties(VibesCm cmChanged, string deploymentProperties)
        {
            using (DataContext context = new DataContext())
            {
                VibesCm cmToUpdate = context.HostCms.SingleOrDefault(c => c.Id == cmChanged.Id);
                XDocument xProperties = XDocument.Parse(deploymentProperties);

                // Loop through deployment properties and only fetch desired properties
                foreach (XElement node in xProperties.Descendants("parameter"))
                {
                    // Property types to search for
                    if (
                        node.Value.ToLower().Contains("http:") ||
                        node.FirstAttribute.Value.Contains("URL_TO") ||
                        node.FirstAttribute.Value == "useVibes" ||
                        node.FirstAttribute.Value.Contains("smart_suite_server_name") ||
                        node.FirstAttribute.Value.Contains("smart_suite_username") ||
                        node.FirstAttribute.Value.Contains("smart_suite_password")
                        )
                    {
                        string propertyKey = node.Attribute("name").Value;
                        string propertyVal = node.Value;

                        // Property Exists, update
                        if (context.DeploymentProperties.Any(p => p.CmId == cmChanged.Id && p.PropertyKey == propertyKey))
                        {
                            DeploymentProperty propToUpdate = context.DeploymentProperties.SingleOrDefault(p => p.CmId == cmChanged.Id && p.PropertyKey == propertyKey);
                            propToUpdate.PropertyValue = propertyVal;
                            context.Update(propToUpdate);
                            continue;
                        }
                        else
                        {
                            // Property does not exist, add
                            context.DeploymentProperties.Add(new DeploymentProperty
                            {
                                PropertyKey = propertyKey,
                                PropertyValue = propertyVal,
                                Cm = context.HostCms.Single(c => c.Id == cmChanged.Id),
                                CmId = context.HostCms.Single(c => c.Id == cmChanged.Id).Id
                            });
                        }
                    }
                }
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Raises popup for hosts file if attached
        /// Called by OnCmCommandComplete
        /// </summary>
        /// <param name="args">CmHelperEvenArgs</param>
        internal void PopupHostsFile(CmHelperEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.HostsFile))
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    HostsFileDisplayView popup = new HostsFileDisplayView(args.HostsFile);
                    popup.Show();
                });
            }
        }

        #endregion

        #region Abstract definitions

        /// <summary>
        /// Method to be extended by child classes, to set host/cm targets prior to other method calls
        /// Will perform validation as well, using methods found in the attached partial
        /// </summary>
        /// <param name="target">The HostType to target</param>
        /// <returns>Tuple containing the currently selected Host/CM</returns>
        internal abstract (VibesHost, VibesCm, ObservableCollection<VibesCm>) SetTargets(object target);

        /// <summary>
        /// Method to be extended by child classes, to set host target prior to other method calls
        /// Will perform validation as well, using methods found in the attached partial
        /// </summary>
        /// <param name="target">The HostType to target</param>
        internal abstract VibesHost SetTargetHost(object target);

        /// <summary>
        /// Method to be extended by child classes, to swap all find/replace properties after swap
        /// </summary>
        /// <param name="target">The host to target</param>
        internal abstract void SwapPropertyTargets(object target);

        /// <summary>
        /// Method to be extended by child classes, pre-populate find/replace properties
        /// </summary>
        /// <param name="target">The host to target</param>
        internal abstract void PrePopulateTargets(object target);

        #endregion
    }

    /// <summary>
    /// Viewmodel base class for all VM's which control CM's
    /// Contains common GUI bound methods used by all such VM's
    /// </summary>
    internal abstract partial class CmControlBase : VmBase
    {
        #region GUI Bound Methods

        /// <summary>
        /// Starts a remote CM
        /// Feedback is returned via CmSshHelper.OnCmCommandComplete
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        internal void StartCm(object parameter)
        {
            try
            {
                (VibesHost targetHost, VibesCm targetCm, ObservableCollection<VibesCm> targetCms) = SetTargets(parameter);
                // Block a start attempt if CM state is not known to avoid sending frivolous ssh commands to server
                if (targetCm.CmStatus != CmStates.Offline)
                {
                    MessageBox.Show("CM must be confirmed stopped prior to starting!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Log.Error($"Attempted to start CM {targetCm.CmResourceName} in unknown state, skipped");
                    return;
                }
                Task.Run(() => CmSshHelper.StartCm(targetHost, targetCm, GetHashCode()));
                targetCm.CmStatus = CmStates.CommandSent;
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to start CM, Error: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Starts a list of remote CM's
        /// Feedback is returned via CmSshHelper.OnCmCommandComplete
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        internal void StartAll(object parameter)
        {
            try
            {
                (VibesHost targetHost, VibesCm targetCm, ObservableCollection<VibesCm> targetCms) = SetTargets(parameter);
                List<VibesCm> cmsToStart = new List<VibesCm>();

                foreach (VibesCm cm in targetCms)
                {
                    // Block a start attempt if CM state is not known to avoid sending frivolous ssh commands to server
                    if (cm.CmStatus == CmStates.Offline)
                    {
                        cm.CmStatus = CmStates.CommandSent;
                        cmsToStart.Add(cm);
                    }
                }
                Task.Run(() => CmSshHelper.StartCmMultiple(targetHost, cmsToStart, GetHashCode()));
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to start CM's, Error: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Stops a remote CM
        /// Feedback is returned via CmSshHelper.OnCmCommandComplete
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        internal void StopCm(object parameter)
        {
            try
            {
                (VibesHost targetHost, VibesCm targetCm, ObservableCollection<VibesCm> targetCms) = SetTargets(parameter);
                // Block a stop attempt if CM state is not known to avoid sending frivolous ssh commands to server
                if (targetCm.CmStatus != CmStates.Alive)
                {
                    MessageBox.Show("CM must be confirmed started prior to stopping!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Log.Error($"Attempted to stop CM {targetCm.CmResourceName} in unknown state, skipped");
                    return;
                }
                Task.Run(() => CmSshHelper.StopCm(targetHost, targetCm, GetHashCode()));
                targetCm.CmStatus = CmStates.CommandSent;
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to stop CM, Error: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Stops a list of remote CM's
        /// Feedback is returned via CmSshHelper.OnCmCommandComplete
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        internal void StopAll(object parameter)
        {
            try
            {
                (VibesHost targetHost, VibesCm targetCm, ObservableCollection<VibesCm> targetCms) = SetTargets(parameter);
                List<VibesCm> cmsToStop = new List<VibesCm>();

                foreach (VibesCm cm in targetCms)
                {
                    // Block a stop attempt if CM state is not known to avoid sending frivolous ssh commands to server
                    if (cm.CmStatus == CmStates.Alive)
                    {
                        cm.CmStatus = CmStates.CommandSent;
                        cmsToStop.Add(cm);
                    }
                }
                Task.Run(() => CmSshHelper.StopCmMultiple(targetHost, cmsToStop, GetHashCode()));
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to stop CM's, Error: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Polls a remote CM
        /// An event is expected back on command complete, which requires subscribing to CmHttpHelper.PollComplete, in order to update the GUI
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        internal void PollCm(object parameter)
        {
            try
            {
                (VibesHost targetHost, VibesCm targetCm, ObservableCollection<VibesCm> targetCms) = SetTargets(parameter);
                Task.Run(() => CmHttpHelper.CheckCmStatus(targetHost, targetCm, GetHashCode()));
                targetCm.CmStatus = CmStates.Polling;
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to poll CM, Error: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Polls a list of remote CM's
        /// An event is expected back on command complete, which requires subscribing to CmHttpHelper.PollComplete, in order to update the GUI
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        internal void PollAll(object parameter)
        {
            try
            {
                (VibesHost targetHost, VibesCm targetCm, ObservableCollection<VibesCm> targetCms) = SetTargets(parameter);
                foreach (VibesCm cm in targetCms)
                {
                    Task.Run(() => CmHttpHelper.CheckCmStatus(targetHost, cm, GetHashCode()));
                    cm.CmStatus = CmStates.Polling;
                }
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to poll CM's, Error: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Modifies a remote CM based on provided search/replace parameters
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        internal void SwapCm(object parameter)
        {
            try
            {
                (VibesHost targetHost, VibesCm targetCm, ObservableCollection<VibesCm> targetCms) = SetTargets(parameter);
                // Block a swap attempt if CM state is not known to avoid sending frivolous ssh commands to server
                if (targetCm.CmStatus != CmStates.Offline)
                {
                    MessageBox.Show("CM must be stopped prior to editing!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Log.Error($"Attempted to swap CM {targetCm.CmResourceName} in unknown state, skipped");
                    return;
                }
                foreach (DeploymentProperty propertyToChange in targetCm.DeploymentProperties)
                {
                    if (string.IsNullOrEmpty(propertyToChange.SearchPattern) || string.IsNullOrEmpty(propertyToChange.ReplacePattern))
                    {
                        continue;
                    }
                    Task.Run(() => CmSshHelper.AlterCm(targetHost, targetCm, propertyToChange.SearchPattern, propertyToChange.ReplacePattern, GetHashCode()));
                }
                if (targetCm.CmStatus != CmStates.CommandSent) targetCm.CmStatus = CmStates.CommandSent;
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to modify CM, Error: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Swaps a list of remote CM's deployment properties as configured
        /// Feedback is returned via CmSshHelper.OnCmCommandComplete
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        internal void SwapAll(object parameter)
        {
            try
            {
                (VibesHost targetHost, VibesCm targetCm, ObservableCollection<VibesCm> targetCms) = SetTargets(parameter);
                List<VibesCm> cmsToCommand = new List<VibesCm>();
                foreach (VibesCm cm in targetCms)
                {
                    if (cm.CmStatus != CmStates.Offline)
                    {
                        Log.Error($"Attempted to alter CM {cm.CmResourceName} in unknown state, skipped");
                        continue;
                    }
                    else
                    {
                        cmsToCommand.Add(cm);
                        cm.CmStatus = CmStates.CommandSent;
                    }
                }
                Task.Run(() => CmSshHelper.AlterCmMultiple(targetHost, cmsToCommand, GetHashCode()));
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to alter all CMs, Error: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Gets deployment.properties from a remote CM
        /// </summary>
        /// <param name="parameter">The cluster the CM is on</param>
        internal void GetProperties(object parameter)
        {
            try
            {
                (VibesHost targetHost, VibesCm targetCm, ObservableCollection<VibesCm> targetCms) = SetTargets(parameter);
                Task.Run(() => CmSshHelper.GetCmParams(targetHost, targetCm, GetHashCode()));
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to get deployment properties, Error: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Get deployment.properties for all CM's on selected host
        /// </summary>
        /// <param name="parameter">The cluster the the CMs are on</param>
        internal void GetAllProperties(object parameter)
        {
            try
            {
                (VibesHost targetHost, VibesCm targetCm, ObservableCollection<VibesCm> targetCms) = SetTargets(parameter);
                List<VibesCm> cmsToCommand = new List<VibesCm>();
                foreach (VibesCm cm in targetCms)
                {
                    cmsToCommand.Add(cm);
                    cm.CmStatus = CmStates.CommandSent;
                }
                Task.Run(() => CmSshHelper.GetAllCmParams(targetHost, cmsToCommand, GetHashCode()));
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to get all deployment properties, Error: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Gets current hosts file from selected host
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        internal void GetHosts(object parameter)
        {
            try
            {
                VibesHost targetHost = SetTargetHost(parameter);
                Task.Run(() => CmSshHelper.GetHostsFile(targetHost, GetHashCode()));
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable get hosts, Error: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Sets production hosts file for host
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        internal void SetProdHosts(object parameter)
        {
            try
            {
                VibesHost target = SetTargetHost(parameter);
                Task.Run(() => CmSshHelper.SwitchHostsFile(target, true, GetHashCode()));
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to set production hosts, Error: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Sets hlc hosts file for host
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        internal void SetHlcHosts(object parameter)
        {
            try
            {
                VibesHost targetHost = SetTargetHost(parameter);
                Task.Run(() => CmSshHelper.SwitchHostsFile(targetHost, false, GetHashCode()));
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to set production hosts, Error: {ex.Message}", true);
            }
        }

        #endregion       
    }

    /// <summary>
    /// Viewmodel base class for all VM's which control CM's
    /// Contains common validator methods used by all such VM's
    /// </summary>
    internal abstract partial class CmControlBase : VmBase
    {
        #region Validators

        /// <summary>
        /// Checks for necessary params for host transaction and throws ArgumentNullException if not found
        /// </summary>
        /// <param name="host">The host param to test</param>
        internal void CheckHostParameters(VibesHost host)
        {
            if (host == null) throw new ArgumentNullException("Unknown");

            if (string.IsNullOrEmpty(host.Url))         throw new ArgumentException("Host URL");
            if (string.IsNullOrEmpty(host.SshUsername)) throw new ArgumentNullException("SSH username");
            if (string.IsNullOrEmpty(host.SshPassword)) throw new ArgumentNullException("SSH password");
        }

        /// <summary>
        /// Checks for necessary params for single transaction and throws ArgumentNullException if not found
        /// </summary>
        /// <param name="host">The host param to test</param>
        /// <param name="cm">The CM param to test</param>
        internal void CheckSingleParameters(VibesHost host, VibesCm cm)
        {
            if (host == null || cm == null) throw new ArgumentNullException("Unknown");

            CheckHostParameters(host);

            if (string.IsNullOrEmpty(cm.CmResourceName)) throw new ArgumentNullException("CM Resource Name");
            if (string.IsNullOrEmpty(cm.CmCorePath))     throw new ArgumentNullException("CM Core Path");
            if (string.IsNullOrEmpty(cm.CmPath))         throw new ArgumentNullException("CM Path");
            if (string.IsNullOrEmpty(cm.CmPort))         throw new ArgumentNullException("CM Port");
        }

        /// <summary>
        /// Checks for necessary params for bulk transaction and throws ArgumentNullException if not found
        /// </summary>
        /// <param name="collectionToCheck">The collection to be started</param>
        /// <param name="hostToCheck">The host to which the colleciton belongs</param>
        internal void CheckBulkParameters(ICollection<VibesCm> collectionToCheck, VibesHost hostToCheck)
        {
            if (!collectionToCheck.Any())                      throw new ArgumentNullException("Unknown");
            if (string.IsNullOrEmpty(hostToCheck.Url))         throw new ArgumentNullException("Host URL");
            if (string.IsNullOrEmpty(hostToCheck.SshUsername)) throw new ArgumentNullException("SSH username");
            if (string.IsNullOrEmpty(hostToCheck.SshPassword)) throw new ArgumentNullException("SSH password");
        }

        #endregion
    }
}
