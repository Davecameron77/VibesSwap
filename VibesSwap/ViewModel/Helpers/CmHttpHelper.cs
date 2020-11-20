using Serilog;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using VibesSwap.Model;

namespace VibesSwap.ViewModel.Helpers
{
    /// <summary>
    /// Helper class for HTTP commands relating to CM status
    /// Singleton, not for instantiation
    /// </summary>
    public static class CmHttpHelper
    {
        #region Constructors

        static CmHttpHelper()
        {
            StaticHttpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(3000) };
        }

        #endregion

        #region Properties

        private static HttpClient StaticHttpClient { get; }
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
            try
            {
                if (hostToCheck == null || cmToCheck == null)
                {
                    string missingParameter = hostToCheck == null ? "Selected Host" : "Selected CM";
                    throw new Exception($"Error polling CM, Missing parameter {missingParameter}");
                }

                int.TryParse(cmToCheck.CmPort, out int port);

                var builder = new UriBuilder("http", hostToCheck.Url, port)
                {
                    Path = "status"
                };
                Uri uri = builder.Uri;

                var response = await StaticHttpClient.GetAsync(uri);
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
            catch (Exception ex)
            {
                // HTTP timeout, CM offline
                if (ex.Message.Contains("A task was canceled."))
                {
                    Log.Information($"HTTP error: Request to {cmToCheck.CmResourceName} timed out");
                    statusCode = HttpStatusCode.RequestTimeout;
                    OnPollComplete(cmToCheck, statusCode, hashCode);
                    return statusCode;
                }
                // Unkonwn Host
                else if (ex.InnerException.ToString().Contains("The remote name could not be resolved"))
                {
                    Log.Information($"HTTP error: Unable to resolve hostname {cmToCheck.VibesHost.Url} for CM {cmToCheck.CmResourceName}");
                    statusCode = HttpStatusCode.NotFound;
                    OnPollComplete(cmToCheck, statusCode, hashCode);
                    return statusCode;
                }
                // Other error
                else if (ex.InnerException.ToString().Contains("Unable to connect to the remote server"))
                {
                    Log.Information($"CM {cmToCheck.CmResourceName} on {cmToCheck.VibesHost.Url} is reporting as offline");
                    statusCode = HttpStatusCode.ServiceUnavailable;
                    OnPollComplete(cmToCheck, statusCode, hashCode);
                    return statusCode;
                }

                Log.Error($"Error polling CM: {ex.Message}");
                Log.Error($"Stack Trace: {ex.StackTrace}");
                OnPollComplete(cmToCheck, statusCode, hashCode);
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