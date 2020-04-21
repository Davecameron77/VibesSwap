using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using VibesSwap.Model.Dimensional;

namespace VibesSwap.Model
{
    /// <summary>
    /// Models a VIBES host
    /// </summary>
    class VibesHost : INotifyPropertyChanged
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public bool IndClustered { get; set; }
        public string SshUsername { get; set; }
        public string SshPassword { get; set; }
        public HostTypes HostType { get; set; }
        public List<VibesCm> VibesCms { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
