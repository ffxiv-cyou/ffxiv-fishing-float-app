using Newtonsoft.Json.Linq;
using System.Diagnostics.Tracing;

namespace FishingFloatApp.Overlay
{
    /// <summary>
    /// 游戏版本
    /// </summary>
    class GameVersion : IWorker
    {
        public string Name => "otk::game_ver";

        public GameVersion()
        {
        }

        public void Init(IEventRepo repo)
        {
            repo.RegisterHandler(this);
        }

        public JToken? HandleEvent(JObject e)
        {
            return JObject.FromObject(new
            {
                version = "2026.05.01.0000.0000",
                lang = 6,
            });
        }
    }
}
