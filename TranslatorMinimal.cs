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
        private static bool _loaded = false;
        private static string _logPath = null;

        public static string Translate(string text)
        {
            // Handle null/empty strings properly
            if (string.IsNullOrEmpty(text)) return text;

            try
            {
                if (!_loaded) Load();

                lock (_lock)
                {
                    if (_cache != null && _cache.TryGetValue(text, out string translated))
                    {
                        return translated;
                    }
                }

                // Log untranslated strings for debugging
                LogUntranslated(text);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HmiTranslator] Error translating '{text}': {ex.Message}");
            }

            return text;
        }

        private static void Load()
        {
            lock (_lock)
            {
                if (_loaded) return;
                _loaded = true;
            }

            var cache = new Dictionary<string, string>();

            string[] searchPaths = new string[]
            {
                // Look in same directory as the running executable
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hmi_translation.txt"),
                // Look one level up (in case exe is in a subdirectory)
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "hmi_translation.txt"),
            };

            string translationFile = null;
            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    translationFile = path;
                    break;
                }
            }

            if (translationFile == null)
            {
                Debug.WriteLine("[HmiTranslator] WARNING: hmi_translation.txt not found in any search path!");
                lock (_lock)
                {
                    _cache = new Dictionary<string, string>();
                }
                return;
            }

            _logPath = Path.Combine(Path.GetDirectoryName(translationFile), "hmi.log");

            Debug.WriteLine($"[HmiTranslator] Loading translations from: {translationFile}");

            try
            {
                string[] lines = File.ReadAllLines(translationFile, Encoding.UTF8);
                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                        continue;

                    int tabPos = line.IndexOf('\t');
                    if (tabPos > 0)
                    {
                        string original = line.Substring(0, tabPos);
                        string translated = line.Substring(tabPos + 1);
                        if (!string.IsNullOrEmpty(original) && !string.IsNullOrEmpty(translated))
                        {
                            cache[original] = translated;
                        }
                    }
                }
                Debug.WriteLine($"[HmiTranslator] Successfully loaded {cache.Count} translations");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HmiTranslator] Error reading translation file: {ex.Message}");
            }

            lock (_lock)
            {
                _cache = cache;
            }
        }

        /// <summary>
        /// Logs untranslated strings to hmi.log for later inclusion in the dictionary.
        /// </summary>

        private static bool IsInternalName(string text)
        {
            if (text == null || text.Length < 2) return true;
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
            if (text == "ms" || text == "011 Module" || text == "100%" || text == "Encoding:") return true;
            if (text.IndexOf("<font") >= 0) return true;
            return false;
        }
        public static void LogUntranslated(string text)
        {
            try
            {
                if (_logPath != null && text.Length > 1 && !IsInternalName(text))
                {
                    // Only log strings that look like they could be UI text
                    // (skip very short strings, numbers, paths, etc.)
                    if (text.Length > 1 && !text.Contains("\\") && !text.Contains("/"))
                    {
                        File.AppendAllText(_logPath, text + Environment.NewLine, Encoding.UTF8);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HmiTranslator] Error logging untranslated text: {ex.Message}");
            }
        }
    }
}
