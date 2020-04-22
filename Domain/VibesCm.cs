using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Domain
{
    /// <summary>
    /// Models a VibesCm
    /// </summary>
    public class VibesCm : INotifyPropertyChanged
    {
        [Key]
        public int Id { get; set; }
        public string CmResourceName { get; set; }
        public string CmPath { get; set; }
        public string CmCorePath { get; set; }
        public string CmPort { get; set; }
        public CmTypes CmType { get; set; }
        public List<DeploymentProperty> DeploymentProperties { get; set; }
        public int VibesHostId { get; set; }
        public VibesHost VibesHost { get; set; }
        public CmStates CmStatus { get; set; } = CmStates.Unchecked;

        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
