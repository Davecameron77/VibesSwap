using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using VibesSwap.ViewModel.Helpers;
using VibesSwap.Model;
using VibesSwap.Model.Dimensional;
using VibesSwap.ViewModel.Pages.Base;

namespace VibesSwap.ViewModel.Pages
{
    internal class SetupVm : VmBase
    {
        #region Constructors

        public SetupVm()
        {
            AddRemoveHostCommand = new RelayCommand(AddRemoveHost);
            AddRemoveCmCommand = new RelayCommand(AddRemoveCm);

            LoadDimensional();
            LoadData(GuiObjectTypes.VibesHost);
        }

        #endregion

        #region Members

        public ObservableCollection<VibesHost> DisplayHosts { get; set; }
        public ObservableCollection<VibesCm> DisplayCms { get; set; }
        public List<string> HostTypes { get; set; }
        public List<string> CmTypes { get; set; }

        private VibesHost _selectedHost;
        private VibesCm _selectedCm;
        private string _selectedHostTYpe;
        private string _selectedCmType;

        public VibesHost SelectedHost
        {
            get { return _selectedHost; }
            set
            {
                if (value != null)
                {
                    _selectedHost = value;
                    // Set HostType combobox
                    SelectedHostType = value.HostType.ToString();
                    // Load CM's
                    LoadData(GuiObjectTypes.VibesCm);
                    // Bind live save handler
                    SelectedHost.PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                }
            }
        }
        public string SelectedHostType
        {
            get { return _selectedHostTYpe; }
            set
            {
                if (value != null)
                {
                    _selectedHostTYpe = value;
                    // Set hosttype on selected host
                    SelectedHost.HostType = (HostTypes)Enum.Parse(typeof(HostTypes), value);
                }
            }
        }
        public VibesCm SelectedCm
        {
            get { return _selectedCm; }
            set
            {
                if (value != null)
                {
                    _selectedCm = value;
                    // Set CmType combobox
                    SelectedCmType = value.CmType.ToString();
                    // Bind live save handler
                    SelectedCm.PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                }
            }
        }
        public string SelectedCmType
        {
            get { return _selectedCmType; }
            set
            {
                if (value != null)
                {
                    _selectedCmType = value;
                    // Set cmtype on selected CM
                    SelectedCm.CmType = (CmTypes)Enum.Parse(typeof(CmTypes), value);
                }
            }
        }

        public bool CanEditHost { get; set; }
        public bool CanEditCm { get; set; }

        public RelayCommand AddRemoveHostCommand { get; set; }
        public RelayCommand AddRemoveCmCommand { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Loads dimensional data for GUI comboboxes
        /// </summary>
        private void LoadDimensional()
        {
            try
            {
                if (DisplayHosts == null)
                {
                    DisplayHosts = new ObservableCollection<VibesHost>();
                }
                if (DisplayCms == null)
                {
                    DisplayCms = new ObservableCollection<VibesCm>();
                }
                if (HostTypes == null)
                {
                    HostTypes = new List<string>();
                    foreach (var value in Enum.GetValues(typeof(HostTypes)))
                    {
                        HostTypes.Add(value.ToString());
                    }
                }
                if (CmTypes == null)
                {
                    CmTypes = new List<string>();
                    foreach (var value in Enum.GetValues(typeof(CmTypes)))
                    {
                        CmTypes.Add(value.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dimensional data: {ex.Message}");
                Log.Error($"Error loading dimensional data: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads data from database
        /// Calls LoadBoilerplate if no data is loaded
        /// </summary>
        /// <param name="type">The GuiObjectType to load</param>
        /// <param name="objectId">If an object is selected prior to refreshed, it's ID can be passed so it remains selected</param>
        private void LoadData(GuiObjectTypes type, int objectId = 0)
        {
            try
            {
                switch (type)
                {
                    case GuiObjectTypes.VibesHost:
                        using (DataContext context = new DataContext())
                        {
                            DisplayHosts.Clear();
                            // Nothing found, load boilerplate and terminate
                            if (!context.EnvironmentHosts.Any())
                            {
                                LoadBoilerPlate();
                                return;
                            }
                            // Load Hosts from DB
                            foreach (VibesHost host in context.EnvironmentHosts.OrderBy(h => h.Name))
                            {
                                VibesHost newHost = host.DeepCopy();
                                newHost.PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                                DisplayHosts.Add(newHost);
                            }
                            // Set GUI particulars and reload CM's
                            SelectedHost = objectId == 0 ? DisplayHosts.FirstOrDefault() : DisplayHosts.SingleOrDefault(h => h.Id == objectId);
                            CanEditHost = true;
                            LoadData(GuiObjectTypes.VibesCm);
                        }
                        break;
                    case GuiObjectTypes.VibesCm:
                        using (DataContext context = new DataContext())
                        {
                            DisplayCms.Clear();
                            // Nothing found, load boilerplate and terminate
                            if (!context.HostCms.Any(c => c.VibesHostId == SelectedHost.Id))
                            {
                                LoadBoilerPlate();
                                return;
                            }
                            // Load CMs from DB
                            DisplayCms.Clear();
                            foreach (VibesCm cm in context.HostCms.Where(c => c.VibesHostId == SelectedHost.Id).OrderBy(c => c.CmResourceName))
                            {
                                VibesCm newCm = cm.DeepCopy();
                                newCm.PropertyChanged += new PropertyChangedEventHandler(PersistTargetChanges);
                                DisplayCms.Add(newCm);
                            }
                            // Set GUI particulars
                            SelectedCm = objectId == 0 ? DisplayCms.FirstOrDefault() : DisplayCms.SingleOrDefault(c => c.Id == objectId);
                            CanEditCm = true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data to GUI: {ex.Message}");
                Log.Error($"Error loading data to GUI: {ex.Message}");
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
                    if (!DisplayHosts.Any())
                    {
                        DisplayHosts.Add(new VibesHost { Name = "No Hosts Loaded", Id = 0 });
                        SelectedHost = DisplayHosts.OrderByDescending(h => h.Id).FirstOrDefault();
                        CanEditHost = false;
                    }
                    if (!DisplayCms.Any())
                    {
                        DisplayCms.Add(new VibesCm { CmResourceName = "No CMs loaded", Id = 0 });
                        SelectedCm = DisplayCms.OrderByDescending(c => c.Id).FirstOrDefault();
                        CanEditCm = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading boilerplate data: {ex.Message}");
            }

        }

        /// <summary>
        /// Adds or removes a Host to GUI/DB
        /// </summary>
        /// <param name="parameter">Add or Remove</param>
        private void AddRemoveHost(object parameter)
        {
            try
            {
                switch (parameter)
                {
                    case GuiOperations.Add:
                        using (DataContext context = new DataContext())
                        {
                            context.Add(new VibesHost { Name = "Please enter a host name" });
                            if (context.SaveChanges() == 1)
                            {
                                CanEditHost = true;
                                LoadData(GuiObjectTypes.VibesHost, context.EnvironmentHosts.OrderByDescending(h => h.Id).FirstOrDefault().Id);
                                return;
                            }
                            MessageBox.Show("Error adding host", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        break;
                    case GuiOperations.Remove:
                        using (DataContext context = new DataContext())
                        {
                            if (SelectedHost == null)
                            {
                                MessageBox.Show("Please select a Host to remove", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                            context.EnvironmentHosts.Remove(context.EnvironmentHosts.SingleOrDefault(h => h.Id == SelectedHost.Id));
                            if (context.SaveChanges() == 1)
                            {
                                LoadData(GuiObjectTypes.VibesHost);
                                return;
                            }
                            MessageBox.Show("Error removing host", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error adding or removing host: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds or removes a CM to GUI/DB
        /// </summary>
        /// <param name="parameter">Add or Remove</param>
        private void AddRemoveCm(object parameter)
        {
            try
            {
                switch (parameter)
                {
                    case GuiOperations.Add:
                        if (SelectedHost == null)
                        {
                            MessageBox.Show("Please add a host first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        using (DataContext context = new DataContext())
                        {
                            context.Add(new VibesCm
                            {
                                CmResourceName = "Please enter a CM resource name",
                                VibesHost = context.EnvironmentHosts.SingleOrDefault(h => h.Id == SelectedHost.Id),
                                VibesHostId = context.EnvironmentHosts.SingleOrDefault(h => h.Id == SelectedHost.Id).Id
                            });
                            if (context.SaveChanges() == 1)
                            {
                                CanEditCm = true;
                                LoadData(GuiObjectTypes.VibesCm, context.HostCms.OrderByDescending(c => c.Id).FirstOrDefault().Id);
                                return;
                            }
                            MessageBox.Show("Error adding CM", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        break;
                    case GuiOperations.Remove:
                        using (DataContext context = new DataContext())
                        {
                            if (SelectedCm == null)
                            {
                                MessageBox.Show("Please select a CM to remove", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                            context.HostCms.Remove(context.HostCms.SingleOrDefault(c => c.Id == SelectedCm.Id));
                            if (context.SaveChanges() == 1)
                            {
                                LoadData(GuiObjectTypes.VibesCm);
                                return;
                            }
                            MessageBox.Show("Error removing CM", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error adding or removing host: {ex.Message}");
            }
        }

        #endregion
    }
}
