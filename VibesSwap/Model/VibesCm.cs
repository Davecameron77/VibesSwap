using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using VibesSwap.Model.Dimensional;

namespace VibesSwap.Model
{
    /// <summary>
    /// Models a VibesCm
    /// </summary>
    class VibesCm : INotifyPropertyChanged
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
    }
}
