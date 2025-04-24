#nullable disable
using BepInEx.Logging;
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
using System.Runtime.CompilerServices;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Content.Client.Translator;

[HarmonyPatch]
[BepInPlugin("Great_REPO_Translator", "REPO_Translator", "1.3.0")]
public class REPO_Translator : BaseUnityPlugin
{
    public class Translate
    {
        // Only For YAML, maybe... Font replace on this? Or another things, like external assets for texture translations?
        public string type { get; set; } = "Translation";
        
        [XmlAttribute]
        public string key { get; set; }

        [XmlAttribute]
        public string translate { get; set; }

        [XmlAttribute]
        public float size { get; set; } = 0.0f;

        [XmlAttribute]
        public float lineSpacing { get; set; } = 0.0f;

        [XmlAttribute]
        public bool autoSizing { get; set; } = true;

        [XmlAttribute]
        public float autoSizingFontMin { get; set; } = 0f;

        [XmlAttribute]
        public bool part { get; set; } = false;

        [XmlAttribute]
        public bool trim { get; set; } = false;
        
        // Only for part
        [XmlAttribute]
        public bool newLine { get; set; } = false;
    }

    public static REPO_Translator PluginInstance;

    public static REPO_Translator_Config ConfigInstance;

    public static LanguageManager.LanguageManager _langMan;

    public static ManualLogSource Log;

    public static Harmony HarmonyInstance;

    public static TMP_FontAsset PerfectFontCyrillicAsset;

    public static TMP_FontAsset VCROSDFontCyrillicAsset;

    public static TMP_FontAsset TekoRegularAsset;

    public static string TranslateFilePath;

    public static bool OneTimeInit;

    public static Dictionary<string, string> AlreadyTranslatedStrings; // Source -> Translation
    
    public static Dictionary<string, string> AlreadyTranslatedCodes; // Source -> LanguageCode
    
    public static Dictionary<string, InputKey> tagDictionary = new Dictionary<string, InputKey>();

    public static List<Translate> AllTranslates;

    private FileSystemWatcher _configWatcher;

    private static AssetBundle _languageSliderBundle;
    private static GameObject _languageSliderPrefab;

    private void Awake()
    {
        PluginInstance = this;
        Log = Logger;
        ConfigInstance = new REPO_Translator_Config(Config);
        ConfigInstance.RegisterOptions();
        _langMan = LanguageManager.LanguageManager.ManagerInstance ?? new LanguageManager.LanguageManager();
        _langMan.InitializeLanguages();

        LoadFonts();

        HarmonyInstance = new Harmony("REPO_Translator");
        HarmonyInstance.PatchAll();
        InitializeTranslator();
        Log.LogInfo("Loaded!");
    }

    private static void LoadFonts()
    {
        string TempPerfectFontCyrillic = Path.Combine(Path.GetTempPath(), "PerfectDOSVGA437_CYRILLIC.ttf");
        using (Stream PerfectFontCyrillicStream = LoadEmbeddedResource("REPO_Translator.Resources.Fonts.PerfectDOSVGA437_CYRILLIC.ttf"))
        {
            using (var file = File.Create(TempPerfectFontCyrillic))
            {
                PerfectFontCyrillicStream.CopyTo(file);
            }
        }
        Font PerfectFontCyrillic = new Font(TempPerfectFontCyrillic);
        PerfectFontCyrillicAsset = TMP_FontAsset.CreateFontAsset(PerfectFontCyrillic);

        string TempVCROSDFontCyrillic = Path.Combine(Path.GetTempPath(), "VCR_OSD_MONO_CYRILLIC.ttf");
        using (Stream VCROSDFontCyrillicStream = LoadEmbeddedResource("REPO_Translator.Resources.Fonts.VCR_OSD_MONO_CYRILLIC.ttf"))
        {
            using (var file = File.Create(TempVCROSDFontCyrillic))
            {
                VCROSDFontCyrillicStream.CopyTo(file);
            }
        }
        Font VCROSDFontCyrillic = new Font(TempVCROSDFontCyrillic);
        VCROSDFontCyrillicAsset = TMP_FontAsset.CreateFontAsset(VCROSDFontCyrillic);

        string TempTekoRegular = Path.Combine(Path.GetTempPath(), "TekoRegular.ttf");
        using (Stream TekoRegularStream = LoadEmbeddedResource("REPO_Translator.Resources.Fonts.TekoRegular.ttf"))
        {
            using (var file = File.Create(TempTekoRegular))
            {
                TekoRegularStream.CopyTo(file);
            }
        }
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
        string configDir = TranslateFilePath;
        string configFile = GetTranslatePath();

        PluginInstance._configWatcher = new FileSystemWatcher(configDir)
        {
            NotifyFilter = NotifyFilters.LastWrite
        };
        Log.LogInfo($"Setuping file watcher in: {configDir}");
        Log.LogInfo($"Watching file: Translate_{_langMan.GetSelectedLanguage()}" + GetTranslateExtantion());
        PluginInstance._configWatcher.Filter = "Translate_" + _langMan.GetSelectedLanguage() + GetTranslateExtantion();
        PluginInstance._configWatcher.Changed += OnConfigFileChanged;
        PluginInstance._configWatcher.Created += OnConfigFileChanged;
        PluginInstance._configWatcher.Renamed += OnConfigFileChanged;
        PluginInstance._configWatcher.EnableRaisingEvents = true;
    }

    private static void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        Log.LogInfo($"Config file changed: {e.FullPath}, reloading...");
        ReloadTranslations();
    }

    public static void ReloadTranslations()
    {
        try
        {
            LoadTranslationsFromFile();

            UpdateAllTextMeshProObjects();

            WatchConfigFile();

            Log.LogInfo("Translations successfully reloaded.");
        }
        catch (Exception ex)
        {
            Log.LogError($"Error reloading translations: {ex}");
        }
    }

    private static void UpdateAllTextMeshProObjects()
    {
        foreach (var textObject in FindObjectsOfType<TMP_Text>())
        {
            if (textObject != null)
            {
                string key = AlreadyTranslatedStrings.FirstOrDefault(pair => pair.Value == textObject.text).Key;
                string currentSelectedLanguage = _langMan.GetSelectedLanguage();
                if (key != null && AlreadyTranslatedCodes[key] != currentSelectedLanguage)
                {
                    AlreadyTranslatedStrings.Remove(key);
                    AlreadyTranslatedCodes.Remove(key);
                    UpdateTextMeshProText(textObject, ref key, true);
                    textObject.text = key;
                }
                else
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

        if (!int.TryParse(text, out _))
        {
            if (textObject.font.name.Contains("Perfect"))
                textObject.font = PerfectFontCyrillicAsset;
            else if (textObject.font.name.Contains("VCR OSD"))
                textObject.font = VCROSDFontCyrillicAsset;
            else if (textObject.font.name.Contains("Teko"))
                textObject.font = TekoRegularAsset;

            string exportText = text;
            Translate translate = AllTranslates?.Find(t => DisplayReplaceTags(t.key) == exportText.Trim());
            var parts = AllTranslates?.Where(t => exportText.Contains(DisplayReplaceTags(t.key)) && t.part).ToList();

            if (translate != null)
            {
                exportText = translate.trim ? DisplayReplaceTags(translate.translate).Trim() : DisplayReplaceTags(translate.translate);
                if (!AlreadyTranslatedStrings.ContainsKey(text))
                    AlreadyTranslatedStrings.Add(text, exportText);
                string selectedLanguage = _langMan.GetSelectedLanguage();
                if (!AlreadyTranslatedCodes.ContainsKey(text))
                    AlreadyTranslatedCodes.Add(text, selectedLanguage);

                if (translate.size != 0.0f)
                {
                    textObject.fontSize = translate.size;
                    textObject.lineSpacing = translate.lineSpacing != 0.0f ? translate.lineSpacing : textObject.lineSpacing;
                    textObject.enableAutoSizing = false;
                }
                else
                {
                    textObject.fontSizeMax = textObject.fontSize;
                    textObject.fontSizeMin = translate.autoSizingFontMin;
                    textObject.lineSpacing = translate.lineSpacing != 0.0f ? translate.lineSpacing : textObject.lineSpacing;
                    textObject.enableAutoSizing = translate.autoSizing;
                }
            }
            else if (parts != null && parts.Any())
            {
                foreach (var part in parts)
                {
                    var partTranslate = part.trim ? part.translate.Trim() : part.translate;
                    exportText = exportText.Replace(part.key, part.newLine ? string.Concat(partTranslate, "<br>") : partTranslate);
                    textObject.lineSpacing = part.lineSpacing != 0.0f ? part.lineSpacing : textObject.lineSpacing;
                    textObject.enableAutoSizing = part.autoSizing;
                }
            }
            else if (!AlreadyTranslatedStrings.ContainsValue(exportText))
            {
                if (!exportText.Any(char.IsDigit) && REPO_Translator_Config.TranslatorDevModeEnabled.Value)
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
                else if (!IsMessageUnwanted(exportText))
                {
                    Log.LogWarning($"WARNING: Untranslated Key: [{exportText.Trim()}]");
                }
            }
            
            if (StatsUI.instance != null && StatsUI.instance.textNumbers.lineSpacing != StatsUI.instance.Text.lineSpacing)
                StatsUI.instance.textNumbers.lineSpacing = StatsUI.instance.Text.lineSpacing;

            if (setText)
                textObject.SetText(exportText, true);
            else
                text = exportText;
        }
    }
    
    
    // Fix for StatsUI: semiworks shitcode
    [HarmonyPrefix]
    [HarmonyPatch(typeof(StatsUI), "Fetch")]
    public static bool StatsUI_Fetch(StatsUI __instance)
    {
        Dictionary<string, int> playerUpgrades = StatsManager.instance.FetchPlayerUpgrades(PlayerController.instance.playerSteamID);
        __instance.Text.text = "";
        __instance.textNumbers.text = "";
        __instance.upgradesHeader.enabled = false;
        __instance.scanlineObject.SetActive(false);
        
        foreach (KeyValuePair<string, int> playerUpgrade in playerUpgrades)
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
            Log.LogError("AllTranslates is null!");
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
    
    public static List<Translate> GetTranslateData()
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
                    .Build();

                list = deserializer.Deserialize<List<Translate>>(yaml);
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

            return list;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading Translation file: {ex.Message}");
            return new List<Translate>();
        }
    }
    
    public static void SaveTranslateData(List<Translate> dataToSave)
    {
        try
        {
            string path = GetTranslatePath();
            string extension = GetTranslateExtantion();
            
            if (extension == ".yml")
            {
                var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

                string yaml = serializer.Serialize(dataToSave);

                File.WriteAllText(GetTranslatePath(), yaml);
            }
            else if (extension == ".xml")
            {
                TextWriter textWriter = new StreamWriter(GetTranslatePath());
                new XmlSerializer(typeof(List<Translate>)).Serialize(textWriter, dataToSave);
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
                Log.LogError("I can't find translation file, check instruction on this link:https://thunderstore.io/c/repo/p/QERT2002/REPO_Translator/ if you try to do translation on your language!!!");
            }
        }
        else
        {
            AllTranslates = GetTranslateData();
        }
    }

    public List<string> GetAllAvailableTranslations()
    {
        string searchPattern = "Translate_*" + GetTranslateExtantion();

        if (!Directory.Exists(TranslateFilePath))
            TranslateFilePath = Path.GetDirectoryName(PluginInstance.Info.Location);

        var files = Directory.GetFiles(TranslateFilePath, searchPattern);

        return files
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Where(name => name.StartsWith("Translate_"))
            .Select(name => name.Replace("Translate_", ""))
            .ToList();
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
            Log.LogError("Page not founded");
            return;
        }
            

        Transform scroller = FindDeepChild(page.transform, "Scroller");
        if (scroller == null)
        {
            Log.LogError("Scroller not found inside page");
            return;
        }

        if (_languageSliderBundle == null || _languageSliderPrefab == null)
        {
            Log.LogInfo("Loading AssetBundle and prefab for the first time...");

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
                Log.LogError("Failed to load AssetBundle.");
                return;
            }

            _languageSliderPrefab = _languageSliderBundle.LoadAsset<GameObject>("LanguageSlider");
            if (_languageSliderPrefab == null)
            {
                Log.LogError("Failed to load prefab 'LanguageSlider' from bundle.");
                return;
            }
        }

        var sliderObj = Instantiate(_languageSliderPrefab, scroller, false);
        var sliderPos = sliderObj.transform.localPosition;
        sliderPos.x = 3.7f;
        sliderObj.transform.localPosition = sliderPos;
        InsertAndShiftByY(scroller, sliderObj, 4);
        Log.LogInfo("Language slider added to Gameplay settings");
        sliderObj.AddComponent<MenuSliderLanguage>();
    }

    private static void InsertAndShiftByY(Transform parent, GameObject newObj, int insertIndex)
    {
        if (parent == null || newObj == null) return;

        if (parent.childCount <= insertIndex)
        {
            Log.LogError("Insert index out of range");
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

    public void InitializeTranslator()
    {
        if (_langMan == null)
        {
            Log.LogError("Language Manager not founded! NULL: Disabling Translator!");
            Destroy(gameObject);
            return;
        }
        else if (_langMan.GetSelectedLanguage() == null)
        {
            Log.LogError("Selected Language not founded! NULL: Disabling Translator!");
            Destroy(gameObject);
            return;
        }

        if (OneTimeInit)
            return;

        Log.LogInfo("Selected Translate: " + _langMan.GetSelectedLanguage());
        Log.LogInfo($"DEVMODE Translate Enabled?: {REPO_Translator_Config.TranslatorDevModeEnabled.Value}");
        TranslateFilePath = Path.GetDirectoryName(PluginInstance.Info.Location);
        Log.LogInfo("TranslateFilePath: " + GetTranslatePath());
        if (REPO_Translator_Config.TranslatorDevModeEnabled.Value)
            Log.LogError("WARNING: YOU HAVE ENABLED DEVMODE TRANSLATOR, DO NOT EDIT THE TRANSLATE FILE BEFORE TURNING OFF THE GAME!!!!");
        AlreadyTranslatedStrings = new Dictionary<string, string>();
        AlreadyTranslatedCodes = new Dictionary<string, string>();
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