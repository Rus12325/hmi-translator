@echo off
"C:\Windows\Microsoft.NET\Framework\v2.0.50727\csc.exe" /target:library /out:"D:\New Folder\HmiTranslator.dll" "D:\translator\Translator_v2.cs"
echo Exit code: %errorlevel%
