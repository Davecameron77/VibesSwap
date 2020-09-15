using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
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
    /// Provides a base for CM control VM's, and common fuctions or abstract definitions
    /// </summary>
    internal abstract partial class CmControlBase : VmBase
    {
        #region Methods    

        #region Called on setup

        /// <summary>
        /// Loads stored CM's into provided collection
        /// Called by LoadData() during GUI setup
        /// </summary>
        /// <param name="cmCollection">The collection to load</param>
        /// <param name="hostType">The type of host from which to fetch CM's</param>
        internal void LoadCmForSwap(ICollection<VibesCm> cmCollection, HostTypes hostType, int hostId = 0)
        {
            cmCollection.Clear();
            using (DataContext context = new DataContext())
            {
                if (context.EnvironmentHosts.Any(h => h.HostType == hostType))
                {
                    // HostId sent but not found, return
                    if (hostId != 0 && !context.EnvironmentHosts.Any(h => h.Id == hostId)) return;
                    var hosts = hostId != 0 ? context.EnvironmentHosts.Where(h => h.Id == hostId).OrderBy(h => h.Name) : context.EnvironmentHosts.Where(h => h.HostType == hostType).OrderBy(h => h.Name);

                    foreach (VibesHost host in hosts)
                    {
                        foreach (VibesCm cm in context.HostCms.Where(c => c.VibesHostId == host.Id).Include(c => c.DeploymentProperties).Include(c => c.VibesHost).OrderBy(c => c.CmResourceName))
                        {
                            var newCm = cm.DeepCopy();
                            foreach (DeploymentProperty prop in newCm.DeploymentProperties)
                            {
                                prop.PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                            }
                            cmCollection.Add(newCm);
                            PollCmAsync(newCm);
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

                foreach (XElement node in xProperties.Descendants("parameter"))
                {
                    if (
                        node.Value.ToLower().Contains("http:") || 
                        node.FirstAttribute.Value == "useVibes" ||
                        node.FirstAttribute.Value.Contains("smart_suite_server_name") ||
                        node.FirstAttribute.Value.Contains("smart_suite_username") ||
                        node.FirstAttribute.Value.Contains("smart_suite_password")
                        )
                    {
                        string propertyKey = node.Attribute("name").Value;
                        string propertyVal = node.Value;

                        // Property Exists
                        if (context.DeploymentProperties.Any(p => p.CmId == cmChanged.Id && p.PropertyKey == propertyKey))
                        {
                            DeploymentProperty propToUpdate = context.DeploymentProperties.SingleOrDefault(p => p.CmId == cmChanged.Id && p.PropertyKey == propertyKey);
                            propToUpdate.PropertyValue = propertyVal;
                            context.Update(propToUpdate);
                            continue;
                        }
                        else
                        {
                            // Property does not exist
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
        /// </summary>
        /// <param name="target">The HostType to target</param>
        /// <returns>Tuple containing the currently selected Host/CM</returns>
        internal abstract (VibesHost, VibesCm) SetTargets(object target);

        /// <summary>
        /// Method to be extended by child classes, to set host target prior to other method calls
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

        #region Bulk Transactions

        /// <summary>
        /// Starts entire collection of CM's
        /// </summary>
        /// <param name="collectionToStart">The collection of CM's to start</param>
        /// <param name="hostCredentials">The host to which the CM's belong</param>
        internal void StartCollection(ICollection<VibesCm> collectionToStart, VibesHost hostCredentials)
        {
            List<VibesCm> cmsToStart = new List<VibesCm>();
            CheckBulkParameters(collectionToStart, hostCredentials);

            foreach (VibesCm cm in collectionToStart)
            {
                // Block a start attempt if CM state is not known to avoid sending frivolous ssh commands to server
                if (cm.CmStatus != CmStates.Offline)
                {
                    Log.Error($"Attempted to start CM {cm.CmResourceName} in unknown state, skipped");
                    continue;
                }
                cm.CmStatus = CmStates.CommandSent;
                Task.Run(() => CmSshHelper.StartCm(hostCredentials, cm, GetHashCode()));
            }
        }

        /// <summary>
        /// Stops entire collection of CM's
        /// </summary>
        /// <param name="collectionToStop">The collection of CM's to stop</param>
        /// <param name="hostCredentials">The host to which the CM's belong</param>
        internal void StopCollection(ICollection<VibesCm> collectionToStop, VibesHost hostCredentials)
        {
            List<VibesCm> cmsToStop = new List<VibesCm>();
            CheckBulkParameters(collectionToStop, hostCredentials);

            foreach (VibesCm cm in collectionToStop)
            {
                // Block a stop attempt if CM state is not known to avoid sending frivolous ssh commands to server
                if (cm.CmStatus != CmStates.Alive)
                {
                    Log.Error($"Attempted to stop CM {cm.CmResourceName} in unknown state, skipped");
                    continue;
                }
                cm.CmStatus = CmStates.CommandSent;
                Task.Run(() => CmSshHelper.StopCm(hostCredentials, cm, GetHashCode()));
            }
        }

        /// <summary>
        /// Swaps entire collection of CM's
        /// </summary>
        /// <param name="collectionToSwap">The collection of CM's to swap</param>
        /// <param name="hostCredentials">The host to which the CM's belong</param>
        internal void SwapCollection(ICollection<VibesCm> collectionToSwap, VibesHost hostCredentials)
        {
            CheckBulkParameters(collectionToSwap, hostCredentials);
            List<(VibesHost hostToEdit, VibesCm cmToEdit, string paramToEdit, string paramToChange)> editsToMake = new List<(VibesHost, VibesCm, string, string)>();

            foreach (VibesCm cm in collectionToSwap)
            {
                foreach (DeploymentProperty propertyToChange in cm.DeploymentProperties)
                {
                    if (string.IsNullOrEmpty(propertyToChange.SearchPattern) || string.IsNullOrEmpty(propertyToChange.ReplacePattern))
                    {
                        continue;
                    }
                    else if (cm.CmStatus != CmStates.Offline)
                    {
                        Log.Error($"Attempted to swap CM {cm.CmResourceName} in unknown state, skipped");
                        continue;
                    }
                    else
                    {
                        Task.Run(() => CmSshHelper.AlterCm(hostCredentials, cm, propertyToChange.SearchPattern, propertyToChange.ReplacePattern, GetHashCode()));
                    }
                }
                if (cm.CmStatus != CmStates.CommandSent) cm.CmStatus = CmStates.CommandSent;
            }
        }

        /// <summary>
        /// Polls entire collection of CM's
        /// </summary>
        /// <param name="collectionToPoll">The collection of CM's to poll</param>
        /// <param name="hostCredentials">The host to which the CM's belong</param>
        internal void PollCollection(ICollection<VibesCm> collectionToPoll, VibesHost hostCredentials)
        {
            CheckBulkParameters(collectionToPoll, hostCredentials);

            foreach (VibesCm cm in collectionToPoll)
            {
                Task.Run(() => CmHttpHelper.CheckCmStatus(cm.VibesHost, cm, GetHashCode()));
                cm.CmStatus = CmStates.Polling;
            }
        }

        #endregion

        #endregion
    }

    // GUI Bound
    internal abstract partial class CmControlBase : VmBase
    {
        #region GUI Bound Methods

        /// <summary>
        /// Starts a remote CM
        /// An event is expected back on command complete, which requires subscribing to CmHttpHelper.PollComplete, in order to update the GUI
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        internal void StartCm(object parameter)
        {
            try
            {
                var targets = SetTargets(parameter);
                // Block a start attempt if CM state is not known to avoid sending frivolous ssh commands to server
                if (targets.Item2.CmStatus != CmStates.Offline)
                {
                    MessageBox.Show("CM must be confirmed stopped prior to starting!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Log.Error($"Attempted to start CM {targets.Item2.CmResourceName} in unknown state, skipped");
                    return;
                }
                Task.Run(() => CmSshHelper.StartCm(targets.Item1, targets.Item2, GetHashCode()));
                targets.Item2.CmStatus = CmStates.CommandSent;
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to start CM, Error: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Stops a remote CM
        /// An event is expected back on command complete, which requires subscribing to CmHttpHelper.PollComplete, in order to update the GUI
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        internal void StopCm(object parameter)
        {
            try
            {
                var targets = SetTargets(parameter);
                // Block a stop attempt if CM state is not known to avoid sending frivolous ssh commands to server
                if (targets.Item2.CmStatus != CmStates.Alive)
                {
                    MessageBox.Show("CM must be confirmed started prior to stopping!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Log.Error($"Attempted to stop CM {targets.Item2.CmResourceName} in unknown state, skipped");
                    return;
                }
                Task.Run(() => CmSshHelper.StopCm(targets.Item1, targets.Item2, GetHashCode()));
                targets.Item2.CmStatus = CmStates.CommandSent;
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to stop CM, Error: {ex.Message}", true);
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
                var targets = SetTargets(parameter);
                Task.Run(() => CmHttpHelper.CheckCmStatus(targets.Item1, targets.Item2, GetHashCode()));
                targets.Item2.CmStatus = CmStates.Polling;
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to poll CM, Error: {ex.Message}", true);
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
                var targets = SetTargets(parameter);
                // Block a swap attempt if CM state is not known to avoid sending frivolous ssh commands to server
                if (targets.Item2.CmStatus != CmStates.Offline)
                {
                    MessageBox.Show("CM must be stopped prior to editing!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Log.Error($"Attempted to swap CM {targets.Item2.CmResourceName} in unknown state, skipped");
                    return;
                }
                foreach (DeploymentProperty propertyToChange in targets.Item2.DeploymentProperties)
                {
                    if (string.IsNullOrEmpty(propertyToChange.SearchPattern) || string.IsNullOrEmpty(propertyToChange.ReplacePattern))
                    {
                        continue;
                    }
                    Task.Run(() => CmSshHelper.AlterCm(targets.Item1, targets.Item2, propertyToChange.SearchPattern, propertyToChange.ReplacePattern, GetHashCode()));
                }
                if (targets.Item2.CmStatus != CmStates.CommandSent) targets.Item2.CmStatus = CmStates.CommandSent;
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to modify CM, Error: {ex.Message}", true);
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
                var targets = SetTargets(parameter);
                Task.Run(() => CmSshHelper.GetCmParams(targets.Item1, targets.Item2, GetHashCode()));
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to get deployment properties, Error: {ex.Message}", true);
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
                VibesHost target = SetTargetHost(parameter);
                Task.Run(() => CmSshHelper.GetHostsFile(target, GetHashCode()));
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
                VibesHost target = SetTargetHost(parameter);
                Task.Run(() => CmSshHelper.SwitchHostsFile(target, false, GetHashCode()));
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to set production hosts, Error: {ex.Message}", true);
            }
        }

        #endregion       
    }

    // Validators
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

            if (string.IsNullOrEmpty(host.Url)) throw new ArgumentException("Host URL");
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
            if (string.IsNullOrEmpty(cm.CmCorePath)) throw new ArgumentNullException("CM Core Path");
            if (string.IsNullOrEmpty(cm.CmPath)) throw new ArgumentNullException("CM Path");
        }

        /// <summary>
        /// Checks for necessary params for bulk transaction and throws ArgumentNullException if not found
        /// </summary>
        /// <param name="collectionToCheck">The collection to be started</param>
        /// <param name="hostToCheck">The host to which the colleciton belongs</param>
        internal void CheckBulkParameters(ICollection<VibesCm> collectionToCheck, VibesHost hostToCheck)
        {
            if (!collectionToCheck.Any()) throw new ArgumentNullException("Unknown");
            if (string.IsNullOrEmpty(hostToCheck.Url)) throw new ArgumentNullException("Host URL");
            if (string.IsNullOrEmpty(hostToCheck.SshUsername)) throw new ArgumentNullException("SSH username");
            if (string.IsNullOrEmpty(hostToCheck.SshPassword)) throw new ArgumentNullException("SSH password");
        }

        #endregion
    }
}
