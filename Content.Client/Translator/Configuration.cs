#nullable disable

using BepInEx.Configuration;
using System;

namespace Content.Client.Translator;

public class REPO_Translator_Config
{
    private readonly ConfigFile _configFile;

    public static ConfigEntry<bool> TranslatorDevModeEnabled;

    public static ConfigEntry<bool> HotReloadEnabled;

    public static ConfigEntry<bool> DevLogEnabled;

    public static ConfigEntry<bool> QuiteModeEnabled;

    public static ConfigEntry<string> TranslatorFileExtansion;

    public static ConfigEntry<string> SelectedTranslate;

    public string SelectedLanguageCode = "EN";

    public REPO_Translator_Config(ConfigFile configFile)
    {
        _configFile = configFile;
    }

    public void RegisterOptions()
    {
        TranslatorDevModeEnabled = _configFile.Bind<bool>("General", "TranslatorDevModeEnabled", false, new ConfigDescription("If enabled, plugin will save new untranslated words in your Translate file (WARNING: Use carefully as ALL lines will be added, some lines do not need to be translated, since they have constantly changing meanings)", (AcceptableValueBase)(object)new AcceptableValueRange<bool>(false, true), Array.Empty<object>()));

        HotReloadEnabled = _configFile.Bind<bool>("General", "HotReload", false, new ConfigDescription("If enabled, plugin will reload selected Translation when you editing translation file.", (AcceptableValueBase)(object)new AcceptableValueRange<bool>(false, true), Array.Empty<object>()));

        DevLogEnabled = _configFile.Bind<bool>("General", "DevLog", false, new ConfigDescription("If enabled, plugin will log all untranslated strings and some dev information.", (AcceptableValueBase)(object)new AcceptableValueRange<bool>(false, true), Array.Empty<object>()));

        QuiteModeEnabled = _configFile.Bind<bool>("General", "QuiteMode", true, new ConfigDescription("If enabled, plugin will log ONLY important information.", (AcceptableValueBase)(object)new AcceptableValueRange<bool>(false, true), Array.Empty<object>()));

        TranslatorFileExtansion = _configFile.Bind<string>("General", "TranslatorFileExtansion", "YML", new ConfigDescription("you can change default file extantion to old(xml). IT WORKS ONLY WITH: YML, XML", (AcceptableValueBase)(object)new AcceptableValueList<string>(new[] { "YML", "XML" }), Array.Empty<object>()));

        SelectedTranslate = _configFile.Bind<string>("General", "SelectedTranslate", "EN", new ConfigDescription("Set the name of what translate file you want to use, example: Translate_RU.yml -> RU / Translate_YourLangCode.yml -> YourLangCode", (AcceptableValueBase)null, Array.Empty<object>()));
    }
}