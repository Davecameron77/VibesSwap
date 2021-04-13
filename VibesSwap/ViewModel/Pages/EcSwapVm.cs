using Microsoft.EntityFrameworkCore;
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
    /// <summary>
    /// VM for CM EC Swap view
    /// VM logic here is only for appliation specific tasks
    /// All HTTP/SSH calls go via CmControlBase
    /// </summary>
    class EcSwapVm : CmControlBase
    {
        #region Constructors

        public EcSwapVm()
        {
            CmsDisplayCommOne = new ObservableCollection<VibesCm>();
            CmsDisplayCommTwo = new ObservableCollection<VibesCm>();

            RefreshCommand = new RelayCommand(LoadData);
            UpdatePropertiesCommand = new RelayCommand(GetProperties);
            GetAllPropsCommand = new RelayCommand(GetAllProperties);
            PrePopulateTermsCommand = new RelayCommand(PrePopulateTargets);
            SwitchTermsCommand = new RelayCommand(SwapPropertyTargets);

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
        public RelayCommand GetAllPropsCommand { get; set; }

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
                if (parameter is VibesCm cmChanged)
                {
                    using (DataContext context = new DataContext())
                    {
                        VibesCm newCm = context.HostCms.Where(c => c.Id == cmChanged.Id).Include(c => c.DeploymentProperties).Include(c => c.VibesHost).FirstOrDefault().DeepCopy();
                        newCm.CmStatus = CmStates.Updated;

                        if (CmsDisplayCommOne.Contains(cmChanged))
                        {
                            
                            CmsDisplayCommOne.Remove(CmsDisplayCommOne.Single(c => c.Id == cmChanged.Id));
                            CmsDisplayCommOne.Add(newCm);
                            SelectedCmCommOne = newCm;

                        }
                        if (CmsDisplayCommTwo.Contains(cmChanged))
                        {
                            CmsDisplayCommTwo.Remove(CmsDisplayCommTwo.Single(c => c.Id == cmChanged.Id));
                            CmsDisplayCommTwo.Add(newCm);
                            SelectedCmCommTwo = newCm;
                        }
                        return;
                    }
                }

                // Comm1
                CmsDisplayCommOne.Clear();
                LoadCmForSwap(CmsDisplayCommOne, HostTypes.COMM1);
                SelectedCmCommOne = CmsDisplayCommOne.FirstOrDefault();
                // Comm2
                CmsDisplayCommTwo.Clear();
                LoadCmForSwap(CmsDisplayCommTwo, HostTypes.COMM2);
                SelectedCmCommTwo = CmsDisplayCommTwo.FirstOrDefault();
            }
            catch (Exception ex)
            {
                LogAndReportException(ex, "Error loading data to GUI", true);
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
            switch (target)
            {
                case HostTypes.COMM1:
                    foreach (VibesCm cm in CmsDisplayCommOne)
                    {
                        foreach (DeploymentProperty property in cm.DeploymentProperties)
                        {
                            string temp = property.SearchPattern;
                            property.SearchPattern = property.ReplacePattern;
                            property.ReplacePattern = temp;
                        }
                    }
                    break;
                case HostTypes.COMM2:
                    foreach (VibesCm cm in CmsDisplayCommTwo)
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

        /// <summary>
        /// Pre-populated search/replace terms for all applicable properties of all CM's for the selected host
        /// </summary>
        /// <param name="target">The host to pre-populate CM's on</param>
        internal sealed override void PrePopulateTargets(object target)
        {
            switch (target)
            {
                case HostTypes.COMM1:
                    foreach (VibesCm cm in CmsDisplayCommOne)
                    {
                        foreach (DeploymentProperty property in cm.DeploymentProperties)
                        {
                            if (property.PropertyValue.Contains("cm-lm11"))
                            {
                                property.SearchPattern = "cm-lm11.yvr.com";
                                property.ReplacePattern = "cm-lm11.hlcvibes.yvr.com";
                            }
                        }
                    }
                    break;
                case HostTypes.COMM2:
                    foreach (VibesCm cm in CmsDisplayCommTwo)
                    {
                        foreach (DeploymentProperty property in cm.DeploymentProperties)
                        {
                            if (property.PropertyValue.Contains("cm-lm11"))
                            {
                                property.SearchPattern = "cm-lm11.yvr.com";
                                property.ReplacePattern = "cm-lm11.hlcvibes.yvr.com";
                            }
                        }
                    }
                    break;
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
                if (CmsDisplayCommOne.Any(c => c.Id == e.Cm.Id))
                {
                    CmsDisplayCommOne.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                if (CmsDisplayCommTwo.Any(c => c.Id == e.Cm.Id))
                {
                    CmsDisplayCommTwo.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
            }
            catch (Exception ex)
            {
                LogAndReportException(ex, $"Error updating EC Swap GUI with HTTP status: {ex.Message}", false);
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
                    switch (e.CmStatus)
                    {
                        case HttpStatusCode.OK:
                            CmsDisplayCommOne.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Alive;
                            break;
                        case HttpStatusCode.ServiceUnavailable:
                            CmsDisplayCommOne.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Offline;
                            break;
                        case HttpStatusCode.NoContent:
                            CmsDisplayCommOne.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Altered;
                            break;
                        case HttpStatusCode.Continue:
                            CmsDisplayCommOne.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Updated;
                            break;
                        default:
                            CmsDisplayCommOne.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Unchecked;
                            PollCmAsync(CmsDisplayCommOne.Single(c => c.Id == e.CmChanged.Id));
                            break;
                    }
                }
                else if (CmsDisplayCommTwo.Contains(e.CmChanged))
                {
                    switch (e.CmStatus)
                    {
                        case HttpStatusCode.OK:
                            CmsDisplayCommTwo.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Alive;
                            break;
                        case HttpStatusCode.ServiceUnavailable:
                            CmsDisplayCommTwo.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Offline;
                            break;
                        case HttpStatusCode.NoContent:
                            CmsDisplayCommTwo.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Altered;
                            break;
                        case HttpStatusCode.Continue:
                            CmsDisplayCommTwo.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Updated;
                            break;
                        default:
                            CmsDisplayCommTwo.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Unchecked;
                            PollCmAsync(CmsDisplayCommTwo.Single(c => c.Id == e.CmChanged.Id));
                            break;
                    }
                }

                // Update properties
                if (e.DeploymentProperties != null && (e.Host.HostType == HostTypes.COMM1 || e.Host.HostType == HostTypes.COMM2))
                {
                    StoreDeploymentProperties(e.CmChanged, e.DeploymentProperties);
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        LoadData(e.CmChanged);
                    });
                }

                // Popup hosts
                if (e.HostsFile != null && e.Host != null && (e.Host.HostType == HostTypes.COMM1 || e.Host.HostType == HostTypes.COMM2))
                {
                    PopupHostsFile(e);
                }
            }
            catch (Exception ex)
            {
                LogAndReportException(ex, $"Error updating EC Swap GUI with CM changes: {ex.Message}", false);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Setups up Host/CM target and checks for missing params
        /// Common code used by all CM commands, but spefcific to this host
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
                case HostTypes.COMM1:
                    CheckSingleParameters(SelectedHostCommOne, SelectedCmCommOne);

                    hostToPoll = SelectedHostCommOne;
                    cmToPoll = SelectedCmCommOne;
                    cms = CmsDisplayCommOne;
                    break;
                case HostTypes.COMM2:
                    CheckSingleParameters(SelectedHostCommTwo, SelectedCmCommTwo);

                    hostToPoll = SelectedHostCommTwo;
                    cmToPoll = SelectedCmCommTwo;
                    cms = CmsDisplayCommTwo;
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
                case HostTypes.COMM1:
                    CheckHostParameters(SelectedHostCommOne);

                    hostToPoll = SelectedHostCommOne;
                    break;
                case HostTypes.COMM2:
                    CheckHostParameters(SelectedHostCommTwo);

                    hostToPoll = SelectedHostCommTwo;
                    break;
            }

            return hostToPoll;
        }

        #endregion

        #endregion
    }
}
