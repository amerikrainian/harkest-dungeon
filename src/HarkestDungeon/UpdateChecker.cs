using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace DD2A11y {
    /// <summary>
    /// Fetches the newest GitHub release on a thread-pool thread and holds the answer for the
    /// pump to announce - the request never touches the main thread. Only a release strictly
    /// newer than the running build ever surfaces; up to date, ahead of the release (a dev
    /// build), offline, or rate-limited all stay spoken-silent with a log line.
    /// </summary>
    public sealed class UpdateChecker {
        private const string ApiUrl =
            "https://api.github.com/repos/amerikrainian/harkest-dungeon/releases/latest";

        private volatile string _newerVersion;

        /// <summary>The version to announce, set once the background request found a release
        /// strictly newer than the running build; null before that, and forever when none is.</summary>
        public string NewerVersion => _newerVersion;

        public void Start(string local) {
            Task.Run(() => {
                try {
                    // Older Mono profiles default to TLS 1.0, which the GitHub API refuses.
                    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                    var request = (HttpWebRequest)WebRequest.Create(ApiUrl);
                    request.UserAgent = "HarkestDungeon-mod";
                    request.Accept = "application/vnd.github+json";
                    request.Timeout = 10000;
                    request.ReadWriteTimeout = 10000;
                    string json;
                    using (var response = request.GetResponse())
                    using (var reader = new StreamReader(response.GetResponseStream())) {
                        json = reader.ReadToEnd();
                    }
                    string remote = Core.UpdateCheck.LatestVersion(json);
                    if (remote == null) {
                        Plugin.Log.LogWarning("update check: release payload named no version");
                        return;
                    }
                    if (Core.UpdateCheck.IsNewer(remote, local)) {
                        Plugin.Log.LogInfo("update check: " + remote + " available (running " + local + ")");
                        _newerVersion = remote;
                    } else {
                        Plugin.Log.LogInfo("update check: up to date (latest " + remote + ", running " + local + ")");
                    }
                } catch (Exception ex) {
                    Plugin.Log.LogWarning("update check: failed (" + ex.Message + ")");
                }
            });
        }
    }
}
