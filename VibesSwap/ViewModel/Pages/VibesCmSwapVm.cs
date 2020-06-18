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
            HostsDisplayExec       = new ObservableCollection<VibesHost>();
            HostsDisplayOperDb     = new ObservableCollection<VibesHost>();
            HostsDisplayOperAppOne = new ObservableCollection<VibesHost>();
            HostsDisplayOperAppTwo = new ObservableCollection<VibesHost>();
            HostsDisplayMs         = new ObservableCollection<VibesHost>();
            HostsDisplayEns        = new ObservableCollection<VibesHost>();

            CmsDisplayExec       = new ObservableCollection<VibesCm>();
            CmsDisplayOperDb     = new ObservableCollection<VibesCm>();
            CmsDisplayOperAppOne = new ObservableCollection<VibesCm>();
            CmsDisplayOperAppTwo = new ObservableCollection<VibesCm>();
            CmsDisplayMs         = new ObservableCollection<VibesCm>();
            CmsDisplayEns        = new ObservableCollection<VibesCm>();

            RefreshCommand          = new RelayCommand(LoadData);
            UpdatePropertiesCommand = new RelayCommand(UpdateProperties);
            GetHostsCommand         = new RelayCommand(GetHosts);
            SetProdHostsCommand     = new RelayCommand(SetProdHosts);
            SetHlcHostsCommand      = new RelayCommand(SetHlcHosts);

            StartCmCommand = new RelayCommand(StartCm);
            StopCmCommand  = new RelayCommand(StopCm);
            PollCmCommand  = new RelayCommand(PollCm);
            SwapCmCommand  = new RelayCommand(SwapCm);
            
            StartAllCommand = new RelayCommand(StartAll);
            StopAllCommand  = new RelayCommand(StopAll);
            SwapAllCommand  = new RelayCommand(SwapAll);
            PollAllCommand  = new RelayCommand(PollAll);

            CmHttpHelper.PollComplete += OnPollComplete;
            CmSshHelper.CmCommandComplete += OnCmCommandComplete;

            LoadData(null);
        }

        #endregion

        #region Members

        public ObservableCollection<VibesHost> HostsDisplayExec { get; set; }
        public ObservableCollection<VibesHost> HostsDisplayOperDb { get; set; }
        public ObservableCollection<VibesHost> HostsDisplayOperAppOne { get; set; }
        public ObservableCollection<VibesHost> HostsDisplayOperAppTwo { get; set; }
        public ObservableCollection<VibesHost> HostsDisplayMs { get; set; }
        public ObservableCollection<VibesHost> HostsDisplayEns { get; set; }

        public ObservableCollection<VibesCm> CmsDisplayExec { get; set; }
        public ObservableCollection<VibesCm> CmsDisplayOperDb { get; set; }
        public ObservableCollection<VibesCm> CmsDisplayOperAppOne { get; set; }
        public ObservableCollection<VibesCm> CmsDisplayOperAppTwo { get; set; }
        public ObservableCollection<VibesCm> CmsDisplayMs { get; set; }
        public ObservableCollection<VibesCm> CmsDisplayEns { get; set; }

        private VibesHost _selectedHostExec;
        private VibesHost _selectedHostOperDb;
        private VibesHost _selectedHostOperAppOne;
        private VibesHost _selectedHostOperAppTwo;
        private VibesHost _selectedHostMs;
        private VibesHost _selectedHostEns;

        public VibesHost SelectedHostExec
        {
            get { return _selectedHostExec; }
            set 
            {
                if (value != null)
                {
                    foreach (VibesCm cm in value.VibesCms)
                    {
                        CmsDisplayExec.Add(cm);
                        cm.PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                    }
                    SelectedCmExec = CmsDisplayExec.FirstOrDefault();
                }
                _selectedHostExec = value; 
            }
        }
        public VibesCm SelectedCmExec { get; set; }
        public VibesHost SelectedHostOperDb
        {
            get { return _selectedHostOperDb; }
            set
            {
                if (value != null)
                {
                    foreach (VibesCm cm in value.VibesCms)
                    {
                        CmsDisplayOperDb.Add(cm);
                        cm.PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                    }
                    SelectedCmOperDb = CmsDisplayOperDb.FirstOrDefault();
                }
                _selectedHostOperDb = value;
            }
        }
        public VibesCm SelectedCmOperDb { get; set; }
        public VibesHost SelectedHostOperAppOne
        {
            get { return _selectedHostOperAppOne; }
            set
            {
                if (value != null)
                {
                    foreach (VibesCm cm in value.VibesCms)
                    {
                        CmsDisplayOperAppOne.Add(cm);
                        cm.PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                    }
                    SelectedCmOperAppOne = CmsDisplayOperAppOne.FirstOrDefault();
                }
                _selectedHostOperAppOne = value;
            }
        }
        public VibesCm SelectedCmOperAppOne { get; set; }
        public VibesHost SelectedHostOperAppTwo
        {
            get { return _selectedHostOperAppTwo; }
            set
            {
                if (value != null)
                {
                    foreach (VibesCm cm in value.VibesCms)
                    {
                        CmsDisplayOperAppTwo.Add(cm);
                        cm.PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                    }
                    SelectedCmOperAppTwo = CmsDisplayOperAppTwo.FirstOrDefault();
                }
                _selectedHostOperAppTwo = value;
            }
        }
        public VibesCm SelectedCmOperAppTwo { get; set; }
        public VibesHost SelectedHostMs
        {
            get { return _selectedHostMs; }
            set
            {
                if (value != null)
                {
                    foreach (VibesCm cm in value.VibesCms)
                    {
                        CmsDisplayMs.Add(cm);
                        cm.PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                    }
                    SelectedCmMs = CmsDisplayMs.FirstOrDefault();
                }
                _selectedHostMs = value;
            }
        }
        public VibesCm SelectedCmMs { get; set; }
        public VibesHost SelectedHostEns
        {
            get { return _selectedHostEns; }
            set
            {
                if (value != null)
                {
                    foreach (VibesCm cm in value.VibesCms)
                    {
                        CmsDisplayEns.Add(cm);
                        cm.PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                    }
                    SelectedCmEns = CmsDisplayEns.FirstOrDefault();
                }
                _selectedHostEns = value;
            }
        }
        public VibesCm SelectedCmEns { get; set; }

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
                using (DataContext context = new DataContext())
                {
                    HostsDisplayExec.Clear();
                    CmsDisplayExec.Clear();
                    if (context.EnvironmentHosts.Any(h => h.HostType == HostTypes.EXEC))
                    {
                        foreach(VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.EXEC))
                        {
                            VibesHost newHost = host.DeepCopy();
                            HostsDisplayExec.Add(newHost);
                        }
                        SelectedHostExec = HostsDisplayExec.FirstOrDefault();
                    }
                    if (context.EnvironmentHosts.Any(h => h.HostType == HostTypes.OPERDB))
                    {
                        foreach (VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.OPERDB))
                        {
                            VibesHost newHost = host.DeepCopy();
                            HostsDisplayOperDb.Add(newHost);
                        }
                        SelectedHostOperDb = HostsDisplayOperDb.FirstOrDefault();
                    }
                    if (context.EnvironmentHosts.Any(h => h.HostType == HostTypes.OPERAPP1))
                    {
                        foreach (VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.OPERAPP1))
                        {
                            VibesHost newHost = host.DeepCopy();
                            HostsDisplayOperAppOne.Add(newHost);
                        }
                        SelectedHostOperAppOne = HostsDisplayOperAppOne.FirstOrDefault();
                    }
                    if (context.EnvironmentHosts.Any(h => h.HostType == HostTypes.OPERAPP2))
                    {
                        foreach (VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.OPERAPP2))
                        {
                            VibesHost newHost = host.DeepCopy();
                            HostsDisplayOperAppTwo.Add(newHost);
                        }
                        SelectedHostOperAppTwo = HostsDisplayOperAppTwo.FirstOrDefault();
                    }
                    if (context.EnvironmentHosts.Any(h => h.HostType == HostTypes.ENS))
                    {
                        foreach (VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.ENS))
                        {
                            VibesHost newHost = host.DeepCopy();
                            HostsDisplayEns.Add(newHost);
                        }
                        SelectedHostEns = HostsDisplayEns.FirstOrDefault();
                    }
                    if (context.EnvironmentHosts.Any(h => h.HostType == HostTypes.MS))
                    {
                        foreach (VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.MS))
                        {
                            VibesHost newHost = host.DeepCopy();
                            HostsDisplayMs.Add(newHost);
                        }
                        SelectedHostMs = HostsDisplayMs.FirstOrDefault();
                    }
                }
                /*a
                if (HostsDisplayExec.Count == 0 || HostsDisplayOperDb.Count == 0 || HostsDisplayOperAppOne.Count == 0 || HostsDisplayOperAppTwo.Count == 0 || HostsDisplayEns.Count == 0 || HostsDisplayMs.Count == 0)
                {
                    LoadBoilerPlate();
                }
                */
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading VIBES host's from DB: {ex.Message}");
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
                    if (!HostsDisplayExec.Any())
                    {
                        HostsDisplayExec.Clear();
                        HostsDisplayExec.Add(new VibesHost { Name = "No Host's Loaded", Id = 0 });
                    }
                    if (!HostsDisplayOperDb.Any())
                    {
                        HostsDisplayOperDb.Clear();
                        HostsDisplayOperDb.Add(new VibesHost { Name = "No Host's Loaded", Id = 0 });
                    }
                    if (!HostsDisplayOperAppOne.Any())
                    {
                        HostsDisplayOperAppOne.Clear();
                        HostsDisplayOperAppOne.Add(new VibesHost { Name = "No Host's Loaded", Id = 0 });
                    }
                    if (!HostsDisplayOperAppTwo.Any())
                    {
                        HostsDisplayOperAppTwo.Clear();
                        HostsDisplayOperAppTwo.Add(new VibesHost { Name = "No Host's Loaded", Id = 0 });
                    }
                    if (!HostsDisplayEns.Any())
                    {
                        HostsDisplayEns.Clear();
                        HostsDisplayEns.Add(new VibesHost { Name = "No Host's Loaded", Id = 0 });
                    }
                    if (!HostsDisplayMs.Any())
                    {
                        HostsDisplayMs.Clear();
                        HostsDisplayMs.Add(new VibesHost { Name = "No Host's Loaded", Id = 0 });
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
