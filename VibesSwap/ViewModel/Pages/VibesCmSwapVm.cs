using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VibesSwap.Model;
using VibesSwap.Model.Dimensional;
using VibesSwap.ViewModel.Helpers;

namespace VibesSwap.ViewModel.Pages
{
    class VibesCmSwapVm : VmBase
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
        private VibesCm _selectedCmExec;
        public VibesHost SelectedHostOperDb { get; set; }
        private VibesCm _selectedCmOperDb;
        public VibesHost SelectedHostOperAppOne { get; set; }
        private VibesCm _selectedCmOperAppOne;
        public VibesHost SelectedHostOperAppTwo { get; set; }
        private VibesCm _selectedCmOperAppTwo;
        public VibesHost SelectedHostMs { get; set; }
        private VibesCm _selectedCmEns;
        public VibesHost SelectedHostEns { get; set; }
        private VibesCm _selectedCmMs;

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

        /// <summary>
        /// Modifies a remote CM based on provided search/replace parameters
        /// </summary>
        /// <param name="parameter">Enum HostTypes</param>
        private void SwapCm(object parameter)
        {
            try
            {
                var targets = SetTargets(parameter);
                if (targets.Item2.CmStatus == CmStates.Alive)
                {
                    MessageBox.Show("CM must be stopped prior to editing!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                foreach (DeploymentProperty propertyToChange in targets.Item2.DeploymentProperties)
                {
                    if (string.IsNullOrEmpty(propertyToChange.SearchPattern) || string.IsNullOrEmpty(propertyToChange.ReplacePattern))
                    {
                        continue;
                    }
                    Task.Run(() => CmSshHelper.AlterCm(targets.Item1, targets.Item2, propertyToChange.SearchPattern, propertyToChange.ReplacePattern, GetHashCode()));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to modify CM, Error: {ex.Message}");
            }
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
                    case HostTypes.EXEC:
                        foreach (VibesCm cm in CmsDisplayExec)
                        {
                            using (DataContext context = new DataContext())
                            {
                                SelectedHostExec = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                                Task.Run(() => CmSshHelper.StartCm(SelectedHostExec, cm, GetHashCode()));
                            }
                        }
                        break;
                    case HostTypes.OPERDB:
                        foreach (VibesCm cm in CmsDisplayOperDb)
                        {
                            using (DataContext context = new DataContext())
                            {
                                SelectedHostOperDb = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                                Task.Run((() => CmSshHelper.StartCm(SelectedHostOperDb, cm, GetHashCode())));
                            }
                        }
                        break;
                    case HostTypes.OPERAPP1:
                        foreach (VibesCm cm in CmsDisplayOperAppOne)
                        {
                            using (DataContext context = new DataContext())
                            {
                                SelectedHostOperAppOne = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                                Task.Run((() => CmSshHelper.StartCm(SelectedHostOperAppOne, cm, GetHashCode())));
                            }
                        }
                        break;
                    case HostTypes.OPERAPP2:
                        foreach (VibesCm cm in CmsDisplayOperAppTwo)
                        {
                            using (DataContext context = new DataContext())
                            {
                                SelectedHostOperAppTwo = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                                Task.Run((() => CmSshHelper.StartCm(SelectedHostOperAppTwo, cm, GetHashCode())));
                            }
                        }
                        break;
                    case HostTypes.ENS:
                        foreach (VibesCm cm in CmsDisplayEns)
                        {
                            using (DataContext context = new DataContext())
                            {
                                SelectedHostEns = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                                Task.Run((() => CmSshHelper.StartCm(SelectedHostEns, cm, GetHashCode())));
                            }
                        }
                        break;
                    case HostTypes.MS:
                        foreach (VibesCm cm in CmsDisplayMs)
                        {
                            using (DataContext context = new DataContext())
                            {
                                SelectedHostMs = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                                Task.Run((() => CmSshHelper.StartCm(SelectedHostMs, cm, GetHashCode())));
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
                    case HostTypes.EXEC:
                        foreach (VibesCm cm in CmsDisplayExec)
                        {
                            using (DataContext context = new DataContext())
                            {
                                SelectedHostExec = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                                Task.Run(() => CmSshHelper.StopCm(SelectedHostExec, cm, GetHashCode()));
                            }
                        }
                        break;
                    case HostTypes.OPERDB:
                        foreach (VibesCm cm in CmsDisplayOperDb)
                        {
                            using (DataContext context = new DataContext())
                            {
                                SelectedHostOperDb = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                                Task.Run((() => CmSshHelper.StopCm(SelectedHostOperDb, cm, GetHashCode())));
                            }
                        }
                        break;
                    case HostTypes.OPERAPP1:
                        foreach (VibesCm cm in CmsDisplayOperAppOne)
                        {
                            using (DataContext context = new DataContext())
                            {
                                SelectedHostOperAppOne = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                                Task.Run((() => CmSshHelper.StopCm(SelectedHostOperAppOne, cm, GetHashCode())));
                            }
                        }
                        break;
                    case HostTypes.OPERAPP2:
                        foreach (VibesCm cm in CmsDisplayOperAppTwo)
                        {
                            using (DataContext context = new DataContext())
                            {
                                SelectedHostOperAppTwo = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                                Task.Run((() => CmSshHelper.StopCm(SelectedHostOperAppTwo, cm, GetHashCode())));
                            }
                        }
                        break;
                    case HostTypes.ENS:
                        foreach (VibesCm cm in CmsDisplayEns)
                        {
                            using (DataContext context = new DataContext())
                            {
                                SelectedHostEns = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                                Task.Run((() => CmSshHelper.StopCm(SelectedHostEns, cm, GetHashCode())));
                            }
                        }
                        break;
                    case HostTypes.MS:
                        foreach (VibesCm cm in CmsDisplayMs)
                        {
                            using (DataContext context = new DataContext())
                            {
                                SelectedHostMs = context.EnvironmentHosts.SingleOrDefault(h => h.Id == cm.VibesHostId);
                                Task.Run((() => CmSshHelper.StopCm(SelectedHostMs, cm, GetHashCode())));
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
                    case HostTypes.EXEC:
                        foreach (VibesCm cm in CmsDisplayExec)
                        {
                            PollCmAsync(cm);
                        }
                        break;
                    case HostTypes.OPERDB:
                        foreach (VibesCm cm in CmsDisplayOperDb)
                        {
                            PollCmAsync(cm);
                        }
                        break;
                    case HostTypes.OPERAPP1:
                        foreach (VibesCm cm in CmsDisplayOperAppOne)
                        {
                            PollCmAsync(cm);
                        }
                        break;
                    case HostTypes.OPERAPP2:
                        foreach (VibesCm cm in CmsDisplayOperAppTwo)
                        {
                            PollCmAsync(cm);
                        }
                        break;
                    case HostTypes.ENS:
                        foreach (VibesCm cm in CmsDisplayEns)
                        {
                            PollCmAsync(cm);
                        }
                        break;
                    case HostTypes.MS:
                        foreach (VibesCm cm in CmsDisplayMs)
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
                        foreach (VibesCm cm in CmsDisplayExec)
                        {
                            SelectedCmExec = cm;
                            SwapCm(HostTypes.EXEC);
                        }
                        break;
                    case HostTypes.OPERDB:
                        foreach (VibesCm cm in CmsDisplayOperDb)
                        {
                            SelectedCmOperDb = cm;
                            SwapCm(HostTypes.OPERDB);
                        }
                        break;
                    case HostTypes.OPERAPP1:
                        foreach (VibesCm cm in CmsDisplayOperAppOne)
                        {
                            SelectedCmOperAppOne = cm;
                            SwapCm(HostTypes.OPERAPP1);
                        }
                        break;
                    case HostTypes.OPERAPP2:
                        foreach (VibesCm cm in CmsDisplayOperAppTwo)
                        {
                            SelectedCmOperAppTwo = cm;
                            SwapCm(HostTypes.OPERAPP2);
                        }
                        break;
                    case HostTypes.ENS:
                        foreach (VibesCm cm in CmsDisplayEns)
                        {
                            SelectedCmEns = cm;
                            SwapCm(HostTypes.ENS);
                        }
                        break;
                    case HostTypes.MS:
                        foreach (VibesCm cm in CmsDisplayMs)
                        {
                            SelectedCmMs = cm;
                            SwapCm(HostTypes.MS);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error swapping all CM's, Error: {ex.Message}");
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
                if (GetHashCode() != e.SubscriberHashCode)
                {
                    return;
                }
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
                if (e.DeploymentProperties != null && (e.Host.HostType == HostTypes.EXEC || e.Host.HostType == HostTypes.OPERDB || e.Host.HostType == HostTypes.OPERAPP1 || e.Host.HostType == HostTypes.OPERAPP2 || e.Host.HostType == HostTypes.ENS || e.Host.HostType == HostTypes.MS))
                {
                    PopulateLocalDeploymentProperties(e.CmChanged, e.DeploymentProperties);
                    System.Windows.Application.Current.Dispatcher.Invoke(delegate
                    {
                        LoadData(null);
                    });
                }
                if (CmsDisplayExec.Contains(e.CmChanged))
                {
                    CmsDisplayExec.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayOperDb.Contains(e.CmChanged))
                {
                    CmsDisplayOperDb.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayOperAppOne.Contains(e.CmChanged))
                {
                    CmsDisplayOperAppOne.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayOperAppTwo.Contains(e.CmChanged))
                {
                    CmsDisplayOperAppTwo.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayEns.Contains(e.CmChanged))
                {
                    CmsDisplayEns.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayMs.Contains(e.CmChanged))
                {
                    CmsDisplayMs.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                if (e.Host != null && (e.Host.HostType == HostTypes.EXEC || e.Host.HostType == HostTypes.OPERDB || e.Host.HostType == HostTypes.OPERAPP1 || e.Host.HostType == HostTypes.OPERAPP2 || e.Host.HostType == HostTypes.ENS || e.Host.HostType == HostTypes.MS))
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
                case HostTypes.EXEC:
                    CheckForMissingParams(SelectedHostExec, SelectedCmExec);
                    hostToPoll = SelectedHostExec;
                    cmToPoll = SelectedCmExec;
                    break;
                case HostTypes.OPERDB:
                    CheckForMissingParams(SelectedHostOperDb, SelectedCmOperDb);
                    hostToPoll = SelectedHostOperDb;
                    cmToPoll = SelectedCmOperDb;
                    break;
                case HostTypes.OPERAPP1:
                    CheckForMissingParams(SelectedHostOperAppOne, SelectedCmOperAppOne);
                    hostToPoll = SelectedHostOperAppOne;
                    cmToPoll = SelectedCmOperAppOne;
                    break;
                case HostTypes.OPERAPP2:
                    CheckForMissingParams(SelectedHostOperAppTwo, SelectedCmOperAppTwo);
                    hostToPoll = SelectedHostOperAppTwo;
                    cmToPoll = SelectedCmOperAppTwo;
                    break;
                case HostTypes.ENS:
                    CheckForMissingParams(SelectedHostEns, SelectedCmEns);
                    hostToPoll = SelectedHostEns;
                    cmToPoll = SelectedCmEns;
                    break;
                case HostTypes.MS:
                    CheckForMissingParams(SelectedHostMs, SelectedCmMs);
                    hostToPoll = SelectedHostMs;
                    cmToPoll = SelectedCmMs;
                    break;
            }

            return (hostToPoll, cmToPoll);
        }

        #endregion

        #endregion
    }
}
