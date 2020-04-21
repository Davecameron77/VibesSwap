using Serilog;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using VibesSwap.Model;
using VibesSwap.Model.Dimensional;

namespace VibesSwap.ViewModel.Pages
{
    internal class EcSwapVm : VmBase
    {
        #region Constructors

        public EcSwapVm()
        {
            LoadData(null);
        }

        #endregion

        #region Members

        public ObservableCollection<VibesCm> CmsDisplayCommOne { get; set; }
        public ObservableCollection<VibesCm> CmsDisplayCommTwo { get; set; }
        public VibesHost SelectedHostCommOne { get; set; }
        private VibesCm _selectedCmCommOne;
        public VibesHost SelectedHostCommTwo { get; set; }
        private VibesCm _selectedCmCommTwo;        
        public VibesCm SelectedCmCommOne
        {
            get { return _selectedCmCommOne; }
            set 
            { 
                _selectedCmCommOne = value;
                if (value != null)
                {
                    PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                    using (DataContext context = new DataContext())
                    {
                        SelectedHostCommOne = context.EnvironmentHosts.SingleOrDefault(h => h.Id == value.VibesHostId);
                    }
                }
            }
        }
        public VibesCm SelectedCmCommTwo
        {
            get { return _selectedCmCommTwo; }
            set 
            { 
                _selectedCmCommTwo = value;
                if (value != null)
                {
                    PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                    using (DataContext context = new DataContext())
                    {
                        SelectedHostCommTwo = context.EnvironmentHosts.SingleOrDefault(h => h.Id == value.VibesHostId);
                    }
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Load data from database
        /// </summary>
        /// <param name="parameter">Not used, only for GUI binding</param>
        private void LoadData(object parameter)
        {
            try
            {
                using (DataContext context = new DataContext())
                {
                    if (CmsDisplayCommOne == null)
                    {
                        CmsDisplayCommOne = new ObservableCollection<VibesCm>();
                    }
                    if (context.EnvironmentHosts.Any(h => h.HostType == HostTypes.COMM1))
                    {
                        CmsDisplayCommOne.Clear();
                        foreach (VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.COMM1))
                        {
                            foreach (VibesCm cm in context.HostCms.Where(c => c.VibesHostId == host.Id))
                            {
                                CmsDisplayCommOne.Add(cm);
                            }
                        }
                        SelectedCmCommOne = CmsDisplayCommOne.FirstOrDefault();
                    }
                    if (CmsDisplayCommOne.Count == 0)
                    {
                        LoadBoilerPlate();
                    }
                    if (CmsDisplayCommTwo == null)
                    {
                        CmsDisplayCommTwo = new ObservableCollection<VibesCm>();
                    }
                    if (context.EnvironmentHosts.Any(h => h.HostType == HostTypes.COMM2))
                    {
                        CmsDisplayCommTwo.Clear();
                        foreach (VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.COMM2))
                        {
                            foreach (VibesCm cm in context.HostCms.Where(c => c.VibesHostId == host.Id))
                            {
                                CmsDisplayCommTwo.Add(cm);
                            }
                        }
                        SelectedCmCommTwo = CmsDisplayCommTwo.FirstOrDefault();
                    }
                    if (CmsDisplayCommTwo.Count == 0)
                    {
                        LoadBoilerPlate();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading CM's from DB: {ex.Message}");
            }
        }

        /// <summary>
        /// Load boilerplate data only when no data is configured locally
        /// </summary>
        private void LoadBoilerPlate()
        {
            try
            {
                using (DataContext context = new DataContext())
                {
                    if (!CmsDisplayCommOne.Any())
                    {
                        CmsDisplayCommOne.Clear();
                        CmsDisplayCommOne.Add(new VibesCm { CmResourceName = "No CM's Loaded", Id = 0 });
                    }
                    if (!CmsDisplayCommTwo.Any())
                    {
                        CmsDisplayCommTwo.Clear();
                        CmsDisplayCommTwo.Add(new VibesCm { CmResourceName = "No CM's Loaded", Id = 0 });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading boilerplate for EC Swap: {ex.Message}");
            }
        }

        #endregion
    }
}
