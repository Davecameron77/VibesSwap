using Domain;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RemoteAccess
{
    /// <summary>
    /// Helper class for HTTP commands relating to CM status
    /// Singleton, not for instantiation
    /// </summary>
    public static class CmHttpHelper
    {
        #region Constructors

        // Nil

        #endregion

        #region Properties

        /// <summary>
        /// Delegate for PollComplete event
        /// </summary>
        /// <param name="sender">Null, not used from static class</param>
        /// <param name="e">CmHelperEventArgs to be passed to subscriber</param>
        public delegate void PollCompleteEventHandler(object sender, CmHelperEventArgs e);
        /// <summary>
        /// Event to be fired when CheckCmStatus is completed
        /// </summary>
        public static event PollCompleteEventHandler PollComplete;

        #endregion

        #region Methods

        /// <summary>
        /// Polls CM for current status via call to http://host:port/status page
        /// </summary>
        /// <param name="hostToCheck">The host on which the CM is installed</param>
        /// <param name="cmToCheck">The CM to poll</param>
        /// <returns>Task<HttpStatusCode> containing CM status, though this is backup functionality, expected use is the event PollComplete</returns>
        public static async Task<HttpStatusCode> CheckCmStatus(VibesHost hostToCheck, VibesCm cmToCheck, int hashCode)
        {
            HttpStatusCode statusCode = HttpStatusCode.ServiceUnavailable;
            int port = 80;

            if (hostToCheck == null || cmToCheck == null)
            {
                string missingParameter = hostToCheck == null ? "Selected Host" : "Selected CM";
                throw new Exception($"Error starting/stopping/editing CM, Missing parameter {missingParameter}");
            }

            int.TryParse(cmToCheck.CmPort, out port);
            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(500) })
            {
                var builder = new UriBuilder("http", hostToCheck.Url, port)
                {
                    Path = "status"
                };
                Uri uri = builder.Uri;

                var response = await client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseBody) && responseBody.Contains("CM = Alive!"))
                    {
                        statusCode = HttpStatusCode.OK;
                    }
                }
                else
                {
                    statusCode = HttpStatusCode.ServiceUnavailable;
                }
            }

            OnPollComplete(cmToCheck, statusCode, hashCode);
            return statusCode;
        }

        /// <summary>
        /// Raises PollComplete event
        /// </summary>
        /// <param name="cmPolled">The CM which was polled</param>
        /// <param name="cmStatus">The status of the CM polled</param>
        public static void OnPollComplete(VibesCm cmPolled, HttpStatusCode cmStatus, int hashCode)
        {
            PollComplete?.Invoke(null, new CmHelperEventArgs { Cm = cmPolled, CmStatus = cmStatus, SubscriberHashCode = hashCode });
        }

        #endregion
    }
}