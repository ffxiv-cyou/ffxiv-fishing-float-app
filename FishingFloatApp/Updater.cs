using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace FishingFloatApp
{
    /// <summary>
    /// 更新检查器，基于 GitHub Release API 实现
    /// </summary>
    public class Updater
    {
        string url { get; }
        public Updater(string org, string repo)
        {
            url = $"https://api.github.com/repos/{org}/{repo}/releases";
        }

        public struct Release
        {
            public string url { get; set; }
            public string html_url { get; set; }
            public string tag_name { get; set; }
            public string name { get; set; }
            public bool draft { get; set; }
            public bool prerelease { get; set; }
            public string created_at { get; set; }
            public string updated_at { get; set; }
            public string publish_at { get; set; }
            public string body { get; set; }
        }

        async Task<Release[]> GetReleasesAsync()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("FishingFloatApp", "1.0"));
            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                return Array.Empty<Release>();

            using (var stream = await resp.Content.ReadAsStreamAsync())
            {
                var releases = System.Text.Json.JsonSerializer.Deserialize<Release[]>(stream);
                return releases ?? Array.Empty<Release>();
            }
        }

        public async Task<Release?> CheckUpdate(Version version)
        {
            var releases = await GetReleasesAsync();
            if (releases == null || releases.Length == 0)
                return null;

            foreach (var release in releases)
            {
                if (release.draft || release.prerelease)
                    continue;

                if (version == null)
                    return release;

                var semVer = Version.Parse(release.tag_name.TrimStart('v'));
                if (semVer.CompareTo(version) > 0)
                    return release;
            }
            return null;
        }

        public Task<Release?> CheckUpdate(string version)
        {
            var currentVersion = Version.Parse(version.TrimStart('v'));
            return CheckUpdate(currentVersion);
        }
    }
}