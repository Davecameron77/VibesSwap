using Microsoft.EntityFrameworkCore;
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
    /// <summary>
    /// VM for CM Swap view
    /// VM logic here is only for appliation specific tasks
    /// All HTTP/SSH calls go via CmControlBase
    /// </summary>
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

            CmsDisplayExec = new ObservableCollection<VibesCm>();
            CmsDisplayOperDb = new ObservableCollection<VibesCm>();
            CmsDisplayOperAppOne = new ObservableCollection<VibesCm>();
            CmsDisplayOperAppTwo = new ObservableCollection<VibesCm>();
            CmsDisplayMs = new ObservableCollection<VibesCm>();

            RefreshCommand = new RelayCommand(LoadData);
            UpdatePropertiesCommand = new RelayCommand(GetProperties);
            GetHostsCommand = new RelayCommand(GetHosts);
            SetProdHostsCommand = new RelayCommand(SetProdHosts);
            SetHlcHostsCommand = new RelayCommand(SetHlcHosts);
            SwitchTermsCommand = new RelayCommand(SwapPropertyTargets);
            PrePopulateTermsCommand = new RelayCommand(PrePopulateTargets);

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

        public ObservableCollection<VibesCm> CmsDisplayExec { get; set; }
        public ObservableCollection<VibesCm> CmsDisplayOperDb { get; set; }
        public ObservableCollection<VibesCm> CmsDisplayOperAppOne { get; set; }
        public ObservableCollection<VibesCm> CmsDisplayOperAppTwo { get; set; }
        public ObservableCollection<VibesCm> CmsDisplayMs { get; set; }

        private VibesHost _selectedHostExec;
        private VibesHost _selectedHostOperDb;
        private VibesHost _selectedHostOperAppOne;
        private VibesHost _selectedHostOperAppTwo;
        private VibesHost _selectedHostMs;

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

        public RelayCommand RefreshCommand { get; set; }
        public RelayCommand UpdatePropertiesCommand { get; set; }
        public RelayCommand GetHostsCommand { get; set; }
        public RelayCommand SetProdHostsCommand { get; set; }
        public RelayCommand SetHlcHostsCommand { get; set; }
        public RelayCommand SwitchTermsCommand { get; set; }
        public RelayCommand PrePopulateTermsCommand { get; set; }

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
                // Single CM is updated, only update that one CM
                using (DataContext context = new DataContext())
                {
                    if (parameter is VibesCm cmChanged)
                    {
                        VibesCm newCm = context.HostCms.Where(c => c.Id == cmChanged.Id).Include(c => c.DeploymentProperties).Include(c => c.VibesHost).FirstOrDefault().DeepCopy();

                        if (CmsDisplayExec.Contains(cmChanged))
                        {
                            CmsDisplayExec.Remove(CmsDisplayExec.Single(c => c.Id == cmChanged.Id));
                            CmsDisplayExec.Add(newCm);
                            SelectedCmExec = newCm;
                            SelectedHostExec = HostsDisplayExec.SingleOrDefault(h => h.Id == newCm.VibesHost.Id);
                        }
                        if (CmsDisplayOperDb.Contains(cmChanged))
                        {
                            CmsDisplayOperDb.Remove(CmsDisplayOperDb.Single(c => c.Id == cmChanged.Id));
                            CmsDisplayOperDb.Add(newCm);
                            SelectedCmOperDb = newCm;
                            SelectedHostOperDb = HostsDisplayOperDb.SingleOrDefault(h => h.Id == newCm.VibesHost.Id);
                        }
                        if (CmsDisplayOperAppOne.Contains(cmChanged))
                        {
                            CmsDisplayOperAppOne.Remove(CmsDisplayOperAppOne.Single(c => c.Id == cmChanged.Id));
                            CmsDisplayOperAppOne.Add(newCm);
                            SelectedCmOperAppOne = newCm;
                            SelectedHostOperAppOne = HostsDisplayOperAppOne.SingleOrDefault(h => h.Id == newCm.VibesHost.Id);
                        }
                        if (CmsDisplayOperAppTwo.Contains(cmChanged))
                        {
                            CmsDisplayOperAppTwo.Remove(CmsDisplayOperAppTwo.Single(c => c.Id == cmChanged.Id));
                            CmsDisplayOperAppTwo.Add(newCm);
                            SelectedCmOperAppTwo = newCm;
                            SelectedHostOperAppTwo = HostsDisplayOperAppTwo.SingleOrDefault(h => h.Id == newCm.VibesHost.Id);
                        }
                        if (CmsDisplayMs.Contains(cmChanged))
                        {
                            CmsDisplayMs.Remove(CmsDisplayMs.Single(c => c.Id == cmChanged.Id));
                            CmsDisplayMs.Add(newCm);
                            SelectedCmMs = newCm;
                            SelectedHostMs = HostsDisplayMs.SingleOrDefault(h => h.Id == newCm.VibesHost.Id);
                        }
                        return;
                    }
                }

                // Initial Load
                using (DataContext context = new DataContext())
                {
                    HostsDisplayExec.Clear();
                    CmsDisplayExec.Clear();
                    if (context.EnvironmentHosts.Any(h => h.HostType == HostTypes.EXEC))
                    {
                        foreach (VibesHost host in context.EnvironmentHosts.Where(h => h.HostType == HostTypes.EXEC).OrderBy(h => h.Name))
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
                LogAndReportException(ex, $"Error loading VIBES host's from DB: {ex.Message}", true);
            }
        }

        #endregion

        #region GUI Bound

        /// <summary>
        /// Swaps configured search/replace terms for all applicable properties of all CM's for the selected host
        /// </summary>
        /// <param name="target">The host to swap CM's on</param>
        internal sealed override void SwapPropertyTargets(object target)
        {
            try
            {
                switch (target)
                {
                    case HostTypes.EXEC:
                        foreach (VibesCm cm in CmsDisplayExec)
                        {
                            foreach (DeploymentProperty property in cm.DeploymentProperties)
                            {
                                string temp = property.SearchPattern;
                                property.SearchPattern = property.ReplacePattern;
                                property.ReplacePattern = temp;
                            }
                        }
                        break;
                    case HostTypes.OPERDB:
                        foreach (VibesCm cm in CmsDisplayOperDb)
                        {
                            foreach (DeploymentProperty property in cm.DeploymentProperties)
                            {
                                string temp = property.SearchPattern;
                                property.SearchPattern = property.ReplacePattern;
                                property.ReplacePattern = temp;
                            }
                        }
                        break;
                    case HostTypes.OPERAPP1:
                        foreach (VibesCm cm in CmsDisplayOperAppOne)
                        {
                            foreach (DeploymentProperty property in cm.DeploymentProperties)
                            {
                                string temp = property.SearchPattern;
                                property.SearchPattern = property.ReplacePattern;
                                property.ReplacePattern = temp;
                            }
                        }
                        break;
                    case HostTypes.OPERAPP2:
                        foreach (VibesCm cm in CmsDisplayOperAppTwo)
                        {
                            foreach (DeploymentProperty property in cm.DeploymentProperties)
                            {
                                string temp = property.SearchPattern;
                                property.SearchPattern = property.ReplacePattern;
                                property.ReplacePattern = temp;
                            }
                        }
                        break;
                    case HostTypes.MS:
                        foreach (VibesCm cm in CmsDisplayMs)
                        {
                            foreach (DeploymentProperty property in cm.DeploymentProperties)
                            {
                                string temp = property.SearchPattern;
                                property.SearchPattern = property.ReplacePattern;
                                property.ReplacePattern = temp;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogAndReportException(ex, $"Error swapping property terms: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Pre-populated search/replace terms for all applicable properties of all CM's for the selected host
        /// </summary>
        /// <param name="target">The host to pre-populate CM's on</param>
        internal sealed override void PrePopulateTargets(object target)
        {
            try
            {
                switch (target)
                {
                    case HostTypes.EXEC:
                        foreach (VibesCm cm in CmsDisplayExec)
                        {
                            foreach (DeploymentProperty property in cm.DeploymentProperties)
                            {
                                if (property.PropertyValue.Contains("cm-bpi11"))
                                {
                                    property.SearchPattern = "cm-bpi11";
                                    property.ReplacePattern = "cm-bpi11.hlcvibes.yvr.com";
                                }
                            }
                        }
                        break;
                    case HostTypes.OPERDB:
                        foreach (VibesCm cm in CmsDisplayOperDb)
                        {
                            foreach (DeploymentProperty property in cm.DeploymentProperties)
                            {
                                if (property.PropertyValue.Contains("cm-bpi11"))
                                {
                                    property.SearchPattern = "cm-bpi11";
                                    property.ReplacePattern = "cm-bpi11.hlcvibes.yvr.com";
                                }
                            }
                        }
                        break;
                    case HostTypes.OPERAPP1:
                        foreach (VibesCm cm in CmsDisplayOperAppOne)
                        {
                            foreach (DeploymentProperty property in cm.DeploymentProperties)
                            {
                                if (property.PropertyValue.Contains("cm-bpi11"))
                                {
                                    property.SearchPattern = "cm-bpi11";
                                    property.ReplacePattern = "cm-bpi11.hlcvibes.yvr.com";
                                }
                            }
                        }
                        break;
                    case HostTypes.OPERAPP2:
                        foreach (VibesCm cm in CmsDisplayOperAppTwo)
                        {
                            foreach (DeploymentProperty property in cm.DeploymentProperties)
                            {
                                if (property.PropertyValue.Contains("cm-bpi11"))
                                {
                                    property.SearchPattern = "cm-bpi11";
                                    property.ReplacePattern = "cm-bpi11.hlcvibes.yvr.com";
                                }
                            }
                        }
                        break;
                    case HostTypes.MS:
                        foreach (VibesCm cm in CmsDisplayMs)
                        {
                            foreach (DeploymentProperty property in cm.DeploymentProperties)
                            {
                                if (property.PropertyValue.Contains("cm-bpi11"))
                                {
                                    property.SearchPattern = "cm-bpi11";
                                    property.ReplacePattern = "cm-bpi11.hlcvibes.yvr.com";
                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogAndReportException(ex, $"Error swapping property terms: {ex.Message}", false);
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
                if (CmsDisplayExec.Any(c => c.Id == e.Cm.Id))
                {
                    CmsDisplayExec.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayOperDb.Any(c => c.Id == e.Cm.Id))
                {
                    CmsDisplayOperDb.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayOperAppOne.Any(c => c.Id == e.Cm.Id))
                {
                    CmsDisplayOperAppOne.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayOperAppTwo.Any(c => c.Id == e.Cm.Id))
                {
                    CmsDisplayOperAppTwo.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayMs.Any(c => c.Id == e.Cm.Id))
                {
                    CmsDisplayMs.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
            }
            catch (Exception ex)
            {
                LogAndReportException(ex, $"Error updating GUI with CM changes: {ex.Message}", false);
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
                    switch (e.CmStatus)
                    {
                        case HttpStatusCode.OK:
                            CmsDisplayExec.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Alive;
                            break;
                        case HttpStatusCode.ServiceUnavailable:
                            CmsDisplayExec.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Offline;
                            break;
                        case HttpStatusCode.NoContent:
                            CmsDisplayExec.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Altered;
                            break;
                        default:
                            CmsDisplayExec.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Unchecked;
                            PollCmAsync(CmsDisplayExec.Single(c => c.Id == e.CmChanged.Id));
                            break;
                    }
                }
                else if (CmsDisplayOperDb.Contains(e.CmChanged))
                {
                    switch (e.CmStatus)
                    {
                        case HttpStatusCode.OK:
                            CmsDisplayOperDb.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Alive;
                            break;
                        case HttpStatusCode.ServiceUnavailable:
                            CmsDisplayOperDb.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Offline;
                            break;
                        case HttpStatusCode.NoContent:
                            CmsDisplayOperDb.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Altered;
                            break;
                        default:
                            CmsDisplayOperDb.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Unchecked;
                            PollCmAsync(CmsDisplayOperDb.Single(c => c.Id == e.CmChanged.Id));
                            break;
                    }
                }
                else if (CmsDisplayOperAppOne.Contains(e.CmChanged))
                {
                    switch (e.CmStatus)
                    {
                        case HttpStatusCode.OK:
                            CmsDisplayOperAppOne.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Alive;
                            break;
                        case HttpStatusCode.ServiceUnavailable:
                            CmsDisplayOperAppOne.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Offline;
                            break;
                        case HttpStatusCode.NoContent:
                            CmsDisplayOperAppOne.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Altered;
                            break;
                        default:
                            CmsDisplayOperAppOne.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Unchecked;
                            PollCmAsync(CmsDisplayOperAppOne.Single(c => c.Id == e.CmChanged.Id));
                            break;
                    }
                }
                else if (CmsDisplayOperAppTwo.Contains(e.CmChanged))
                {
                    switch (e.CmStatus)
                    {
                        case HttpStatusCode.OK:
                            CmsDisplayOperAppTwo.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Alive;
                            break;
                        case HttpStatusCode.ServiceUnavailable:
                            CmsDisplayOperAppTwo.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Offline;
                            break;
                        case HttpStatusCode.NoContent:
                            CmsDisplayOperAppTwo.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Altered;
                            break;
                        default:
                            CmsDisplayOperAppTwo.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Unchecked;
                            PollCmAsync(CmsDisplayOperAppTwo.Single(c => c.Id == e.CmChanged.Id));
                            break;
                    }
                }
                else if (CmsDisplayMs.Contains(e.CmChanged))
                {
                    switch (e.CmStatus)
                    {
                        case HttpStatusCode.OK:
                            CmsDisplayMs.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Alive;
                            break;
                        case HttpStatusCode.ServiceUnavailable:
                            CmsDisplayMs.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Offline;
                            break;
                        case HttpStatusCode.NoContent:
                            CmsDisplayMs.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Altered;
                            break;
                        default:
                            CmsDisplayMs.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Unchecked;
                            PollCmAsync(CmsDisplayMs.Single(c => c.Id == e.CmChanged.Id));
                            break;
                    }
                }

                // Update properties
                if (e.DeploymentProperties != null && (e.Host.HostType == HostTypes.EXEC || e.Host.HostType == HostTypes.OPERDB || e.Host.HostType == HostTypes.OPERAPP1 || e.Host.HostType == HostTypes.OPERAPP2 || e.Host.HostType == HostTypes.MS))
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
                LogAndReportException(ex, $"Error updating GUI with CM changes: {ex.Message}", false);
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
        internal override sealed (VibesHost, VibesCm, ObservableCollection<VibesCm>) SetTargets(object target)
        {
            VibesHost hostToPoll = null;
            VibesCm cmToPoll = null;
            ObservableCollection<VibesCm> cms = null;

            switch (target)
            {
                case HostTypes.EXEC:
                    CheckSingleParameters(SelectedHostExec, SelectedCmExec);

                    hostToPoll = SelectedHostExec;
                    cmToPoll = SelectedCmExec;
                    cms = CmsDisplayExec;
                    break;
                case HostTypes.OPERDB:
                    CheckSingleParameters(SelectedHostOperDb, SelectedCmOperDb);

                    hostToPoll = SelectedHostOperDb;
                    cmToPoll = SelectedCmOperDb;
                    cms = CmsDisplayOperDb;
                    break;
                case HostTypes.OPERAPP1:
                    CheckSingleParameters(SelectedHostOperAppOne, SelectedCmOperAppOne);

                    hostToPoll = SelectedHostOperAppOne;
                    cmToPoll = SelectedCmOperAppOne;
                    cms = CmsDisplayOperAppOne;
                    break;
                case HostTypes.OPERAPP2:
                    CheckSingleParameters(SelectedHostOperAppTwo, SelectedCmOperAppTwo);

                    hostToPoll = SelectedHostOperAppTwo;
                    cmToPoll = SelectedCmOperAppTwo;
                    cms = CmsDisplayOperAppTwo;
                    break;
                case HostTypes.MS:
                    CheckSingleParameters(SelectedHostMs, SelectedCmMs);

                    hostToPoll = SelectedHostMs;
                    cmToPoll = SelectedCmMs;
                    cms = CmsDisplayMs;
                    break;
            }

            return (hostToPoll, cmToPoll, cms);
        }

        /// <summary>
        /// Sets up up Host target and checks for missing params
        /// Common code used by all CM commands, but specific to this host
        /// </summary>
        /// <param name="target">The HostType to target</param>
        /// <returns>The target Host/Cm, validated</returns>
        internal override sealed VibesHost SetTargetHost(object target)
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
                case HostTypes.MS:
                    CheckHostParameters(SelectedHostMs);
                    hostToPoll = SelectedHostMs;
                    break;
            }

            return hostToPoll;
        }

        #endregion

        #endregion
    }
}
