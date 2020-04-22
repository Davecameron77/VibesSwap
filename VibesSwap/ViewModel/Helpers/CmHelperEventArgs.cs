using System;
using System.Net;
using VibesSwap.Model;

namespace VibesSwap.ViewModel.Helpers
{
    /// <summary>
    /// Event args for passing back and forth with CM helper classes
    /// </summary>
    public class CmHelperEventArgs : EventArgs
    {
        #region Members

        /// <summary>
        /// The CM queried but not changed
        /// </summary>
        public VibesCm Cm { get; set; }
        /// <summary>
        /// The CM which was changed via CmSshHelper
        /// </summary>
        public VibesCm CmChanged { get; set; }
        /// <summary>
        /// Deployment properties fetched via SSH
        /// </summary>
        public string DeploymentProperties { get; set; }
        /// <summary>
        /// The host queried but not changed
        /// </summary>
        public VibesHost Host { get; set; }
        /// <summary>
        /// Host file fetched via ssh
        /// </summary>
        public string HostsFile { get; set; }
        /// <summary>
        /// The HTTP status of the CM polled/changed
        /// </summary>
        public HttpStatusCode CmStatus { get; set; }
        /// <summary>
        /// The hashcode of the subscriber
        /// </summary>
        public int SubscriberHashCode { get; set; }


        #endregion
    }
}
