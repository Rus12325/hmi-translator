using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace HmiTranslator
{
    public static class Translator
    {
        private static readonly object _lock = new object();
        private static Dictionary<string, string> _cache = null;
        private static volatile bool _loaded = false;
        private static string _logPath = null;
        private static Dictionary<string, bool> _logged = null;


        private static bool IsAllDigits(string text)
        {
            for (int i = 0; i < text.Length; i++)
                if (!char.IsDigit(text[i])) return false;
            return true;
        }
        private static bool MatchPrefix(string text, string prefix)
        {
            if (!text.StartsWith(prefix)) return false;
            if (text.Length == prefix.Length) return true;
            return char.IsDigit(text[prefix.Length]);
        }

        private static bool IsInternalName(string text)
        {
            if (text == null || text.Length < 1) return true;
            if (text.Trim().Length == 0) return true;
            if (MatchPrefix(text, "bar")) return true;
            if (MatchPrefix(text, "buttonItem")) return true;
            if (MatchPrefix(text, "label")) return true;
            if (MatchPrefix(text, "panelEx")) return true;
            if (MatchPrefix(text, "superTab")) return true;
            if (MatchPrefix(text, "itemPanel")) return true;
            if (MatchPrefix(text, "help")) return true;
            if (MatchPrefix(text, "TabItem")) return true;
            if (MatchPrefix(text, "checkBox")) return true;
            if (MatchPrefix(text, "line")) return true;
            if (MatchPrefix(text, "toolStrip")) return true;
            if (text == "TopMessage") return true;
            if (text == "insert" || text == "load" || text == "type") return true;
            if (text == "logon" || text == "button1") return true;
            if (text == "attid" || text == "initval" || text == "zhushi") return true;
            if (text == "ID" || text == "OK" || text == "Cancel") return true;
            if (text == "BLACK" || text == "BLUE" || text == "Style") return true;
            if (text == "100%" || text == "ms") return true;
            if (text == "MessageForm" || text == "textmessage") return true;
            if (text == "progform" || text == "sendke") return true;
            if (text == "Form1") return true;
            if (MatchPrefix(text, "linkLabel")) return true;
            if (text.StartsWith("Ver:") || text.StartsWith("Model:") || text.StartsWith("Encoding:")) return true;
            if (IsAllDigits(text)) return true;
            if (text.StartsWith("Frames:")) return true;
            if (text.IndexOf("<font") >= 0) return true;
            return false;
        }

        private static bool ContainsCyrillic(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c >= '\u0400' && c <= '\u04FF')
                    return true;
            }
            return false;
        }

        public static string Translate(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            try
            {
                if (!_loaded) Load();
                // Уже переведено на русский — вернуть как есть, не логировать.
                if (ContainsCyrillic(text)) return text;
                lock (_lock)
                {
                    if (_cache != null)
                    {
                        string translated;
                        if (_cache.TryGetValue(text, out translated))
                            return translated;
                    }
                }
                LogUntranslated(text);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[HmiTranslator] Error: " + ex.Message);
            }
            return text;
        }

        private static void Load()
        {
            if (_loaded) return;
            Dictionary<string, string> cache = new Dictionary<string, string>();
            lock (_lock)
            {
                if (_loaded) return;
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string p1 = Path.Combine(basePath, "hmi_translation.txt");
                string p2 = Path.Combine(Path.GetDirectoryName(basePath), "hmi_translation.txt");
                string tf = File.Exists(p1) ? p1 : (File.Exists(p2) ? p2 : null);
                if (tf == null)
                {
                    Debug.WriteLine("[HmiTranslator] hmi_translation.txt not found!");
                    _cache = new Dictionary<string, string>();
                    _loaded = true;
                    return;
                }
                _logPath = Path.Combine(Path.GetDirectoryName(tf), "hmi.log");
                try
                {
                    string[] lines = File.ReadAllLines(tf, Encoding.UTF8);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;
                        int tabPos = line.IndexOf((char)9);
                        if (tabPos > 0)
                        {
                            string orig = line.Substring(0, tabPos);
                            string trans = line.Substring(tabPos + 1);
                            if (orig.Length > 0 && trans.Length > 0)
                                cache[orig] = trans;
                        }
                    }
                    Debug.WriteLine("[HmiTranslator] Loaded " + cache.Count + " translations");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[HmiTranslator] Error loading: " + ex.Message);
                }
                _cache = cache;
                _loaded = true;
            }
        }

        public static void LogUntranslated(string text)
        {
            try
            {
                if (_logPath == null) return;
                if (text.Length <= 1) return;
                if (IsInternalName(text)) return;
                lock (_lock)
                {
                    if (_logged == null) _logged = new Dictionary<string, bool>();
                    if (_logged.ContainsKey(text)) return;
                    _logged[text] = true;
                }
                File.AppendAllText(_logPath, text + "\r\n", Encoding.UTF8);
            }
            catch { }
        }
    }
}
