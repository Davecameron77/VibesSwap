using Serilog;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using VibesSwap.Model;
using VibesSwap.Model.Dimensional;
using VibesSwap.ViewModel.Helpers;
using VibesSwap.ViewModel.Pages.Base;

namespace VibesSwap.ViewModel.Pages
{
    internal class EcSwapVm : CmControlBase
    {
        #region Constructors

        public EcSwapVm()
        {
            CmsDisplayCommOne = new ObservableCollection<VibesCm>();
            CmsDisplayCommTwo = new ObservableCollection<VibesCm>();

            RefreshCommand = new RelayCommand(LoadData);
            UpdatePropertiesCommand = new RelayCommand(UpdateProperties);
            SetProdHostsCommand = new RelayCommand(SetProdHosts);
            SetHlcHostsCommand = new RelayCommand(SetHlcHosts);
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
        public VibesHost SelectedHostCommTwo { get; set; }
        
        private VibesCm _selectedCmCommOne;
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
        private VibesCm _selectedCmCommTwo;
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
        public RelayCommand SetProdHostsCommand { get; set; }
        public RelayCommand SetHlcHostsCommand { get; set; }

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
                // Comm1
                CmsDisplayCommOne.Clear();
                LoadCmForSwap(CmsDisplayCommOne, HostTypes.COMM1);
                SelectedCmCommOne = CmsDisplayCommOne.FirstOrDefault();
                // Comm2
                CmsDisplayCommTwo.Clear();
                LoadCmForSwap(CmsDisplayCommTwo, HostTypes.COMM2);
                SelectedCmCommTwo = CmsDisplayCommTwo.FirstOrDefault();

                if (CmsDisplayCommOne.Count == 0 || CmsDisplayCommTwo.Count == 0)
                {
                    LoadBoilerPlate();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading CM's from DB: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show("Error loading data to GUI", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Log.Error($"Stack Trace: {ex.StackTrace}");
            }
        }

        #region GUI Bound Handlers

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
                        StartCollection(CmsDisplayCommOne);
                        break;
                    case HostTypes.COMM2:
                        StartCollection(CmsDisplayCommTwo);
                        break;
                    /*
                    Example from other VM
                    CmsDisplayExec does not exist in this VM, CmsDisplayCommOne does not exist in the other
                    Otherwise Functionally identical
                    case HostTypes.Exec:
                        StartCollection(CmsDisplayExec);
                        break;
                    */
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to start all CM's, Error: {ex.Message}");
                Log.Error($"Stack Trace {ex.Message}");
                MessageBox.Show("Error starting all CM's", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        StopCollection(CmsDisplayCommOne);
                        break;
                    case HostTypes.COMM2:
                        StopCollection(CmsDisplayCommTwo);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to stop all CM's, Error: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"Error stopping all CM's", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        PollCollection(CmsDisplayCommOne);
                        break;
                    case HostTypes.COMM2:
                        PollCollection(CmsDisplayCommTwo);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to poll all CM's, Error: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"Error polling all CM's", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Swaps all CM's on specified host
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        private void SwapAll(object parameter)
        {
            try
            {
                switch (parameter)
                {
                    case HostTypes.COMM1:
                        SwapCollection(CmsDisplayCommOne);
                        break;
                    case HostTypes.COMM2:
                        SwapCollection(CmsDisplayCommTwo);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to swap all CM's, Error: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"Error swapping all CM's", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                // This VM is not the sender, return
                if (GetHashCode() != e.SubscriberHashCode)
                {
                    return;
                }
                // Set CM Status
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
                Log.Error($"Stack Trace: {ex.StackTrace}");
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
                // This VM is not the sender, return
                if (e.SubscriberHashCode != GetHashCode())
                {
                    return;
                }

                // Set CM Status
                if (CmsDisplayCommOne.Contains(e.CmChanged))
                {
                    CmsDisplayCommOne.Single(c => c.Id == e.CmChanged.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayCommTwo.Contains(e.CmChanged))
                {
                    CmsDisplayCommTwo.Single(c => c.Id == e.CmChanged.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }

                // Update properties
                if (e.DeploymentProperties != null && (e.Host.HostType == HostTypes.COMM1 || e.Host.HostType == HostTypes.COMM2))
                {
                    PopulateLocalDeploymentProperties(e.CmChanged, e.DeploymentProperties);
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        LoadData(null);
                    });
                }
                
                // Popup hosts
                if (e.Host != null && (e.Host.HostType == HostTypes.COMM1 || e.Host.HostType == HostTypes.COMM2))
                {
                    PopupHostsFile(e);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error updating GUI with CM changes: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
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
        internal override sealed (VibesHost, VibesCm) SetTargets(object target)
        {
            VibesHost hostToPoll = null;
            VibesCm cmToPoll = null;

            switch (target)
            {
                case HostTypes.COMM1:
                    if (!RequiredParametersProvided(SelectedHostCommOne, SelectedCmCommOne))
                    {
                        throw new ArgumentNullException($"Required parameters not provided");
                    }
                    hostToPoll = SelectedHostCommOne;
                    cmToPoll = SelectedCmCommOne;
                    break;
                case HostTypes.COMM2:
                    if (!RequiredParametersProvided(SelectedHostCommTwo, SelectedCmCommTwo))
                    {
                        throw new ArgumentNullException($"Required parameters not provided");
                    }
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
