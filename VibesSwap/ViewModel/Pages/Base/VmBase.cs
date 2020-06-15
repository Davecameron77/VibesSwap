using Serilog;
using System;
using System.ComponentModel;
using System.Linq;
using VibesSwap.Model;
using VibesSwap.Model.Dimensional;

namespace VibesSwap.ViewModel.Pages.Base
{
    internal abstract class VmBase : INotifyPropertyChanged
    {
        #region Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        /// <summary>
        /// Autosave CM changes when PropertyChanged fires, used to live save objects on keypress
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
                        /*
                        if (cmToUpdate.DeploymentProperties.Any())
                        {
                            cmToUpdate.DeploymentProperties.Clear();
                            foreach (DeploymentProperty prop in newCmValues.DeploymentProperties)
                            {
                                cmToUpdate.DeploymentProperties.Add(prop);
                            }
                        }
                        */
                        context.Update(cmToUpdate);
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
                            // VibesCm cmToUpdate = context.HostCms.Single(c => c.Id == property.CmId).Include(c => c.DeploymentProperties);
                            DeploymentProperty propToUpdate = context.DeploymentProperties.SingleOrDefault(p => p.Id == property.Id);
                            propToUpdate.SearchPattern = property.SearchPattern;
                            propToUpdate.ReplacePattern = property.ReplacePattern;
                            context.Update(propToUpdate);
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

        #endregion

    }
}