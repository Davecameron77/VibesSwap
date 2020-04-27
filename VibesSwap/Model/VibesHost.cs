using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using VibesSwap.Model.Dimensional;

namespace VibesSwap.Model
{
    /// <summary>
    /// Models a VIBES host
    /// </summary>
    public class VibesHost : INotifyPropertyChanged
    {
        public VibesHost()
        {
            VibesCms = new List<VibesCm>();
        }

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

        public VibesHost DeepCopy()
        {
            VibesHost newHost = new VibesHost
            {
                Id = Id,
                Name = Name ?? "",
                HostType = HostType,
                IndClustered = IndClustered,
                Url = Url ?? "",
                SshUsername = SshUsername ?? "",
                SshPassword = SshPassword ?? ""
            };
            if (VibesCms != null && VibesCms.Any())
            {
                foreach (VibesCm cm in VibesCms)
                {
                    newHost.VibesCms.Add(cm);
                }
            }
            return newHost;
        }
    }
}
