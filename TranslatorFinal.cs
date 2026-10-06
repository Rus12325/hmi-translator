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

        private static bool IsInternalName(string text)
        {
            if (text == null || text.Length < 2) return true;
            // Skip whitespace-only strings
            if (text.Trim().Length == 0) return true;
            // Skip pure ASCII short strings that are internal names
            if (text.StartsWith("bar")) return true;
            if (text.StartsWith("buttonItem")) return true;
            if (text.StartsWith("label")) return true;
            if (text.StartsWith("panelEx")) return true;
            if (text.StartsWith("superTab")) return true;
            if (text.StartsWith("itemPanel")) return true;
            if (text.StartsWith("help")) return true;
            if (text.StartsWith("TabItem")) return true;
            if (text.StartsWith("TopMessage")) return true;
            if (text.StartsWith("checkBox")) return true;
            if (text.StartsWith("line")) return true;
            if (text.StartsWith("button")) return true;
            if (text.StartsWith("Ver:")) return true;
            if (text.StartsWith("Model:")) return true;
            if (text == "insert" || text == "load" || text == "type") return true;
            if (text == "logon" || text == "button1") return true;
            if (text == "attid" || text == "initval" || text == "zhushi") return true;
            if (text == "ID" || text == "OK" || text == "Cancel") return true;
            if (text == "BLACK" || text == "BLUE" || text == "Style") return true;
            if (text == "ms" || text == "011 Module") return true;
            if (text == "100%" || text == "Encoding:") return true;
            if (text.IndexOf("<font") >= 0) return true;
            return false;
        }

        public static string Translate(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            try
            {
                if (!_loaded) Load();
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
            lock (_lock) { if (_loaded) return; }
            Dictionary<string, string> cache = new Dictionary<string, string>();
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string p1 = Path.Combine(basePath, "hmi_translation.txt");
            string p2 = Path.Combine(Path.GetDirectoryName(basePath), "hmi_translation.txt");
            string tf = File.Exists(p1) ? p1 : (File.Exists(p2) ? p2 : null);
            if (tf == null)
            {
                Debug.WriteLine("[HmiTranslator] hmi_translation.txt not found!");
                lock (_lock) { _cache = new Dictionary<string, string>(); }
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
            catch (Exception ex) { Debug.WriteLine("[HmiTranslator] Error: " + ex.Message); }
            lock (_lock) { _loaded = true; _cache = cache; }
        }

        public static void LogUntranslated(string text)
        {
            try
            {
                if (_logPath != null && text.Length > 1 && !IsInternalName(text))
                    File.AppendAllText(_logPath, text + "\r\n", Encoding.UTF8);
            }
            catch { }
        }
    }
}
