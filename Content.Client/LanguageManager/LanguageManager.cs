using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Content.Client.Translator;
using SingularityGroup.HotReload;

namespace Content.Client.LanguageManager;

public class LanguageManager
{
    // Manager Instance
    public static LanguageManager ManagerInstance { get; internal set; }

    // Default Language Code
    public static string DefaultLanguageCode = "EN";

    // Language code : Language Name
    public Dictionary<string, string> Languages = new Dictionary<string, string>();

    private static REPO_Translator_Config _config = Translator.REPO_Translator.ConfigInstance;

    private void Awake()
    {
        ManagerInstance = this;
        Debug.Log($"[Translator Init] LanguageManager Initialized");
        InitializeLanguages();
        LoadLanguage();
    }

    public void InitializeLanguages()
    {
        Debug.Log($"[Translator Init] PluginInstance: {Translator.REPO_Translator.PluginInstance}");

        var translations = Translator.REPO_Translator.PluginInstance.GetAllAvailableTranslations();

        if (translations == null)
        {
            Debug.LogError("[Translator Init] GetAllAvailableTranslations() returned null!");
            return;
        }

        if (Languages == null)
        {
            Debug.LogError("[Translator Init] Languages dictionary is null!");
            return;
        }

        foreach (var language in translations)
        {
            try
            {
                CultureInfo culture = new CultureInfo(language);
                Languages.Add(language, culture.EnglishName);
            }
            catch (CultureNotFoundException)
            {
                Translator.REPO_Translator.Log.LogError($"Language '{language}' not founded as real.");
            }
        }
    }

    public string GetSelectedLanguage()
    {
        if (!string.IsNullOrEmpty(_config.SelectedLanguageCode))
        {
            return REPO_Translator_Config.SelectedTranslate.Value;
        }
        else
        {
            Translator.REPO_Translator.Log.LogWarning("SELECTED LANGUAGE NOT FOUNDED, RETURNED EN");
            return "EN";
        }
    }
    public void LoadLanguage()
    {
        try
        {
            var SelectedLanguage = REPO_Translator_Config.SelectedTranslate.Value;
            if (string.IsNullOrEmpty(SelectedLanguage))
            {
                Translator.REPO_Translator.Log.LogWarning("Loaded language is null or empty, falling back to EN.");
                REPO_Translator_Config.SelectedTranslate.Value = "EN";
            }
        }
        catch (Exception ex)
        {
            Translator.REPO_Translator.Log.LogError("Failed to load language: " + ex.Message);
            SaveLanguage();
        }
    }

    public void SaveLanguage()
    {
        REPO_Translator_Config.SelectedTranslate.Value = _config.SelectedLanguageCode;
    }
}