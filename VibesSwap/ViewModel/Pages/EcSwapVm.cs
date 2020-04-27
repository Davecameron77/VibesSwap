using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using VibesSwap.Model;
using VibesSwap.Model.Dimensional;
using VibesSwap.ViewModel.Helpers;

namespace VibesSwap.ViewModel.Pages
{
    internal class EcSwapVm : VmBase
    {
        #region Constructors

        public EcSwapVm()
        {
            CmsDisplayCommOne = new ObservableCollection<VibesCm>();
            CmsDisplayCommTwo = new ObservableCollection<VibesCm>();

            RefreshCommand = new RelayCommand(LoadData);
            UpdatePropertiesCommand = new RelayCommand(UpdateProperties);
            StartCmCommand = new RelayCommand(StartCm);
            StopCmCommand = new RelayCommand(StopCm);
            PollCmCommand = new RelayCommand(PollCm);
            SwapCmCommand = new RelayCommand(SwapCm);
            StartAllCommand = new RelayCommand(StartAll);
            StopAllCommand = new RelayCommand(StopAll);
            SwapAllCommand = new RelayCommand(SwapAll);
            PollAllCommand = new RelayCommand(PollAll);

            CmHttpHelper.PollComplete += OnPollComplete;
            CmSshHelper.CmCommandComplete += OnCmCommandComplete;

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

        public RelayCommand RefreshCommand { get; set; }
        public RelayCommand UpdatePropertiesCommand { get; set; }

        public RelayCommand StartCmCommand { get; set; }
        public RelayCommand StopCmCommand { get; set; }
        public RelayCommand PollCmCommand { get; set; }
        public RelayCommand SwapCmCommand { get; set; }

        public RelayCommand StartAllCommand { get; set; }
        public RelayCommand StopAllCommand { get; set; }
        public RelayCommand PollAllCommand { get; set; }
        public RelayCommand SwapAllCommand { get; set; }

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
                    // Comm1
                    CmsDisplayCommOne.Clear();
                    if (context.EnvironmentHosts.Any(h => h.HostType == HostTypes.COMM1))
                    {
                        foreach (VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.COMM1))
                        {
                            foreach (VibesCm cm in context.HostCms.Where(c => c.VibesHostId == host.Id).Include(c => c.DeploymentProperties))
                            {
                                var newCm = cm.DeepCopy();
                                newCm.PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                                CmsDisplayCommOne.Add(newCm);
                            }
                        }
                        SelectedCmCommOne = CmsDisplayCommOne.FirstOrDefault();
                    }
                    // Comm2
                    CmsDisplayCommTwo.Clear();
                    if (context.EnvironmentHosts.Any(h => h.HostType == HostTypes.COMM2))
                    {
                        foreach (VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.COMM2))
                        {
                            foreach (VibesCm cm in context.HostCms.Where(c => c.VibesHostId == host.Id).Include(c => c.DeploymentProperties))
                            {
                                var newCm = cm.DeepCopy();
                                newCm.PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                                CmsDisplayCommTwo.Add(newCm);
                            }
                        }
                        SelectedCmCommTwo = CmsDisplayCommTwo.FirstOrDefault();
                    }
                    if (CmsDisplayCommOne.Count == 0 || CmsDisplayCommTwo.Count == 0)
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

        #region Single Transaction

        /// <summary>
        /// Starts a remote CM
        /// An event is expected back on command complete, which requires subscribing to CmHttpHelper.PollComplete, in order to update the GUI
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        private void StartCm(object parameter)
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
            }
        }

        /// <summary>
        /// Stops a remote CM
        /// An event is expected back on command complete, which requires subscribing to CmHttpHelper.PollComplete, in order to update the GUI
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        private void StopCm(object parameter)
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
            }
        }

        /// <summary>
        /// Polls a remote CM
        /// An event is expected back on command complete, which requires subscribing to CmHttpHelper.PollComplete, in order to update the GUI
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        private void PollCm(object parameter)
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
            }
        }

        private void SwapCm(object parameter)
        {

        }

        /// <summary>
        /// Gets deployment.properties from a remote CM
        /// </summary>
        /// <param name="parameter">The cluster the CM is on</param>
        private void UpdateProperties(object parameter)
        {
            try
            {
                var targets = SetTargets(parameter);
                Task.Run(() => CmSshHelper.GetCmParams(targets.Item1, targets.Item2, GetHashCode()));
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to get deployment properties, Error: {ex.Message}");
            }
        }

        #endregion

        #region Bulk Transaction

        /// <summary>
        /// Starts all CM's asyncronously from GUI via command binding
        /// An event is expected back on command complete, which requires subscribing to CmHttpHelper.PollComplete, in order to update the GUI
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        private void StartAll(object parameter)
        {
            try
            {
                switch (parameter)
                {
                    case HostTypes.COMM1:
                        foreach (VibesCm cm in CmsDisplayCommOne)
                        {
                            using (DataContext context = new DataContext())
                            {
                                SelectedHostCommOne = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                                Task.Run(() => CmSshHelper.StartCm(SelectedHostCommOne, cm, GetHashCode()));
                            }
                                
                        }
                        break;
                    case HostTypes.COMM2:
                        foreach (VibesCm cm in CmsDisplayCommTwo)
                        {
                            using (DataContext context = new DataContext())
                            {
                                SelectedHostCommTwo = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                                Task.Run((() => CmSshHelper.StartCm(SelectedHostCommTwo, cm, GetHashCode())));
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to start all CM's, Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops all CM's asyncronously from GUI via command binding
        /// An event is expected back on command complete, which requires subscribing to CmHttpHelper.PollComplete, in order to update the GUI
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        private void StopAll(object parameter)
        {
            try
            {
                switch (parameter)
                {
                    case HostTypes.COMM1:
                        foreach (VibesCm cm in CmsDisplayCommOne)
                        {
                            using (DataContext context = new DataContext())
                            {
                                SelectedHostCommOne = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                                Task.Run(() => CmSshHelper.StopCm(SelectedHostCommOne, cm, GetHashCode()));
                            }
                        }
                        break;
                    case HostTypes.COMM2:
                        foreach (VibesCm cm in CmsDisplayCommTwo)
                        {
                            using (DataContext context = new DataContext())
                            {
                                SelectedHostCommTwo = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                                Task.Run(() => CmSshHelper.StopCm(SelectedHostCommTwo, cm, GetHashCode()));
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to stop all CM's, Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Polls all CM's asyncronously from GUI via command binding
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        private void PollAll(object parameter)
        {
            try
            {
                switch (parameter)
                {
                    case HostTypes.COMM1:
                        foreach (VibesCm cm in CmsDisplayCommOne)
                        {
                            PollCmAsync(cm);
                        }
                        break;
                    case HostTypes.COMM2:
                        foreach (VibesCm cm in CmsDisplayCommTwo)
                        {
                            PollCmAsync(cm);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to poll CM, Error: {ex.Message}");
            }
        }

        private void SwapAll(object parameter)
        {

        }

        #endregion

        #region Handlers

        /// <summary>
        /// Event handler for a completed HTTP poll
        /// Will update GUI for the CM modified
        /// </summary>
        /// <param name="sender">Sender of event, expected to be null</param>
        /// <param name="e">CmHelperEventArgs with relevant details</param>
        private void OnPollComplete(object sender, CmHelperEventArgs e)
        {
            try
            {
                if (GetHashCode() != e.SubscriberHashCode)
                {
                    return;
                }
                if (CmsDisplayCommOne.Contains(e.Cm))
                {
                    CmsDisplayCommOne.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayCommTwo.Contains(e.Cm))
                {
                    CmsDisplayCommOne.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error updating GUI with CM changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for a completed SSH command
        /// Will update GUI for the CM changed
        /// </summary>
        /// <param name="sender">Sender of event, expected to be null</param>
        /// <param name="e">CmHelperEventArgs with relevant details</param>
        private void OnCmCommandComplete(object sender, CmHelperEventArgs e)
        {
            try
            {
                if (e.SubscriberHashCode != GetHashCode())
                {
                    return;
                }
                if (e.DeploymentProperties != null && (e.Host.HostType == HostTypes.COMM1 || e.Host.HostType == HostTypes.COMM2))
                {
                    PopulateLocalDeploymentProperties(e.CmChanged, e.DeploymentProperties);
                    System.Windows.Application.Current.Dispatcher.Invoke(delegate
                    {
                        LoadData(null);
                        if (e.CmChanged != null)
                        {
                            PollCmAsync(e.CmChanged);
                        }
                    });
                }
                if (CmsDisplayCommOne.Contains(e.CmChanged))
                {
                    CmsDisplayCommOne.Single(c => c.Id == e.CmChanged.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayCommTwo.Contains(e.CmChanged))
                {
                    CmsDisplayCommTwo.Single(c => c.Id == e.CmChanged.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                if (e.Host != null && (e.Host.HostType == HostTypes.COMM1 || e.Host.HostType == HostTypes.COMM2))
                {
                    PopupHostsFile(e);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error updating GUI with CM changes: {ex.Message}");
            }
        }       

        #endregion

        #region Helpers

        /// <summary>
        /// Setups up Host/CM target and checks for missing params
        /// Common code used by all CM commands, but specific to this host
        /// </summary>
        /// <param name="target">The HostType to target</param>
        /// <returns>Tuple containing the target Host/Cm, validated</returns>
        private (VibesHost, VibesCm) SetTargets(object target)
        {
            VibesHost hostToPoll = null;
            VibesCm cmToPoll = null;
            switch (target)
            {
                case HostTypes.COMM1:
                    CheckForMissingParams(SelectedHostCommOne, SelectedCmCommOne);
                    hostToPoll = SelectedHostCommOne;
                    cmToPoll = SelectedCmCommOne;
                    break;
                case HostTypes.COMM2:
                    hostToPoll = SelectedHostCommTwo;
                    cmToPoll = SelectedCmCommTwo;
                    break;
            }

            return (hostToPoll, cmToPoll);
        }

        #endregion

        #endregion
    }
}
