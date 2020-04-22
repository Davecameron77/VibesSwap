using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Domain
{
    /// <summary>
    /// Models a VIBES host
    /// </summary>
    public class VibesHost : INotifyPropertyChanged
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

        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
