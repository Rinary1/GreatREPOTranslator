#nullable disable

using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Reflection;
using Content.Client.MenuSliderElement;
using System;
using TMPro;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using LogType = Content.Client.LogManager.LogType;
using static ChatManager;
using UnityEngine.Events;
using WebSocketSharp;

namespace Content.Client.Translator;

[HarmonyPatch]
[BepInPlugin("Great_REPO_Translator", "Great REPO Translator", "1.4.2")]
public class REPO_Translator : BaseUnityPlugin
{

    public static REPO_Translator PluginInstance;

    public static REPO_Translator_Config ConfigInstance;

    public static LogManager.LogManager _logMan;

    public static LanguageManager.LanguageManager _langMan;

    public static Harmony HarmonyInstance;

    public static TMP_FontAsset PerfectFontCyrillicAsset;

    public static TMP_FontAsset VCROSDFontCyrillicAsset;

    public static TMP_FontAsset TekoRegularAsset;

    public static string TranslateFilePath;

    public static bool OneTimeInit;

    public static Dictionary<string, TranslatedTextInfo> AlreadyTranslatedStrings; // Source -> Translation, Language, FontSize

    public static Dictionary<string, InputKey> tagDictionary = new Dictionary<string, InputKey>();

    public static List<Translate> AllTranslates;

    public static List<Translate> AllChatMessages;

    public static List<DatasetEntry> AllDatasets;

    private FileSystemWatcher _configWatcher;

    private static AssetBundle _languageSliderBundle;
    private static GameObject _languageSliderPrefab;

    private void Awake()
    {
        PluginInstance = this;
        ConfigInstance = new REPO_Translator_Config(Config);
        ConfigInstance.RegisterOptions();
        _langMan = LanguageManager.LanguageManager.ManagerInstance ?? new LanguageManager.LanguageManager();
        _langMan.InitializeLanguages();
        _logMan = LogManager.LogManager.ManagerInstance ?? new LogManager.LogManager();

        LoadFonts();

        HarmonyInstance = new Harmony("Great_REPO_Translator");
        HarmonyInstance.PatchAll();
        InitializeTranslator();
        _logMan.TryLog("Loaded!", LogType.Info);
    }
    
    private static void LoadFonts()
    {
        string fallbackTempDir = Path.Combine(TranslateFilePath, "temp");

        string tempPath = Path.GetTempPath();
        try
        {
            string testPath = Path.Combine(tempPath, "temp_test.tmp");
            File.WriteAllText(testPath, "test");
            File.Delete(testPath);
        }
        catch
        {
            _logMan.TryLog($"Default temp path '{tempPath}' is not writable. Falling back to '{fallbackTempDir}'", LogType.Warning);
            tempPath = fallbackTempDir;

            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);
        }

        string TempPerfectFontCyrillic = Path.Combine(tempPath, "PerfectDOSVGA437_CYRILLIC.ttf");
        using (Stream PerfectFontCyrillicStream = LoadEmbeddedResource("Great_REPO_Translator.Resources.Fonts.PerfectDOSVGA437_CYRILLIC.ttf"))
        using (var file = File.Create(TempPerfectFontCyrillic))
            PerfectFontCyrillicStream.CopyTo(file);
        Font PerfectFontCyrillic = new Font(TempPerfectFontCyrillic);
        PerfectFontCyrillicAsset = TMP_FontAsset.CreateFontAsset(PerfectFontCyrillic);

        string TempVCROSDFontCyrillic = Path.Combine(tempPath, "VCR_OSD_MONO_CYRILLIC.ttf");
        using (Stream VCROSDFontCyrillicStream = LoadEmbeddedResource("Great_REPO_Translator.Resources.Fonts.VCR_OSD_MONO_CYRILLIC.ttf"))
        using (var file = File.Create(TempVCROSDFontCyrillic))
            VCROSDFontCyrillicStream.CopyTo(file);
        Font VCROSDFontCyrillic = new Font(TempVCROSDFontCyrillic);
        VCROSDFontCyrillicAsset = TMP_FontAsset.CreateFontAsset(VCROSDFontCyrillic);

        string TempTekoRegular = Path.Combine(tempPath, "TekoRegular.ttf");
        using (Stream TekoRegularStream = LoadEmbeddedResource("Great_REPO_Translator.Resources.Fonts.TekoRegular.ttf"))
        using (var file = File.Create(TempTekoRegular))
            TekoRegularStream.CopyTo(file);
        Font TekoRegularFont = new Font(TempTekoRegular);
        TekoRegularAsset = TMP_FontAsset.CreateFontAsset(TekoRegularFont);
    }

    public static Stream LoadEmbeddedResource(string name)
    {
        var asm = Assembly.GetExecutingAssembly();
        return asm.GetManifestResourceStream(name);
    }

    private static void WatchConfigFile()
    {
        if (!REPO_Translator_Config.HotReloadEnabled.Value)
        {
            _logMan.TryLog("Hot Reload Disabled, startup denied", LogType.Info);
            return;
        }

        string configDir = TranslateFilePath;
        string configFile = GetTranslatePath();

        PluginInstance._configWatcher = new FileSystemWatcher(configDir)
        {
            NotifyFilter = NotifyFilters.LastWrite
        };
        _logMan.TryLog($"Setuping file watcher in: {configDir}", LogType.Info);
        _logMan.TryLog($"Watching file: Translate_{_langMan.GetSelectedLanguage()}" + GetTranslateExtantion(), LogType.Info);
        PluginInstance._configWatcher.Filter = "Translate_" + _langMan.GetSelectedLanguage() + GetTranslateExtantion();
        PluginInstance._configWatcher.Changed += OnConfigFileChanged;
        PluginInstance._configWatcher.Created += OnConfigFileChanged;
        PluginInstance._configWatcher.Renamed += OnConfigFileChanged;
        PluginInstance._configWatcher.EnableRaisingEvents = true;
    }

    private static void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        _logMan.TryLog($"Config file changed: {e.FullPath}, reloading...", LogType.Info);
        ReloadTranslations();
    }

    public static void ReloadTranslations()
    {
        try
        {
            LoadTranslationsFromFile();

            UpdateAllTextMeshProObjects();

            WatchConfigFile();

            _logMan.TryLog("Translations successfully reloaded.", LogType.Info);
        }
        catch (Exception ex)
        {
            _logMan.TryLog($"Error reloading translations: {ex}", LogType.Error);
        }
    }

    private static void UpdateAllTextMeshProObjects()
    {
        foreach (var textObject in FindObjectsOfType<TMP_Text>())
        {
            if (textObject != null)
            {
                string key = AlreadyTranslatedStrings.FirstOrDefault(pair => pair.Value.TranslatedText == textObject.text).Key;
                string currentSelectedLanguage = _langMan.GetSelectedLanguage();
                if (key != null && AlreadyTranslatedStrings.TryGetValue(key, out var info) && info.LanguageCode != currentSelectedLanguage)
                {
                    AlreadyTranslatedStrings.Remove(key);
                    UpdateTextMeshProText(textObject, ref key, true);
                    textObject.text = key;
                } 
                else if (key == null)
                {
                    string text = textObject.text;
                    UpdateTextMeshProText(textObject, ref text, true);
                    textObject.text = text;
                }
            }
        }
    }

    private static void UpdateTextMeshProText(TMP_Text textObject, ref string text, bool setText = true)
    {
        if (textObject == null || string.IsNullOrEmpty(text) || textObject.transform.root == null || _langMan == null)
            return;

        string rootName = textObject.transform.root.gameObject.name;
        if (rootName == "UniverseLibCanvas" || rootName == "ExplorerCanvas")
            return;

        if (int.TryParse(text, out _))
            return;
        
        UpdateFontIfNeeded(textObject);
        
        if (AlreadyTranslatedStrings.TryGetValue(text, out TranslatedTextInfo translatedInfo) && translatedInfo.Translation != null)
        {
            if (translatedInfo.Translation != null)
                ApplyTranslateSettings(textObject, translatedInfo.Translation);
            
            if (setText && textObject.text != translatedInfo.TranslatedText)
                textObject.SetText(translatedInfo.TranslatedText, true);
            else
                text = translatedInfo.TranslatedText;
            return;
        }

        string exportText = text;
        Translate translate = AllTranslates?.Find(t => DisplayReplaceTags(t.key) == (t.ignoreCase ? exportText.Trim().ToLower() : exportText.Trim()));

        if (translate != null)
        {
            exportText = translate.trim ? DisplayReplaceTags(translate.translate).Trim() : DisplayReplaceTags(translate.translate);
            
            AlreadyTranslatedStrings[text] = new TranslatedTextInfo(exportText, _langMan.GetSelectedLanguage(), translate);

            ApplyTranslateSettings(textObject, translate);
        }
        else
        {
            var parts = AllTranslates?.Where(t => exportText.Contains(DisplayReplaceTags(t.key)) && t.part).ToList();
            if (parts != null && parts.Count > 0)
            {
                foreach (var part in parts)
                {
                    string partTranslate = part.trim ? part.translate.Trim() : part.translate;
                    try
                    {
                        exportText = Regex.Replace(exportText, part.key, part.newLine ? $"{partTranslate}<br>" : partTranslate);
                    }
                    catch (ArgumentException ex)
                    {
                        _logMan.TryLog($"Invalid regex pattern in key: '{part.key}' — {ex.Message}", LogType.Warning);
                    }
                }
                
                AlreadyTranslatedStrings[text] = new TranslatedTextInfo(exportText, _langMan.GetSelectedLanguage(), null);

                ApplyTranslateSettings(textObject, parts.Last());
            }
            else if (!IsMessageUnwanted(exportText) && !exportText.Any(char.IsDigit))
            {
                if (REPO_Translator_Config.TranslatorDevModeEnabled.Value)
                {
                    var newTranslate = new Translate
                    {
                        key = exportText,
                        translate = exportText,
                        size = textObject.fontSize
                    };
                    AllTranslates.Add(newTranslate);
                    SaveTranslateData(AllTranslates);
                }
                else if (!AlreadyTranslatedStrings.TryGetValue(exportText, out TranslatedTextInfo dontTranslated))
                    _logMan.TryLog($"WARNING: Untranslated Key: [{exportText.Trim()}]", LogType.Warning);
            }
        }

        if (StatsUI.instance != null && StatsUI.instance.textNumbers.lineSpacing != StatsUI.instance.Text.lineSpacing)
            StatsUI.instance.textNumbers.lineSpacing = StatsUI.instance.Text.lineSpacing;

        if (setText)
            textObject.SetText(exportText, true);
        else
            text = exportText;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ChatManager), "PossessChat")]
    public static bool ChatManager_PossessChat(ChatManager __instance, PossessChatID _possessChatID, ref string message, float typingSpeed, Color _possessColor, float _messageDelay, bool sendInTaxmanChat, int sendInTaxmanChatEmojiInt, UnityEvent eventExecutionAfterMessageIsDone)
    {
        var exportMessage = message;
        if (AllChatMessages?.Count > 0)
        {
            var found = AllChatMessages.Find(t => DisplayReplaceTags(t.key) == exportMessage.Trim());
            var parts = AllChatMessages?.Where(t => exportMessage.Contains(DisplayReplaceTags(t.key)) && t.part).ToList();
            if (found != null)
                exportMessage = found.translate;
            else if (parts != null && parts.Any())
            {
                foreach (var part in parts)
                {
                    var partTranslate = part.trim ? part.translate.Trim() : part.translate;
                    exportMessage = exportMessage.Replace(part.key, part.newLine ? string.Concat(partTranslate, "<br>") : partTranslate);
                }
            }
        }
        message = exportMessage;

        return true;
    }


    // Fix for StatsUI: semiworks shitcode
    [HarmonyPrefix]
    [HarmonyPatch(typeof(StatsUI), "Fetch")]
    public static bool StatsUI_Fetch(StatsUI __instance)
    {
        var currentUpgrades = StatsManager.instance.FetchPlayerUpgrades(PlayerController.instance.playerSteamID);

        if (__instance.playerUpgrades != null && __instance.playerUpgrades.Count == currentUpgrades.Count && !__instance.playerUpgrades.Except(currentUpgrades).Any())
            return false;

        __instance.playerUpgrades = StatsManager.instance.FetchPlayerUpgrades(PlayerController.instance.playerSteamID);
        __instance.Text.text = "";
        __instance.textNumbers.text = "";
        __instance.upgradesHeader.enabled = false;
        __instance.scanlineObject.SetActive(value: false);

        foreach (KeyValuePair<string, int> playerUpgrade in __instance.playerUpgrades)
        {
            if (playerUpgrade.Value > 0)
            {
                string upgradeName = playerUpgrade.Key.ToUpper();

                if (!string.IsNullOrEmpty(__instance.Text.text))
                    __instance.Text.text += "\n";

                __instance.Text.text += upgradeName;

                if (!string.IsNullOrEmpty(__instance.textNumbers.text))
                    __instance.textNumbers.text += "\n";

                __instance.textNumbers.text += $"<b>{playerUpgrade.Value}</b>";
            }
        }

        if (!string.IsNullOrEmpty(__instance.Text.text))
        {
            __instance.upgradesHeader.enabled = true;
            __instance.scanlineObject.SetActive(true);
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
    public static bool OV_TextMeshProUGUI_OnEnable(TextMeshProUGUI __instance)
    {
        if (__instance != null && AllTranslates != null)
        {
            string text = __instance.text;
            UpdateTextMeshProText(__instance, ref text, setText: true);
        }
        else if (AllTranslates == null)
        {
            _logMan.TryLog("AllTranslates is null!", LogType.Error);
        }
        return true;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.text), MethodType.Setter)]
    public static bool OV_TMP_Text_text(TMP_Text __instance, ref string value)
    {
        if (__instance != null)
            UpdateTextMeshProText(__instance, ref value, setText: false);
        return true;
    }

    public static bool IsMessageUnwanted(string message)
    {
        string cleanedMessage = Regex.Replace(message, "<.*?>", "").Trim();

        bool hasLetters = Regex.IsMatch(cleanedMessage, @"\p{L}");

        bool onlyNumbersAndSymbols = Regex.IsMatch(cleanedMessage, @"^[\d\p{P}\p{S}\s]+$");

        bool containsDollar = cleanedMessage.Contains("$");

        return !hasLetters || onlyNumbersAndSymbols || containsDollar;
    }

    public static string GetTranslatePath() => TranslateFilePath + "\\Translate_" + _langMan.GetSelectedLanguage() + GetTranslateExtantion();

    public static string GetTranslateExtantion() => REPO_Translator_Config.TranslatorFileExtansion.Value == "YML" ? ".yml" : ".xml";

    public static List<Translate> GetTranslateData(bool ChatMessages = false)
    {
        try
        {
            if (_langMan.GetSelectedLanguage() == "EN")
                return new List<Translate>();

            string path = GetTranslatePath();
            string extension = GetTranslateExtantion();

            List<Translate> list = null;

            if (extension == ".yml")
            {
                var yaml = File.ReadAllText(path);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var data = deserializer.Deserialize<TranslationDataFile>(yaml);

                list = data.translations;
            }
            else if (extension == ".xml")
            {
                var serializer = new XmlSerializer(typeof(List<Translate>));
                using (var reader = new StreamReader(path))
                {
                    list = (List<Translate>)serializer.Deserialize(reader);
                }
            }
            else
            {
                throw new Exception($"Unsupported file extension: {extension}");
            }
            
            list.RemoveAll(item =>
            {
                bool invalid = string.IsNullOrWhiteSpace(item.key) || string.IsNullOrWhiteSpace(item.translate);
                if (invalid)
                    Console.WriteLine("Warning: skipped translation with missing 'key' or 'translate'");
                return invalid;
            });

            if (ChatMessages)
                list = list.Where(t => t.chatMessage).ToList();

            return list;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading Translation file: {ex.Message}");
            return new List<Translate>();
        }
    }

    public static List<DatasetEntry> GetDatasetEntries()
    {
        try
        {
            if (_langMan.GetSelectedLanguage() == "EN")
                return new List<DatasetEntry>();

            string path = GetTranslatePath();
            string extension = GetTranslateExtantion();

            List<DatasetEntry> list = new List<DatasetEntry>();

            if (extension == ".yml")
            {
                var yaml = File.ReadAllText(path);
                var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).IgnoreUnmatchedProperties().Build();

                var data = deserializer.Deserialize<TranslationDataFile>(yaml);

                list = data.datasets;
            }
            else
            {
                throw new Exception($"Unsupported file extension: {extension}");
            }
            
            list.RemoveAll(item =>
            {
                bool invalid = string.IsNullOrWhiteSpace(item.key) || item.translations.Count == 0;
                if (invalid)
                    Console.WriteLine("Warning: skipped dataset with missing 'key' or 'translations'");
                return invalid;
            });

            return list;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading Datasets from file: {ex.Message}");
            return new List<DatasetEntry>();
        }
    }

    public static void SaveTranslateData(List<Translate> translations)
    {
        try
        {
            string path = GetTranslatePath();
            string extension = GetTranslateExtantion();
            
            TranslationDataFile dataToSave = new TranslationDataFile
            {
                translations = translations,
                datasets = AllDatasets
            };

            if (extension == ".yml")
            {
                var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

                string yaml = serializer.Serialize(dataToSave);

                File.WriteAllText(GetTranslatePath(), yaml);
            }
            else if (extension == ".xml")
            {
                TextWriter textWriter = new StreamWriter(GetTranslatePath());
                new XmlSerializer(typeof(List<Translate>)).Serialize(textWriter, translations);
                textWriter.Close();
            }
            else
            {
                throw new Exception($"Unsupported file extension: {extension}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving Translation YAML: {ex.Message}");
        }
    }

    public static string DisplayReplaceTags(string _text)
    {
        if (_text == null || InputManager.instance == null || tagDictionary == null || tagDictionary.Count == 0)
            return _text;

        string text = _text;

        foreach (KeyValuePair<string, InputKey> item in tagDictionary)
        {
            if (item.Key == "[move]" && text.Contains(item.Key))
            {
                string UpKey = DisplayTextReplace(InputManager.instance.GetMovementKeyString(MovementDirection.Up).Split('/')[^1].ToUpper());
                string LeftKey = DisplayTextReplace(InputManager.instance.GetMovementKeyString(MovementDirection.Left).Split('/')[^1].ToUpper());
                string DownKey = DisplayTextReplace(InputManager.instance.GetMovementKeyString(MovementDirection.Down).Split('/')[^1].ToUpper());
                string RightKey = DisplayTextReplace(InputManager.instance.GetMovementKeyString(MovementDirection.Right).Split('/')[^1].ToUpper());
                text = text.Replace(item.Key, "<u><b>" + UpKey + LeftKey + DownKey + RightKey + "</b></u>");
            }
            else if (text.Contains(item.Key))
            {
                string keyString = InputManager.instance.GetKeyString(item.Value);
                string cleanedKey = DisplayTextReplace(keyString.Split('/')[^1].ToUpper());
                if (keyString.Contains("scroll/y"))
                    cleanedKey = DisplayTextReplace(keyString);

                text = text.Replace(item.Key, "<u><b>" + cleanedKey + "</b></u>");
            }
        }
        return text;
    }

    public static string DisplayTextReplace(string text)
    {
        if (text == "CTRL") return "CONTROL";
        if (text == "LEFTSHIFT") return "LEFT SHIFT";
        if (text == "LEFTBUTTON") return "MOUSE LEFT";
        else if (text == "RIGHTBUTTON") return "MOUSE RIGHT";
        else if (text == "MIDDLEBUTTON") return "MOUSE MIDDLE";
        else if (text.Contains("scroll/y")) return "MOUSE SCROLL";

        return text;
    }

    public static void LoadTranslationsFromFile()
    {
        if (!File.Exists(GetTranslatePath()))
        {
            AllTranslates = new List<Translate>();
            if (REPO_Translator_Config.TranslatorDevModeEnabled.Value)
            {
                SaveTranslateData(AllTranslates);
            }
            else
            {
                _logMan.TryLog("I can't find translations file, maybe... You don't correct install me? Check instruction on: https://github.com/Rinary1/GreatREPOTranslator", LogType.Error);
            }
        }
        else
        {
            AllTranslates = GetTranslateData();
            AllChatMessages = GetTranslateData(true);
            AllDatasets = GetDatasetEntries();
        }
    }

    public List<string> GetAllAvailableTranslations()
    {
        string searchPattern = "Translate_*" + GetTranslateExtantion();

        if (!Directory.Exists(TranslateFilePath))
            TranslateFilePath = Path.GetDirectoryName(PluginInstance.Info.Location);

        var files = Directory.GetFiles(TranslateFilePath, searchPattern);
        
        var Languages = files.Select(f => Path.GetFileNameWithoutExtension(f)).Where(name => name.StartsWith("Translate_")).Select(name => name.Replace("Translate_", "")).ToList();
        Languages.Add("EN");

        return Languages;
    }

    public int GetAvailableTranslationsCount()
    {
        return GetAllAvailableTranslations().Count;
    }

    [HarmonyPatch(typeof(MenuPageSettingsPage), "Start")]
    [HarmonyPostfix]
    private static void MenuPageSettings_StartHook(MenuPageSettingsPage __instance)
    {
        if (__instance.settingType != DataDirector.SettingType.Gameplay) return;

        var page = __instance.GetComponent<MenuPage>();
        if (page == null)
        {
            _logMan.TryLog("Page not founded", LogType.Error);
            return;
        }


        Transform scroller = FindDeepChild(page.transform, "Scroller");
        if (scroller == null)
        {
            _logMan.TryLog("Scroller not found inside page", LogType.Error);
            return;
        }

        if (_languageSliderBundle == null || _languageSliderPrefab == null)
        {
            _logMan.TryLog("Loading AssetBundle and prefab for the first time...", LogType.Info);

            Stream LanguageSliderStream = LoadEmbeddedResource("REPO_Translator.Resources.prefab.unity3d");
            string TempLanguageSlider = Path.Combine(Path.GetTempPath(), "prefab.unity3d");

            if (!File.Exists(TempLanguageSlider))
            {
                using (var file = File.Create(TempLanguageSlider))
                {
                    LanguageSliderStream.CopyTo(file);
                }
            }

            _languageSliderBundle = AssetBundle.LoadFromFile(TempLanguageSlider);
            if (_languageSliderBundle == null)
            {
                _logMan.TryLog("Failed to load AssetBundle.", LogType.Error);
                return;
            }

            _languageSliderPrefab = _languageSliderBundle.LoadAsset<GameObject>("LanguageSlider");
            if (_languageSliderPrefab == null)
            {
                _logMan.TryLog("Failed to load prefab 'LanguageSlider' from bundle.", LogType.Error);
                return;
            }
        }

        var sliderObj = Instantiate(_languageSliderPrefab, scroller, false);
        var sliderPos = sliderObj.transform.localPosition;
        sliderPos.x = 3.7f;
        sliderObj.transform.localPosition = sliderPos;
        InsertAndShiftByY(scroller, sliderObj, 4);
        _logMan.TryLog("Language slider added to Gameplay settings", LogType.Info);
        sliderObj.AddComponent<MenuSliderLanguage>();
    }

    private static void InsertAndShiftByY(Transform parent, GameObject newObj, int insertIndex)
    {
        if (parent == null || newObj == null) return;

        if (parent.childCount <= insertIndex)
        {
            _logMan.TryLog("Insert index out of range", LogType.Error);
            return;
        }

        Transform targetChild = parent.GetChild(insertIndex);
        Vector3 targetPos = targetChild.localPosition;

        Vector3 previousPos = insertIndex > 0
            ? parent.GetChild(insertIndex + 1).localPosition
            : targetPos;

        float yOffset = targetPos.y - previousPos.y;

        Transform newTransform = newObj.transform;
        Vector3 newPos = newTransform.localPosition;
        newPos.y = targetPos.y;
        newTransform.SetParent(parent, false);
        newTransform.localPosition = newPos;
        newTransform.SetSiblingIndex(insertIndex);

        for (int i = insertIndex + 1; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Vector3 pos = child.localPosition;
            pos.y -= yOffset;
            child.localPosition = pos;
        }
    }

    public static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            var result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
    
    private static void UpdateFontIfNeeded(TMP_Text textObject)
    {
        string fontName = textObject.font.name;

        if (fontName.Contains("Perfect") && textObject.font != PerfectFontCyrillicAsset)
            textObject.font = PerfectFontCyrillicAsset;
        else if (fontName.Contains("VCR OSD") && textObject.font != VCROSDFontCyrillicAsset)
            textObject.font = VCROSDFontCyrillicAsset;
        else if (fontName.Contains("Teko") && textObject.font != TekoRegularAsset)
            textObject.font = TekoRegularAsset;
    }
    
    private static void ApplyTranslateSettings(TMP_Text textObject, Translate translate)
    {
        if (translate.size != textObject.fontSize && translate.size != 0.0f)
        {
            textObject.fontSize = translate.size;
            textObject.lineSpacing = translate.lineSpacing != 0.0f ? translate.lineSpacing : textObject.lineSpacing;
            textObject.enableAutoSizing = false;
        }
        else
        {
            textObject.fontSizeMax = translate.autoSizingMax != 0f ? translate.autoSizingMax : textObject.fontSize;
            textObject.fontSizeMin = translate.autoSizingMin != 0f ? translate.autoSizingMin : textObject.fontSizeMin;
            textObject.lineSpacing = translate.lineSpacing != 0.0f ? translate.lineSpacing : textObject.lineSpacing;
            textObject.enableAutoSizing = translate.autoSizing;
        }
    }

    public void InitializeTranslator()
    {
        if (_langMan == null)
        {
            _logMan.TryLog("Language Manager not founded! NULL: Disabling Translator!", LogType.Fatal);
            Destroy(gameObject);
            return;
        }
        else if (_langMan.GetSelectedLanguage() == null)
        {
            _logMan.TryLog("Selected Language not founded! NULL: Disabling Translator!", LogType.Fatal);
            Destroy(gameObject);
            return;
        }

        if (OneTimeInit)
            return;

        _logMan.TryLog("Selected Translate: " + _langMan.GetSelectedLanguage(), LogType.Info);
        _logMan.TryLog($"DEVMODE Translate Enabled?: {REPO_Translator_Config.TranslatorDevModeEnabled.Value}", LogType.Info);
        TranslateFilePath = Path.GetDirectoryName(PluginInstance.Info.Location);
        _logMan.TryLog("TranslateFilePath: " + GetTranslatePath(), LogType.Info);
        if (REPO_Translator_Config.TranslatorDevModeEnabled.Value)
            _logMan.TryLog("WARNING: YOU HAVE ENABLED DEVMODE TRANSLATOR, DO NOT EDIT THE TRANSLATE FILE BEFORE TURNING OFF THE GAME!!!!", LogType.Warning);
        AlreadyTranslatedStrings = new Dictionary<string, TranslatedTextInfo>();
        LoadTranslationsFromFile();
        OneTimeInit = true;
        WatchConfigFile();

        tagDictionary.Add("[move]", InputKey.Movement);
        tagDictionary.Add("[jump]", InputKey.Jump);
        tagDictionary.Add("[grab]", InputKey.Grab);
        tagDictionary.Add("[grab2]", InputKey.Rotate);
        tagDictionary.Add("[sprint]", InputKey.Sprint);
        tagDictionary.Add("[crouch]", InputKey.Crouch);
        tagDictionary.Add("[map]", InputKey.Map);
        tagDictionary.Add("[inventory1]", InputKey.Inventory1);
        tagDictionary.Add("[inventory2]", InputKey.Inventory2);
        tagDictionary.Add("[inventory3]", InputKey.Inventory3);
        tagDictionary.Add("[tumble]", InputKey.Tumble);
        tagDictionary.Add("[interact]", InputKey.Interact);
        tagDictionary.Add("[push]", InputKey.Push);
        tagDictionary.Add("[pull]", InputKey.Pull);
        tagDictionary.Add("[chat]", InputKey.Chat);
    }
}