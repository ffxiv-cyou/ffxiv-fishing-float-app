using System.Net.Http;

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
            public string url;
            public string html_url;
            public string tag_name;
            public string name;
            public bool draft;
            public bool prerelease;
            public string created_at;
            public string updated_at;
            public string publish_at;
            public string body;
        }

        async Task<Release[]> GetReleasesAsync()
        {
            HttpClient client = new HttpClient();
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