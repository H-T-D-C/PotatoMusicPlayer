using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using PotatoMusicPlayer.Models;

namespace PotatoMusicPlayer.Services
{
    /// <summary>
    /// JSONベースの表示文字列を管理するサービス。
    /// 言語追加時は Resources/Languages にJSONを追加し、LanguageFileNameへ登録する。
    /// </summary>
    public class LanguageService
    {
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();
        public Language CurrentLanguage { get; private set; }

        public LanguageService(Language language) => Load(language);

        public void Load(Language language)
        {
            CurrentLanguage = language;
            _strings.Clear();
            string path = Path.Combine(AppContext.BaseDirectory, "Resources", "Languages", LanguageFileName(language));
            if (!File.Exists(path)) return;

            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
            if (values == null) return;
            foreach (var value in values)
                _strings[value.Key] = value.Value;
        }

        public string Get(string key) => _strings.TryGetValue(key, out var value) ? value : key;

        private static string LanguageFileName(Language language) =>
            language == Language.Japanese ? "ja-JP.json" : "en-US.json";
    }
}
