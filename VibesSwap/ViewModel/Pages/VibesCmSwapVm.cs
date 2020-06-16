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
    class VibesCmSwapVm : CmControlBase
    {
        #region Constructors

        public VibesCmSwapVm()
        {
            CmsDisplayExec = new ObservableCollection<VibesCm>();
            CmsDisplayOperDb = new ObservableCollection<VibesCm>();
            CmsDisplayOperAppOne = new ObservableCollection<VibesCm>();
            CmsDisplayOperAppTwo = new ObservableCollection<VibesCm>();
            CmsDisplayMs = new ObservableCollection<VibesCm>();
            CmsDisplayEns = new ObservableCollection<VibesCm>();

            RefreshCommand = new RelayCommand(LoadData);
            UpdatePropertiesCommand = new RelayCommand(UpdateProperties);
            GetHostsCommand = new RelayCommand(GetHosts);
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

        public ObservableCollection<VibesCm> CmsDisplayExec { get; set; }
        public ObservableCollection<VibesCm> CmsDisplayOperDb { get; set; }
        public ObservableCollection<VibesCm> CmsDisplayOperAppOne { get; set; }
        public ObservableCollection<VibesCm> CmsDisplayOperAppTwo { get; set; }
        public ObservableCollection<VibesCm> CmsDisplayMs { get; set; }
        public ObservableCollection<VibesCm> CmsDisplayEns { get; set; }

        public VibesHost SelectedHostExec { get; set; }
        public VibesCm SelectedCmExec
        {
            get { return _selectedCmExec; }
            set
            {
                _selectedCmExec = value;
                if (value != null)
                {
                    PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                    using (DataContext context = new DataContext())
                    {
                        SelectedHostExec = context.EnvironmentHosts.SingleOrDefault(h => h.Id == value.VibesHostId);
                    }
                }
            }
        }
        private VibesCm _selectedCmExec;

        public VibesHost SelectedHostOperDb { get; set; }
        public VibesCm SelectedCmOperDb
        {
            get { return _selectedCmOperDb; }
            set
            {
                _selectedCmOperDb = value;
                if (value != null)
                {
                    PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                    using (DataContext context = new DataContext())
                    {
                        SelectedHostExec = context.EnvironmentHosts.SingleOrDefault(h => h.Id == value.VibesHostId);
                    }
                }
            }
        }
        private VibesCm _selectedCmOperDb;

        public VibesHost SelectedHostOperAppOne { get; set; }
        public VibesCm SelectedCmOperAppOne
        {
            get { return _selectedCmOperAppOne; }
            set
            {
                _selectedCmOperAppOne = value;
                if (value != null)
                {
                    PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                    using (DataContext context = new DataContext())
                    {
                        SelectedHostExec = context.EnvironmentHosts.SingleOrDefault(h => h.Id == value.VibesHostId);
                    }
                }
            }
        }
        private VibesCm _selectedCmOperAppOne;

        public VibesHost SelectedHostOperAppTwo { get; set; }
        public VibesCm SelectedCmOperAppTwo
        {
            get { return _selectedCmOperAppTwo; }
            set
            {
                _selectedCmOperAppTwo = value;
                if (value != null)
                {
                    PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                    using (DataContext context = new DataContext())
                    {
                        SelectedHostExec = context.EnvironmentHosts.SingleOrDefault(h => h.Id == value.VibesHostId);
                    }
                }
            }
        }
        private VibesCm _selectedCmOperAppTwo;

        public VibesHost SelectedHostMs { get; set; }
        public VibesCm SelectedCmMs
        {
            get { return _selectedCmMs; }
            set
            {
                _selectedCmMs = value;
                if (value != null)
                {
                    PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                    using (DataContext context = new DataContext())
                    {
                        SelectedHostExec = context.EnvironmentHosts.SingleOrDefault(h => h.Id == value.VibesHostId);
                    }
                }
            }
        }
        private VibesCm _selectedCmMs;

        public VibesHost SelectedHostEns { get; set; }
        public VibesCm SelectedCmEns
        {
            get { return _selectedCmEns; }
            set
            {
                _selectedCmEns = value;
                if (value != null)
                {
                    PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                    using (DataContext context = new DataContext())
                    {
                        SelectedHostExec = context.EnvironmentHosts.SingleOrDefault(h => h.Id == value.VibesHostId);
                    }
                }
            }
        }
        private VibesCm _selectedCmEns;

        public RelayCommand RefreshCommand { get; set; }
        public RelayCommand UpdatePropertiesCommand { get; set; }
        public RelayCommand GetHostsCommand { get; set; }
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

        #region Setup

        /// <summary>
        /// Load data from database
        /// </summary>
        /// <param name="parameter">Not used, only for GUI binding</param>
        private void LoadData(object parameter)
        {
            try
            {
                // Exec
                CmsDisplayExec.Clear();
                LoadCmForSwap(CmsDisplayExec, HostTypes.EXEC);
                SelectedCmExec = CmsDisplayExec.FirstOrDefault();
                // Operdb
                CmsDisplayOperDb.Clear();
                LoadCmForSwap(CmsDisplayOperDb, HostTypes.OPERDB);
                SelectedCmOperDb = CmsDisplayOperDb.FirstOrDefault();
                // OperAppOne
                CmsDisplayOperAppOne.Clear();
                LoadCmForSwap(CmsDisplayOperAppOne, HostTypes.OPERAPP1);
                SelectedCmOperAppOne = CmsDisplayOperAppOne.FirstOrDefault();
                // OperAppTwo
                CmsDisplayOperAppTwo.Clear();
                LoadCmForSwap(CmsDisplayOperAppTwo, HostTypes.OPERAPP2);
                SelectedCmOperAppTwo = CmsDisplayOperAppTwo.FirstOrDefault();
                // Ens
                CmsDisplayEns.Clear();
                LoadCmForSwap(CmsDisplayEns, HostTypes.ENS);
                SelectedCmEns = CmsDisplayEns.FirstOrDefault();
                // MS
                CmsDisplayMs.Clear();
                LoadCmForSwap(CmsDisplayMs, HostTypes.MS);
                SelectedCmMs = CmsDisplayMs.FirstOrDefault();

                if (CmsDisplayExec.Count == 0 || CmsDisplayOperDb.Count == 0 || CmsDisplayOperAppOne.Count == 0 || CmsDisplayOperAppTwo.Count == 0 || CmsDisplayEns.Count == 0 || CmsDisplayMs.Count == 0)
                {
                    LoadBoilerPlate();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading VIBES CM's from DB: {ex.Message}");
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
                    if (!CmsDisplayExec.Any())
                    {
                        CmsDisplayExec.Clear();
                        CmsDisplayExec.Add(new VibesCm { CmResourceName = "No CM's Loaded", Id = 0 });
                    }
                    if (!CmsDisplayOperDb.Any())
                    {
                        CmsDisplayOperDb.Clear();
                        CmsDisplayOperDb.Add(new VibesCm { CmResourceName = "No CM's Loaded", Id = 0 });
                    }
                    if (!CmsDisplayOperAppOne.Any())
                    {
                        CmsDisplayOperAppOne.Clear();
                        CmsDisplayOperAppOne.Add(new VibesCm { CmResourceName = "No CM's Loaded", Id = 0 });
                    }
                    if (!CmsDisplayOperAppTwo.Any())
                    {
                        CmsDisplayOperAppTwo.Clear();
                        CmsDisplayOperAppTwo.Add(new VibesCm { CmResourceName = "No CM's Loaded", Id = 0 });
                    }
                    if (!CmsDisplayEns.Any())
                    {
                        CmsDisplayEns.Clear();
                        CmsDisplayEns.Add(new VibesCm { CmResourceName = "No CM's Loaded", Id = 0 });
                    }
                    if (!CmsDisplayMs.Any())
                    {
                        CmsDisplayMs.Clear();
                        CmsDisplayMs.Add(new VibesCm { CmResourceName = "No CM's Loaded", Id = 0 });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading boilerplate for Vibes CM Swap: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
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
                    case HostTypes.EXEC:
                        StartCollection(CmsDisplayExec);
                        break;
                    case HostTypes.OPERDB:
                        StartCollection(CmsDisplayOperDb);
                        break;
                    case HostTypes.OPERAPP1:
                        StartCollection(CmsDisplayOperAppOne);
                        break;
                    case HostTypes.OPERAPP2:
                        StartCollection(CmsDisplayOperAppTwo);
                        break;
                    case HostTypes.ENS:
                        StartCollection(CmsDisplayEns);
                        break;
                    case HostTypes.MS:
                        StartCollection(CmsDisplayMs);
                        break;
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
                    case HostTypes.EXEC:
                        StopCollection(CmsDisplayExec);
                        break;
                    case HostTypes.OPERDB:
                        StopCollection(CmsDisplayOperDb);
                        break;
                    case HostTypes.OPERAPP1:
                        StopCollection(CmsDisplayOperAppOne);
                        break;
                    case HostTypes.OPERAPP2:
                        StopCollection(CmsDisplayOperAppTwo);
                        break;
                    case HostTypes.ENS:
                        StopCollection(CmsDisplayEns);
                        break;
                    case HostTypes.MS:
                        StopCollection(CmsDisplayMs);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to stop all CM's, Error: {ex.Message}");
                Log.Error($"Stack Trace {ex.Message}");
                MessageBox.Show("Error stopping all CM's", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    case HostTypes.EXEC:
                        PollCollection(CmsDisplayExec);
                        break;
                    case HostTypes.OPERDB:
                        PollCollection(CmsDisplayOperDb);
                        break;
                    case HostTypes.OPERAPP1:
                        PollCollection(CmsDisplayOperAppOne);
                        break;
                    case HostTypes.OPERAPP2:
                        PollCollection(CmsDisplayOperAppTwo);
                        break;
                    case HostTypes.ENS:
                        PollCollection(CmsDisplayEns);
                        break;
                    case HostTypes.MS:
                        PollCollection(CmsDisplayMs);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to poll all CM's, Error: {ex.Message}");
                Log.Error($"Stack Trace {ex.Message}");
                MessageBox.Show("Error polling all CM's", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    case HostTypes.EXEC:
                        SwapCollection(CmsDisplayExec);
                        break;
                    case HostTypes.OPERDB:
                        SwapCollection(CmsDisplayOperDb);
                        break;
                    case HostTypes.OPERAPP1:
                        SwapCollection(CmsDisplayOperAppOne);
                        break;
                    case HostTypes.OPERAPP2:
                        SwapCollection(CmsDisplayOperAppTwo);
                        break;
                    case HostTypes.ENS:
                        SwapCollection(CmsDisplayEns);
                        break;
                    case HostTypes.MS:
                        SwapCollection(CmsDisplayMs);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to swap all CM's, Error: {ex.Message}");
                Log.Error($"Stack Trace {ex.Message}");
                MessageBox.Show("Error swapping all CM's", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                if (CmsDisplayExec.Contains(e.Cm))
                {
                    CmsDisplayExec.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayOperDb.Contains(e.Cm))
                {
                    CmsDisplayOperDb.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayOperAppOne.Contains(e.Cm))
                {
                    CmsDisplayOperAppOne.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayOperAppTwo.Contains(e.Cm))
                {
                    CmsDisplayOperAppTwo.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayEns.Contains(e.Cm))
                {
                    CmsDisplayEns.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayMs.Contains(e.Cm))
                {
                    CmsDisplayMs.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
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
                if (CmsDisplayExec.Contains(e.CmChanged))
                {
                    CmsDisplayExec.Single(c => c.Id == e.CmChanged.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayOperDb.Contains(e.CmChanged))
                {
                    CmsDisplayOperDb.Single(c => c.Id == e.CmChanged.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayOperAppOne.Contains(e.CmChanged))
                {
                    CmsDisplayOperAppOne.Single(c => c.Id == e.CmChanged.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayOperAppTwo.Contains(e.CmChanged))
                {
                    CmsDisplayOperAppTwo.Single(c => c.Id == e.CmChanged.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayEns.Contains(e.CmChanged))
                {
                    CmsDisplayEns.Single(c => c.Id == e.CmChanged.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayMs.Contains(e.CmChanged))
                {
                    CmsDisplayMs.Single(c => c.Id == e.CmChanged.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }

                // Update properties
                if (e.DeploymentProperties != null && (e.Host.HostType == HostTypes.EXEC || e.Host.HostType == HostTypes.OPERDB || e.Host.HostType == HostTypes.OPERAPP1 || e.Host.HostType == HostTypes.OPERAPP2 || e.Host.HostType == HostTypes.ENS || e.Host.HostType == HostTypes.MS))
                {
                    StoreDeploymentProperties(e.CmChanged, e.DeploymentProperties);
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        LoadData(null);
                    });
                }

                // Popup hosts
                if (e.Host != null && (e.Host.HostType == HostTypes.EXEC || e.Host.HostType == HostTypes.OPERAPP1 || e.Host.HostType == HostTypes.OPERAPP2))
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
            try
            {
                VibesHost hostToPoll = null;
                VibesCm cmToPoll = null;

                switch (target)
                {
                    case HostTypes.EXEC:
                        if (!RequiredParametersProvided(SelectedHostExec, SelectedCmExec))
                        {
                            throw new ArgumentNullException($"Required parameters not provided");
                        }
                        hostToPoll = SelectedHostExec;
                        cmToPoll = SelectedCmExec;
                        break;
                    case HostTypes.OPERDB:
                        if (!RequiredParametersProvided(SelectedHostOperDb, SelectedCmOperDb))
                        {
                            throw new ArgumentNullException($"Required parameters not provided");
                        }
                        hostToPoll = SelectedHostOperDb;
                        cmToPoll = SelectedCmOperDb;
                        break;
                    case HostTypes.OPERAPP1:
                        if (!RequiredParametersProvided(SelectedHostOperAppOne, SelectedCmOperAppOne))
                        {
                            throw new ArgumentNullException($"Required parameters not provided");
                        }
                        hostToPoll = SelectedHostOperAppOne;
                        cmToPoll = SelectedCmOperAppOne;
                        break;
                    case HostTypes.OPERAPP2:
                        if (!RequiredParametersProvided(SelectedHostOperAppTwo, SelectedCmOperAppTwo))
                        {
                            throw new ArgumentNullException($"Required parameters not provided");
                        }
                        hostToPoll = SelectedHostOperAppTwo;
                        cmToPoll = SelectedCmOperAppTwo;
                        break;
                    case HostTypes.ENS:
                        if (!RequiredParametersProvided(SelectedHostEns, SelectedCmEns))
                        {
                            throw new ArgumentNullException($"Required parameters not provided");
                        }
                        hostToPoll = SelectedHostEns;
                        cmToPoll = SelectedCmEns;
                        break;
                    case HostTypes.MS:
                        if (!RequiredParametersProvided(SelectedHostMs, SelectedCmMs))
                        {
                            throw new ArgumentNullException($"Required parameters not provided");
                        }
                        hostToPoll = SelectedHostMs;
                        cmToPoll = SelectedCmMs;
                        break;
                }

                return (hostToPoll, cmToPoll);
            }
            catch(Exception ex)
            {
                Log.Error($"Error setting parameters for CM command");
                Log.Error($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        #endregion

        #endregion
    }
}
