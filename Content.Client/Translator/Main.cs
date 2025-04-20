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

namespace Content.Client.Translator;

[HarmonyPatch]
[BepInPlugin("Great_REPO_Translator", "REPO_Translator", "1.2")]
public class REPO_Translator : BaseUnityPlugin
{
    public class Translate
    {
        [XmlAttribute]
        public string key;

        [XmlAttribute]
        public string translate;

        [XmlAttribute]
        public float size = 0.0f;

        [XmlAttribute]
        public float lineSpacing = 0.0f;

        [XmlAttribute]
        public bool autoSizing = true;

        [XmlAttribute]
        public float autoSizingFontMin = 0f;

        [XmlAttribute]
        public bool part = false;

        [XmlAttribute]
        public bool trim = false;
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

    public static List<string> AlreadyTranslatedStrings;

    public static List<Translate> AllTranslates;

    private FileSystemWatcher _configWatcher;

    private Dictionary<string, InputKey> tagDictionary = new Dictionary<string, InputKey>();

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

    private void Start()
    {
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
        Log.LogInfo($"Watching file: Translate_{_langMan.GetSelectedLanguage()}.xml");
        PluginInstance._configWatcher.Filter = "Translate_" + _langMan.GetSelectedLanguage() + ".xml";
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
            AlreadyTranslatedStrings.Clear();

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
                string text = textObject.text;
                UpdateTextMeshProText(textObject, ref text, true);
                textObject.text = text;
                textObject.ForceMeshUpdate();
            }
        }
    }

    private static void UpdateTextMeshProText(TMP_Text textObject, ref string text, bool setText = true)
    {
        if (textObject == null || string.IsNullOrEmpty(text) || textObject.transform.root == null)
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

            string text2 = text;
            Translate translate = AllTranslates?.Find(t => DisplayReplaceTags(t.key) == text2.Trim());
            var parts = AllTranslates?.Where(t => text2.Contains(DisplayReplaceTags(t.key)) && t.part).ToList();

            if (translate != null)
            {
                text2 = DisplayReplaceTags(translate.translate);
                AlreadyTranslatedStrings.Add(text2);

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
                    text2 = text2.Replace(part.key, part.translate);
                    textObject.lineSpacing = part.lineSpacing != 0.0f ? part.lineSpacing : textObject.lineSpacing;
                    textObject.enableAutoSizing = part.autoSizing;
                }
            }
            else if (!AlreadyTranslatedStrings.Contains(text2))
            {
                if (!text2.Any(char.IsDigit) && REPO_Translator_Config.TranslatorDevModeEnabled.Value)
                {
                    var newTranslate = new Translate
                    {
                        key = text2,
                        translate = text2,
                        size = textObject.fontSize
                    };
                    AllTranslates.Add(newTranslate);
                    SaveTranslateData(AllTranslates);
                }
                else if (!IsMessageUnwanted(text2))
                {
                    Log.LogWarning($"WARNING: Untranslated Key: [{text2.Trim()}]");
                }
            }

            if (setText)
                textObject.SetText(text2, true);
            else
                text = text2;
        }
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

    public static string GetTranslatePath()
    {
        return TranslateFilePath + "\\Translate_" + _langMan.GetSelectedLanguage() + ".xml";
    }

    public static List<Translate> GetTranslateData()
    {
        try
        {
            using (FileStream fileStream = new FileStream(GetTranslatePath(), FileMode.Open))
            {
                return (List<Translate>)new XmlSerializer(typeof(List<Translate>)).Deserialize(fileStream);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading Translation XML: {ex.Message}");
            return new List<Translate>();
        }
    }

    public static void SaveTranslateData(List<Translate> dataToSave)
    {
        TextWriter textWriter = new StreamWriter(GetTranslatePath());
        new XmlSerializer(typeof(List<Translate>)).Serialize(textWriter, dataToSave);
        textWriter.Close();
    }

    public static string DisplayReplaceTags(string _text)
    {

        string text = _text;
        foreach (KeyValuePair<string, InputKey> item in PluginInstance.tagDictionary)
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
        string searchPattern = "Translate_*.xml";

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
        {
            return;
        }
        Log.LogInfo("Selected Translate: " + _langMan.GetSelectedLanguage());
        Log.LogInfo($"DEVMODE Translate Enabled?: {REPO_Translator_Config.TranslatorDevModeEnabled.Value}");
        TranslateFilePath = Path.GetDirectoryName(PluginInstance.Info.Location);
        Log.LogInfo("TranslateFilePath: " + GetTranslatePath());
        if (REPO_Translator_Config.TranslatorDevModeEnabled.Value)
        {
            Log.LogError("WARNING: YOU HAVE ENABLED DEVMODE TRANSLATOR, DO NOT EDIT THE TRANSLATE FILE BEFORE TURNING OFF THE GAME!!!!");
        }
        AlreadyTranslatedStrings = new List<string>();
        LoadTranslationsFromFile();
        OneTimeInit = true;
        WatchConfigFile();
    }
}