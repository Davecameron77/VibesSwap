using Serilog;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using VibesSwap.Model;
using VibesSwap.Model.Dimensional;

namespace VibesSwap.ViewModel.Pages.Base
{
    /// <summary>
    /// Base class for all VM's
    /// Provides helper methods to all subclasses
    /// PersistTargetChanges - GUI editable fields will send OnPropertyChanged so that changes can be persisted live
    /// LogAndReportException - Common place to setup exception popups and logging
    /// </summary>
    internal abstract class VmBase : INotifyPropertyChanged
    {
        #region Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        /// <summary>
        /// Autosave CM changes when PropertyChanged fires, used to save objects on keypress
        /// </summary>
        /// <param name="sender">The object changed</param>
        /// <param name="args">Propertychanged args</param>
        internal void PersistTargetChanges(object sender, PropertyChangedEventArgs args)
        {
            try
            {
                // Host on setup page
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
                // CM on setup page
                else if (sender is VibesCm newCmValues)
                {
                    using (DataContext context = new DataContext())
                    {
                        VibesCm cmToUpdate = context.HostCms.Single(c => c.Id == newCmValues.Id);

                        cmToUpdate.CmResourceName = newCmValues.CmResourceName ?? cmToUpdate.CmResourceName;
                        cmToUpdate.CmPort = newCmValues.CmPort ?? cmToUpdate.CmPort;
                        cmToUpdate.CmPath = newCmValues.CmPath ?? cmToUpdate.CmPath;
                        cmToUpdate.CmCorePath = newCmValues.CmCorePath ?? cmToUpdate.CmCorePath;
                        cmToUpdate.CmType = newCmValues.CmType;

                        context.SaveChanges();
                    }
                }
                // CM deployment properties on swap page
                else if (sender is DeploymentProperty property)
                {
                    using (DataContext context = new DataContext())
                    {
                        if (context.HostCms.Any(p => p.Id == property.CmId))
                        {
                            DeploymentProperty propToUpdate = context.DeploymentProperties.SingleOrDefault(p => p.Id == property.Id);
                            propToUpdate.SearchPattern = property.SearchPattern;
                            propToUpdate.ReplacePattern = property.ReplacePattern;

                            context.SaveChanges();
                        }
                    }
                }
                else if (sender is VibesCmSwapVm || sender is EcSwapVm)
                {
                    return;
                }
                else
                {
                    throw new ArgumentException("Unknown type supplied");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to persist target changes on {sender.GetType()}, Error: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Common code for logging and reporting exceptions
        /// </summary>
        /// <param name="exceptionToReport">The exception thrown</param>
        /// <param name="customMessage">Specific message about error</param>
        /// <param name="indDisplayInGui">True if a MessageBox is to be shown</param>
        internal void LogAndReportException(Exception exceptionToReport, string customMessage, bool indDisplayInGui = false)
        {
            Log.Error(customMessage);
            Log.Error(exceptionToReport.StackTrace);
            if (indDisplayInGui) MessageBox.Show(customMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}