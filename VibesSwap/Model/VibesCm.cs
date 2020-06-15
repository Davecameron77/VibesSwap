using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using VibesSwap.Model.Dimensional;

namespace VibesSwap.Model
{
    /// <summary>
    /// Models a VibesCm
    /// </summary>
    public class VibesCm : INotifyPropertyChanged
    {
        #region Constructors

        public VibesCm()
        {
            DeploymentProperties = new List<DeploymentProperty>();
        }

        #endregion

        #region Members

        [Key]
        public int Id { get; set; }
        // CM Attributes
        public string CmResourceName { get; set; }
        public string CmPath { get; set; }
        public string CmCorePath { get; set; }
        public string CmPort { get; set; }
        public CmTypes CmType { get; set; }
        public CmStates CmStatus { get; set; } = CmStates.Unchecked;
        public ICollection<DeploymentProperty> DeploymentProperties { get; set; }
        // Relations
        public int VibesHostId { get; set; }
        public VibesHost VibesHost { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        public VibesCm DeepCopy()
        {
            VibesCm newCm = new VibesCm
            {
                Id = Id,
                CmResourceName = CmResourceName ?? "",
                CmPath = CmPath ?? "",
                CmCorePath = CmCorePath ?? "",
                CmPort = CmPort ?? "",
                CmStatus = CmStatus,
                CmType = CmType,
                VibesHost = VibesHost,
                VibesHostId = VibesHostId
            };
            if (DeploymentProperties != null && DeploymentProperties.Any())
            {
                foreach (DeploymentProperty prop in DeploymentProperties)
                {
                    newCm.DeploymentProperties.Add(prop);
                }
            }
            return newCm;
        }

        #endregion
    }
}
