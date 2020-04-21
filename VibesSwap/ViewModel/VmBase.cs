using Serilog;
using System;
using System.ComponentModel;
using System.Linq;
using VibesSwap.Model;

namespace VibesSwap.ViewModel
{
    internal class VmBase : INotifyPropertyChanged
    {
        #region Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

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
                        hostToUpdate.Name = newHostValues.Name != null ? newHostValues.Name : hostToUpdate.Name;
                        hostToUpdate.Url = newHostValues.Url != null ? newHostValues.Url : hostToUpdate.Url;
                        hostToUpdate.SshUsername = newHostValues.SshUsername != null ? newHostValues.SshUsername : hostToUpdate.SshUsername;
                        hostToUpdate.SshPassword = newHostValues.SshPassword != null ? newHostValues.SshPassword : hostToUpdate.SshPassword;
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
                        cmToUpdate.CmResourceName = newCmValues.CmResourceName != null ? newCmValues.CmResourceName : cmToUpdate.CmResourceName;
                        cmToUpdate.CmPort = newCmValues.CmPort != null ? newCmValues.CmPort : cmToUpdate.CmPort;
                        cmToUpdate.CmPath = newCmValues.CmPath != null ? newCmValues.CmPath : cmToUpdate.CmPath;
                        cmToUpdate.CmCorePath = newCmValues.CmCorePath != null ? newCmValues.CmCorePath : cmToUpdate.CmCorePath;
                        cmToUpdate.CmType = newCmValues.CmType;
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to persist target changes on {sender.GetType()}, Error: {ex.Message}");
            }   
        }

        #endregion
    }
}
