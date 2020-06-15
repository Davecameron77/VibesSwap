using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VibesSwap.Model.Dimensional
{
    /// <summary>
    /// Models key-value pairs for deployment properties
    /// </summary>
    public class DeploymentProperty : INotifyPropertyChanged
    {
        [Key]
        public int Id { get; set; }
        public string PropertyKey { get; set; }
        public string PropertyValue { get; set; }
        public string SearchPattern { get; set; }
        public string ReplacePattern { get; set; }
        public VibesCm Cm { get; set; }
        public int CmId { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
