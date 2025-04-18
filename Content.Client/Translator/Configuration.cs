#nullable disable

using BepInEx.Configuration;
using System;

namespace Content.Client.Translator;

public class REPO_Translator_Config
{
    private readonly ConfigFile _configFile;

    public static ConfigEntry<bool> TranslatorDevModeEnabled;

    public static ConfigEntry<string> SelectedTranslate;

    public string SelectedLanguageCode = "EN";

    public REPO_Translator_Config(ConfigFile configFile)
    {
        _configFile = configFile;
    }

    public void RegisterOptions()
    {
        TranslatorDevModeEnabled = _configFile.Bind<bool>("General", "TranslatorDevModeEnabled", false, new ConfigDescription("If enabled, plugin will save new untranslated words in your Translate file (WARNING: Use carefully as ALL lines will be added, some lines do not need to be translated, since they have constantly changing meanings)", (AcceptableValueBase)(object)new AcceptableValueRange<bool>(false, true), Array.Empty<object>()));

        SelectedTranslate = _configFile.Bind<string>("General", "SelectedTranslate", "EN", new ConfigDescription("Set the name of what translate file you want to use, example: Translate_RU.xml -> RU / Translate_YourLangCode.xml -> YourLangCode", (AcceptableValueBase)null, Array.Empty<object>()));
    }
}