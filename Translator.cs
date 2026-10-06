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
        public static void LogUntranslated(string text)
        {
            try
            {
                if (_logPath != null && !string.IsNullOrEmpty(text))
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
