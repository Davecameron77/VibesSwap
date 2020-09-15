using Microsoft.EntityFrameworkCore;
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
            UpdatePropertiesCommand = new RelayCommand(GetProperties);
            StartCmCommand = new RelayCommand(StartCm);
            StopCmCommand = new RelayCommand(StopCm);
            PollCmCommand = new RelayCommand(PollCm);
            SwapCmCommand = new RelayCommand(SwapCm);
            StartAllCommand = new RelayCommand(StartAll);
            StopAllCommand = new RelayCommand(StopAll);
            SwapAllCommand = new RelayCommand(SwapAll);
            PollAllCommand = new RelayCommand(PollAll);
            SwitchTermsCommand = new RelayCommand(SwapPropertyTargets);
            PrePopulateTermsCommand = new RelayCommand(PrePopulateTargets);

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
                if (parameter is VibesCm cmChanged)
                {
                    using (DataContext context = new DataContext())
                    {
                        VibesCm newCm = context.HostCms.Where(c => c.Id == cmChanged.Id).Include(c => c.DeploymentProperties).Include(c => c.VibesHost).FirstOrDefault().DeepCopy();

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
                Log.Error($"Error loading CM EC's from DB: {ex.Message}");
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
                    case HostTypes.COMM1:
                        StartCollection(CmsDisplayCommOne, SelectedHostCommOne);
                        break;
                    case HostTypes.COMM2:
                        StartCollection(CmsDisplayCommTwo, SelectedHostCommTwo);
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
                    case HostTypes.COMM1:
                        StopCollection(CmsDisplayCommOne, SelectedHostCommOne);
                        break;
                    case HostTypes.COMM2:
                        StopCollection(CmsDisplayCommTwo, SelectedHostCommTwo);
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
                    case HostTypes.COMM1:
                        PollCollection(CmsDisplayCommOne, SelectedHostCommOne);
                        break;
                    case HostTypes.COMM2:
                        PollCollection(CmsDisplayCommTwo, SelectedHostCommTwo);
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
                    case HostTypes.COMM1:
                        SwapCollection(CmsDisplayCommOne, SelectedHostCommOne);
                        break;
                    case HostTypes.COMM2:
                        SwapCollection(CmsDisplayCommTwo, SelectedHostCommTwo);
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
                if (CmsDisplayCommOne.Contains(e.Cm))
                {
                    CmsDisplayCommOne.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
                else if (CmsDisplayCommTwo.Contains(e.Cm))
                {
                    CmsDisplayCommTwo.Single(c => c.Id == e.Cm.Id).CmStatus = e.CmStatus == HttpStatusCode.OK ? CmStates.Alive : CmStates.Offline;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error updating EC Swap GUI with HTTP status: {ex.Message}");
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
                    switch (e.CmStatus)
                    {
                        case HttpStatusCode.OK:
                            CmsDisplayCommOne.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Alive;
                            break;
                        case HttpStatusCode.ServiceUnavailable:
                            CmsDisplayCommOne.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Offline;
                            break;
                        default:
                            CmsDisplayCommOne.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Unchecked;
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
                        default:
                            CmsDisplayCommTwo.Single(c => c.Id == e.CmChanged.Id).CmStatus = CmStates.Unchecked;
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
                if (e.Host != null && (e.Host.HostType == HostTypes.COMM1 || e.Host.HostType == HostTypes.COMM2))
                {
                    PopupHostsFile(e);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error updating EC Swap GUI with CM changes: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
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
        internal override sealed (VibesHost, VibesCm) SetTargets(object target)
        {
            try
            {
                VibesHost hostToPoll = null;
                VibesCm cmToPoll = null;

                switch (target)
                {
                    case HostTypes.COMM1:
                        CheckSingleParameters(SelectedHostCommOne, SelectedCmCommOne);
                        
                        hostToPoll = SelectedHostCommOne;
                        cmToPoll = SelectedCmCommOne;
                        break;
                    case HostTypes.COMM2:
                        CheckSingleParameters(SelectedHostCommTwo, SelectedCmCommTwo);
                        
                        hostToPoll = SelectedHostCommTwo;
                        cmToPoll = SelectedCmCommTwo;
                        break;
                }

                return (hostToPoll, cmToPoll);
            }
            catch (Exception ex)
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
            catch (Exception ex)
            {
                Log.Error($"Error setting parameters for Host command, {ex.Message}");
                Log.Error($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }

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
            catch (Exception ex)
            {
                Log.Error($"Error swapping property terms, {ex.Message}");
                Log.Error($"StackTrace: {ex.StackTrace}");
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
                    case HostTypes.COMM1:
                        foreach (VibesCm cm in CmsDisplayCommOne)
                        {
                            foreach (DeploymentProperty property in cm.DeploymentProperties)
                            {
                                if (property.PropertyValue.Contains("cm-lm11"))
                                {
                                    property.SearchPattern = "cm-lm11";
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
                                    property.SearchPattern = "cm-lm11";
                                    property.ReplacePattern = "cm-lm11.hlcvibes.yvr.com";
                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error swapping property terms, {ex.Message}");
                Log.Error($"StackTrace: {ex.StackTrace}");
            }
        }

        #endregion

        #endregion
    }
}
