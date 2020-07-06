using Serilog;
using System;
using System.Collections.ObjectModel;
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
            HostsDisplayExec = new ObservableCollection<VibesHost>();
            HostsDisplayOperDb = new ObservableCollection<VibesHost>();
            HostsDisplayOperAppOne = new ObservableCollection<VibesHost>();
            HostsDisplayOperAppTwo = new ObservableCollection<VibesHost>();
            HostsDisplayMs = new ObservableCollection<VibesHost>();
            HostsDisplayEns = new ObservableCollection<VibesHost>();

            CmsDisplayExec = new ObservableCollection<VibesCm>();
            CmsDisplayOperDb = new ObservableCollection<VibesCm>();
            CmsDisplayOperAppOne = new ObservableCollection<VibesCm>();
            CmsDisplayOperAppTwo = new ObservableCollection<VibesCm>();
            CmsDisplayMs = new ObservableCollection<VibesCm>();
            CmsDisplayEns = new ObservableCollection<VibesCm>();

            RefreshCommand = new RelayCommand(LoadData);
            UpdatePropertiesCommand = new RelayCommand(GetProperties);
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
                    CmsDisplayExec.Clear();
                    LoadCmForSwap(CmsDisplayExec, HostTypes.EXEC, value.Id);
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
                    CmsDisplayOperDb.Clear();
                    LoadCmForSwap(CmsDisplayOperDb, HostTypes.OPERDB, value.Id);
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
                    CmsDisplayOperAppOne.Clear();
                    LoadCmForSwap(CmsDisplayOperAppOne, HostTypes.OPERAPP1, value.Id);
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
                    CmsDisplayOperAppTwo.Clear();
                    LoadCmForSwap(CmsDisplayOperAppTwo, HostTypes.OPERAPP2, value.Id);
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
                    CmsDisplayMs.Clear();
                    LoadCmForSwap(CmsDisplayMs, HostTypes.MS, value.Id);
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
                    CmsDisplayEns.Clear();
                    LoadCmForSwap(CmsDisplayEns, HostTypes.ENS, value.Id);
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
                        foreach(VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.EXEC).OrderBy(h => h.Name))
                        {
                            VibesHost newHost = host.DeepCopy();
                            HostsDisplayExec.Add(newHost);
                        }
                        SelectedHostExec = HostsDisplayExec.FirstOrDefault();
                    }
                    HostsDisplayOperDb.Clear();
                    CmsDisplayOperDb.Clear();
                    if (context.EnvironmentHosts.Any(h => h.HostType == HostTypes.OPERDB))
                    {
                        foreach (VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.OPERDB).OrderBy(h => h.Name))
                        {
                            VibesHost newHost = host.DeepCopy();
                            HostsDisplayOperDb.Add(newHost);
                        }
                        SelectedHostOperDb = HostsDisplayOperDb.FirstOrDefault();
                    }
                    HostsDisplayOperAppOne.Clear();
                    CmsDisplayOperAppOne.Clear();
                    if (context.EnvironmentHosts.Any(h => h.HostType == HostTypes.OPERAPP1))
                    {
                        foreach (VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.OPERAPP1).OrderBy(h => h.Name))
                        {
                            VibesHost newHost = host.DeepCopy();
                            HostsDisplayOperAppOne.Add(newHost);
                        }
                        SelectedHostOperAppOne = HostsDisplayOperAppOne.FirstOrDefault();
                    }
                    HostsDisplayOperAppTwo.Clear();
                    CmsDisplayOperAppTwo.Clear();
                    if (context.EnvironmentHosts.Any(h => h.HostType == HostTypes.OPERAPP2))
                    {
                        foreach (VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.OPERAPP2).OrderBy(h => h.Name))
                        {
                            VibesHost newHost = host.DeepCopy();
                            HostsDisplayOperAppTwo.Add(newHost);
                        }
                        SelectedHostOperAppTwo = HostsDisplayOperAppTwo.FirstOrDefault();
                    }
                    HostsDisplayEns.Clear();
                    CmsDisplayEns.Clear();
                    if (context.EnvironmentHosts.Any(h => h.HostType == HostTypes.ENS))
                    {
                        foreach (VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.ENS).OrderBy(h => h.Name))
                        {
                            VibesHost newHost = host.DeepCopy();
                            HostsDisplayEns.Add(newHost);
                        }
                        SelectedHostEns = HostsDisplayEns.FirstOrDefault();
                    }
                    HostsDisplayMs.Clear();
                    CmsDisplayMs.Clear();
                    if (context.EnvironmentHosts.Any(h => h.HostType == HostTypes.MS))
                    {
                        foreach (VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.MS).OrderBy(h => h.Name))
                        {
                            VibesHost newHost = host.DeepCopy();
                            HostsDisplayMs.Add(newHost);
                        }
                        SelectedHostMs = HostsDisplayMs.FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading VIBES host's from DB: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show("Error loading data to GUI", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Bulk Transaction/GUI Bound

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
                        StartCollection(CmsDisplayExec, SelectedHostExec);
                        break;
                    case HostTypes.OPERDB:
                        StartCollection(CmsDisplayOperDb, SelectedHostOperDb);
                        break;
                    case HostTypes.OPERAPP1:
                        StartCollection(CmsDisplayOperAppOne, SelectedHostOperAppOne);
                        break;
                    case HostTypes.OPERAPP2:
                        StartCollection(CmsDisplayOperAppTwo, SelectedHostOperAppTwo);
                        break;
                    case HostTypes.ENS:
                        StartCollection(CmsDisplayEns, SelectedHostEns);
                        break;
                    case HostTypes.MS:
                        StartCollection(CmsDisplayMs, SelectedHostMs);
                        break;
                }
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to start all CM's, Error: {ex.Message}", true);
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
                        StopCollection(CmsDisplayExec, SelectedHostExec);
                        break;
                    case HostTypes.OPERDB:
                        StopCollection(CmsDisplayOperDb, SelectedHostOperDb);
                        break;
                    case HostTypes.OPERAPP1:
                        StopCollection(CmsDisplayOperAppOne, SelectedHostOperAppOne);
                        break;
                    case HostTypes.OPERAPP2:
                        StopCollection(CmsDisplayOperAppTwo, SelectedHostOperAppTwo);
                        break;
                    case HostTypes.ENS:
                        StopCollection(CmsDisplayEns, SelectedHostEns);
                        break;
                    case HostTypes.MS:
                        StopCollection(CmsDisplayMs, SelectedHostMs);
                        break;
                }
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to stop all CM's, Error: {ex.Message}", true);
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
                        PollCollection(CmsDisplayExec, SelectedHostExec);
                        break;
                    case HostTypes.OPERDB:
                        PollCollection(CmsDisplayOperDb, SelectedHostOperDb);
                        break;
                    case HostTypes.OPERAPP1:
                        PollCollection(CmsDisplayOperAppOne, SelectedHostOperAppOne);
                        break;
                    case HostTypes.OPERAPP2:
                        PollCollection(CmsDisplayOperAppTwo, SelectedHostOperAppTwo);
                        break;
                    case HostTypes.ENS:
                        PollCollection(CmsDisplayEns, SelectedHostEns);
                        break;
                    case HostTypes.MS:
                        PollCollection(CmsDisplayMs, SelectedHostMs);
                        break;
                }
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to poll all CM's, Error: {ex.Message}", true);
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
                        SwapCollection(CmsDisplayExec, SelectedHostExec);
                        break;
                    case HostTypes.OPERDB:
                        SwapCollection(CmsDisplayOperDb, SelectedHostOperDb);
                        break;
                    case HostTypes.OPERAPP1:
                        SwapCollection(CmsDisplayOperAppOne, SelectedHostOperAppOne);
                        break;
                    case HostTypes.OPERAPP2:
                        SwapCollection(CmsDisplayOperAppTwo, SelectedHostOperAppTwo);
                        break;
                    case HostTypes.ENS:
                        SwapCollection(CmsDisplayEns, SelectedHostEns);
                        break;
                    case HostTypes.MS:
                        SwapCollection(CmsDisplayMs, SelectedHostMs);
                        break;
                }
            }
            catch (ArgumentNullException ex)
            {
                LogAndReportException(ex, $"Unable to swap all CM's, Error: {ex.Message}", true);
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
                        LoadData(e.CmChanged);
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
        /// Sets up up Host/CM target and checks for missing params
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
                        CheckSingleParameters(SelectedHostExec, SelectedCmExec);

                        hostToPoll = SelectedHostExec;
                        cmToPoll = SelectedCmExec;
                        break;
                    case HostTypes.OPERDB:
                        CheckSingleParameters(SelectedHostOperDb, SelectedCmOperDb);

                        hostToPoll = SelectedHostOperDb;
                        cmToPoll = SelectedCmOperDb;
                        break;
                    case HostTypes.OPERAPP1:
                        CheckSingleParameters(SelectedHostOperAppOne, SelectedCmOperAppOne);

                        hostToPoll = SelectedHostOperAppOne;
                        cmToPoll = SelectedCmOperAppOne;
                        break;
                    case HostTypes.OPERAPP2:
                        CheckSingleParameters(SelectedHostOperAppTwo, SelectedCmOperAppTwo);
                        
                        hostToPoll = SelectedHostOperAppTwo;
                        cmToPoll = SelectedCmOperAppTwo;
                        break;
                    case HostTypes.ENS:
                        CheckSingleParameters(SelectedHostEns, SelectedCmEns);
                        
                        hostToPoll = SelectedHostEns;
                        cmToPoll = SelectedCmEns;
                        break;
                    case HostTypes.MS:
                        CheckSingleParameters(SelectedHostMs, SelectedCmMs);
                        
                        hostToPoll = SelectedHostMs;
                        cmToPoll = SelectedCmMs;
                        break;
                }

                return (hostToPoll, cmToPoll);
            }
            catch(Exception ex)
            {
                Log.Error($"Error setting parameters for CM command, {ex.Message}");
                Log.Error($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Sets up up Host target and checks for missing params
        /// Common code used by all CM commands, but specific to this host
        /// </summary>
        /// <param name="target">The HostType to target</param>
        /// <returns>The target Host/Cm, validated</returns>
        internal override sealed VibesHost SetTargetHost(object target)
        {
            try
            {
                VibesHost hostToPoll = null;

                switch (target)
                {
                    case HostTypes.EXEC:
                        CheckHostParameters(SelectedHostExec);
                        hostToPoll = SelectedHostExec;
                        break;
                    case HostTypes.OPERDB:
                        CheckHostParameters(SelectedHostOperDb);
                        hostToPoll = SelectedHostOperDb;
                        break;
                    case HostTypes.OPERAPP1:
                        CheckHostParameters(SelectedHostOperAppOne);
                        hostToPoll = SelectedHostOperAppOne;
                        break;
                    case HostTypes.OPERAPP2:
                        CheckHostParameters(SelectedHostOperAppTwo);
                        hostToPoll = SelectedHostOperAppTwo;
                        break;
                    case HostTypes.ENS:
                        CheckHostParameters(SelectedHostEns);
                        hostToPoll = SelectedHostEns;
                        break;
                    case HostTypes.MS:
                        CheckHostParameters(SelectedHostMs);
                        hostToPoll = SelectedHostMs;
                        break;
                }

                return hostToPoll;
            }
            catch (Exception ex)
            {
                Log.Error($"Error setting parameters for Host command, {ex.Message}");
                Log.Error($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        #endregion

        #endregion
    }
}
