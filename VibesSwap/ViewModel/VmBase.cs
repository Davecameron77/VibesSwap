using Serilog;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using VibesSwap.Model;
using VibesSwap.Model.Dimensional;
using VibesSwap.View.Dialogs;
using VibesSwap.ViewModel.Helpers;

namespace VibesSwap.ViewModel
{
    internal class VmBase : INotifyPropertyChanged
    {
        #region Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        /// <summary>
        /// Polls CM asyncronously from code
        /// An event is expected back on poll complete, which requires subscribing to CmHttpHelper.PollComplete, in order to update the GUI
        /// </summary>
        /// <param name="cmToPoll">Reference to CM to be polled</param>
        internal void PollCmAsync(VibesCm cmToPoll)
        {
            try
            {
                DataContext context = new DataContext();
                VibesHost hostToPoll = context.EnvironmentHosts.Single(h => h.Id == cmToPoll.VibesHostId);
                Task.Run(() => CmHttpHelper.CheckCmStatus(hostToPoll, cmToPoll, GetHashCode()));
                cmToPoll.CmStatus = CmStates.Polling;
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to poll CM, Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Autosave CM changes when PropertyChanged fires
        /// </summary>
        /// <param name="sender">The object changed</param>
        /// <param name="args">Propertychanged args</param>
        internal void PersistTargetChanges(object sender, PropertyChangedEventArgs args)
        {
            try
            {
                if (sender is VibesHost newHostValues)
                {
                    using (DataContext context = new DataContext())
                    {
                        VibesHost hostToUpdate = context.EnvironmentHosts.Single(h => h.Id == newHostValues.Id);
                        hostToUpdate.Name = newHostValues.Name ?? hostToUpdate.Name;
                        hostToUpdate.Url = newHostValues.Url ?? hostToUpdate.Url;
                        hostToUpdate.SshUsername = newHostValues.SshUsername ?? hostToUpdate.SshUsername;
                        hostToUpdate.SshPassword = newHostValues.SshPassword ?? hostToUpdate.SshPassword;
                        hostToUpdate.HostType = newHostValues.HostType;
                        hostToUpdate.IndClustered = newHostValues.IndClustered;
                        context.SaveChanges();
                    }
                }
                if (sender is VibesCm newCmValues)
                {
                    using (DataContext context = new DataContext())
                    {
                        VibesCm cmToUpdate = context.HostCms.Single(c => c.Id == newCmValues.Id);
                        cmToUpdate.CmResourceName = newCmValues.CmResourceName ?? cmToUpdate.CmResourceName;
                        cmToUpdate.CmPort = newCmValues.CmPort ?? cmToUpdate.CmPort;
                        cmToUpdate.CmPath = newCmValues.CmPath ?? cmToUpdate.CmPath;
                        cmToUpdate.CmCorePath = newCmValues.CmCorePath ?? cmToUpdate.CmCorePath;
                        cmToUpdate.CmType = newCmValues.CmType;
                        if (cmToUpdate.DeploymentProperties.Any())
                        {
                            cmToUpdate.DeploymentProperties.Clear();
                            foreach (DeploymentProperty prop in newCmValues.DeploymentProperties)
                            {
                                cmToUpdate.DeploymentProperties.Add(prop);
                            }
                        }
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to persist target changes on {sender.GetType()}, Error: {ex.Message}");
            }   
        }

        /// <summary>
        /// Helper method to check for null Host or CM
        /// Throws an exception if either is null, to be caught externally
        /// </summary>
        /// <param name="host">The host param to test</param>
        /// <param name="cm">The CM param to test</param>
        internal void CheckForMissingParams(VibesHost host, VibesCm cm)
        {
            if (host == null || cm == null)
            {
                string missingParameter = host == null ? "Selected Host" : "Selected CM";
                Log.Error($"Error starting/stopping/editing CM, Missing parameter {missingParameter}");
            }
        }

        /// <summary>
        /// Raise popup for deployment properties if attached
        /// </summary>
        /// <param name="args">CmHelperEventArgs</param>
        internal void PopupDeploymentProperties(CmHelperEventArgs args)
        {
            try
            {
                if (!string.IsNullOrEmpty(args.DeploymentProperties))
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        XDocument deploymentProperties = XDocument.Parse(args.DeploymentProperties);
                        DeploymentPropertiesDisplayView popup = new DeploymentPropertiesDisplayView(deploymentProperties);
                        popup.Show();
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error creating deployment properties popup: {ex.Message}");
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
            }
        }

        #endregion
    }
}
