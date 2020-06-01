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
    internal abstract class CmControlBase : VmBase
    {
        #region Members



        #endregion

        #region Methods

        #region Utility Methods

        /// <summary>
        /// Helper method to check for null Host or CM
        /// </summary>
        /// <param name="host">The host param to test</param>
        /// <param name="cm">The CM param to test</param>
        /// <returns>Bool, false if parameter is missing otherwise true</returns>
        internal bool RequiredParametersProvided(VibesHost host, VibesCm cm)
        {
            try
            {
                bool parametersProvided = true;

                if (host == null || cm == null)
                {
                    string missingParameter = host == null ? "Selected Host" : "Selected CM";

                    Log.Error($"Error starting/stopping/editing CM, Missing parameter {missingParameter}");
                    parametersProvided = false;
                }

                return parametersProvided;
            }
            catch (Exception ex)
            {
                Log.Error($"Error starting/stopping/editing CM: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Stores deployment properties if returned by SSH command
        /// Used to populate VibesCmSwapView and EcSwapView
        /// </summary>
        /// <param name="cmChanged">The CM which was queried</param>
        /// <param name="deploymentProperties">The deployment properties file fetched</param>
        internal void StoreDeploymentProperties(VibesCm cmChanged, string deploymentProperties)
        {
            try
            {
                using (DataContext context = new DataContext())
                {
                    VibesCm cmToUpdate = context.HostCms.SingleOrDefault(c => c.Id == cmChanged.Id);
                    XDocument xProperties = XDocument.Parse(deploymentProperties);

                    foreach (XElement node in xProperties.Descendants("parameter"))
                    {
                        if (node.Value.ToLower().Contains("http:"))
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
            catch (Exception ex)
            {
                Log.Error($"Error storing deployment properties: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Raises popup for hosts file if attached
        /// </summary>
        /// <param name="args">CmHelperEvenArgs</param>
        internal void PopupHostsFile(CmHelperEventArgs args)
        {
            try
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
            catch (Exception ex)
            {
                Log.Error($"Error creating hosts file popup: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Loads stored CM's into provided collection
        /// </summary>
        /// <param name="cmCollection">The collection to load</param>
        /// <param name="hostType">The type of host from which to fetch CM's</param>
        internal void LoadCmForSwap(ICollection<VibesCm> cmCollection, HostTypes hostType)
        {
            try
            {
                using (DataContext context = new DataContext())
                {
                    if (context.EnvironmentHosts.Any(h => h.HostType == hostType))
                    {
                        foreach (VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == hostType))
                        {
                            foreach (VibesCm cm in context.HostCms.Where(c => c.VibesHostId == host.Id).Include(c => c.DeploymentProperties))
                            {
                                var newCm = cm.DeepCopy();
                                newCm.PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                                cmCollection.Add(newCm);
                                PollCmAsync(newCm);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading CM's to GUI: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Method to be extended by child classes, to set targets prior to other method calls
        /// </summary>
        /// <param name="target">The HostType to target</param>
        /// <returns>Tuple containing the currently selected Host/CM</returns>
        internal abstract (VibesHost, VibesCm) SetTargets(object target);

        #endregion

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
                Task.Run(() => CmSshHelper.StartCm(targets.Item1, targets.Item2, GetHashCode()));
                targets.Item2.CmStatus = CmStates.Polling;
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to start CM, Error: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"Unable to start CM", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Task.Run(() => CmSshHelper.StopCm(targets.Item1, targets.Item2, GetHashCode()));
                targets.Item2.CmStatus = CmStates.Polling;
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to stop CM, Error: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"Unable to stop CM", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            catch (Exception ex)
            {
                Log.Error($"Unable to poll CM, Error: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"Unable to poll CM", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                if (targets.Item2.CmStatus == CmStates.Alive)
                {
                    MessageBox.Show("CM must be stopped prior to editing!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to modify CM, Error: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"Unable to swap CM", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }     

        /// <summary>
        /// Gets deployment.properties from a remote CM
        /// </summary>
        /// <param name="parameter">The cluster the CM is on</param>
        internal void UpdateProperties(object parameter)
        {
            try
            {
                var targets = SetTargets(parameter);
                Task.Run(() => CmSshHelper.GetCmParams(targets.Item1, targets.Item2, GetHashCode()));
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to get deployment properties, Error: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"Unable to Update Properties", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var targets = SetTargets(parameter);
                Task.Run(() => CmSshHelper.SwitchHostsFile(targets.Item1, true, GetHashCode()));
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to set production hosts, Error: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"Unable to set Production Hosts", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var targets = SetTargets(parameter);
                Task.Run(() => CmSshHelper.SwitchHostsFile(targets.Item1, false, GetHashCode()));
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to set production hosts, Error: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"Unable to set HLC hosts", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Bulk Transactions

        /// <summary>
        /// Polls CM asyncronously from code
        /// An event is expected back on poll complete, which requires subscribing to CmHttpHelper.PollComplete, in order to update the GUI
        /// </summary>
        /// <param name="cmToPoll">Reference to CM to be polled</param>
        internal void PollCmAsync(VibesCm cmToPoll)
        {
            try
            {
                using (DataContext context = new DataContext())
                {
                    VibesHost hostToPoll = context.EnvironmentHosts.Single(h => h.Id == cmToPoll.VibesHostId);
                    Task.Run(() => CmHttpHelper.CheckCmStatus(hostToPoll, cmToPoll, GetHashCode()));
                    cmToPoll.CmStatus = CmStates.Polling;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to poll CM, Error: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }
        
        /// <summary>
        /// Starts entire collection of CM's
        /// </summary>
        /// <param name="collectionToStart">The collection of CM's to start</param>
        internal void StartCollection(ICollection<VibesCm> collectionToStart)
        {
            try
            {
                foreach (VibesCm cm in collectionToStart)
                {
                    using (DataContext context = new DataContext())
                    {
                        VibesHost host = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                        Task.Run(() => CmSshHelper.StartCm(host, cm, GetHashCode()));
                        cm.CmStatus = CmStates.Polling;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error starting all CM's in collection: {ex.Message}");
                Log.Error($"Stack Trace {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Stops entire collection of CM's
        /// </summary>
        /// <param name="collectionToStop">The collection of CM's to stop</param>
        internal void StopCollection(ICollection<VibesCm> collectionToStop)
        {
            try
            {
                foreach (VibesCm cm in collectionToStop)
                {
                    using (DataContext context = new DataContext())
                    {
                        VibesHost host = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                        Task.Run(() => CmSshHelper.StopCm(host, cm, GetHashCode()));
                        cm.CmStatus = CmStates.Polling;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error starting all CM's in collection: {ex.Message}");
                Log.Error($"Stack Trace {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Swaps entire collection of CM's
        /// </summary>
        /// <param name="collectionToPoll">The collection of CM's to swap</param>
        internal void SwapCollection(ICollection<VibesCm> collectionToPoll)
        {
            try
            {
                foreach (VibesCm cm in collectionToPoll)
                {
                    using (DataContext context = new DataContext())
                    {
                        VibesHost host = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                        
                        foreach (DeploymentProperty propertyToChange in cm.DeploymentProperties)
                        {
                            if (string.IsNullOrEmpty(propertyToChange.SearchPattern) || string.IsNullOrEmpty(propertyToChange.ReplacePattern))
                            {
                                continue;
                            }
                            Task.Run(() => CmSshHelper.AlterCm(host, cm, propertyToChange.SearchPattern, propertyToChange.ReplacePattern, GetHashCode()));
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error starting all CM's in collection: {ex.Message}");
                Log.Error($"Stack Trace {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Polls entire collection of CM's
        /// </summary>
        /// <param name="collectionToPoll">The collection of CM's to poll</param>
        internal void PollCollection(ICollection<VibesCm> collectionToPoll)
        {
            try
            {
                foreach (VibesCm cm in collectionToPoll)
                {
                    using (DataContext context = new DataContext())
                    {
                        VibesHost host = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                        Task.Run(() => CmHttpHelper.CheckCmStatus(host, cm, GetHashCode()));
                        cm.CmStatus = CmStates.Polling;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error starting all CM's in collection: {ex.Message}");
                Log.Error($"Stack Trace {ex.StackTrace}");
                throw;
            }
        }

        #endregion

        #endregion
    }
}
