using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibesSwap.Model.Dimensional
{
    class VibesSwapSshCommand
    {
        public VibesHost Host { get; set; }
        public VibesCm Cm { get; set; }
        public string SshCommand { get; set; }
    }
}
