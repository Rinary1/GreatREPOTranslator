using UnityEngine;
using UnityEngine.Events;
using Content.Client.Translator;
using System.Linq;
using SingularityGroup.HotReload;
using System;
using LogType = Content.Client.LogManager.LogType;

namespace Content.Client.MenuSliderElement;

public class MenuSliderLanguage : MonoBehaviour
{
	private MenuSlider? menuSlider;

	public UnityEvent? langEvent;

	private int currentLangCount = 0;

	private static Translator.REPO_Translator _Translator = Translator.REPO_Translator.PluginInstance;

	private static LanguageManager.LanguageManager _langMan = Translator.REPO_Translator._langMan;

	private static LogManager.LogManager _logMan = Translator.REPO_Translator._logMan;

	private static REPO_Translator_Config _config = Translator.REPO_Translator.ConfigInstance;

	public void Start()
	{
		menuSlider = GetComponent<MenuSlider>();
		SetOptions();
	}

	private void Update()
	{
		if (menuSlider == null)
			return;

		if (currentLangCount != _Translator.GetAvailableTranslationsCount())
		{
			SetOptions();
			return;
		}

		if (_langMan == null || _langMan.Languages == null || _langMan.Languages.Count == 0)
		{
			_langMan = LanguageManager.LanguageManager.ManagerInstance ?? new LanguageManager.LanguageManager();
			_langMan.InitializeLanguages();
			if (_langMan.Languages == null || _langMan.Languages.Count == 0)
				return;
		}
		else if (_langMan.GetSelectedLanguage() == null)
		{
			Debug.LogError("Selected Language not founded! NULL: Disabling Translator!");
			return;
		}

		var selectedSliderLang = menuSlider.customOptions[menuSlider.currentValue].customOptionText;

		if (string.IsNullOrEmpty(selectedSliderLang))
			return;

		if (_langMan.GetSelectedLanguage() != selectedSliderLang)
		{
			_config.SelectedLanguageCode = selectedSliderLang;
			REPO_Translator_Config.SelectedTranslate.Value = selectedSliderLang;
			_langMan.SaveLanguage();
			Translator.REPO_Translator.ReloadTranslations();
		}
	}

	private void SetOptions()
	{
		if (menuSlider == null)
			return;

		// Clear
		menuSlider.customOptions.Clear();
		menuSlider.hasCustomOptions = true;
		menuSlider.hasCustomValues = true;

		// Find Selected
		if (_langMan == null)
		{
			_langMan = LanguageManager.LanguageManager.ManagerInstance ?? new LanguageManager.LanguageManager();
			_langMan.InitializeLanguages();
		}

		string selectedLang = _langMan.GetSelectedLanguage();
		int selectedIndex = 0;

		// Find
		var translations = _Translator.GetAllAvailableTranslations();
		_logMan.TryLog($"Found {translations.Count} translations.", LogType.Info);

		// Add
		for (int i = 0; i < translations.Count; i++)
		{
			string lang = translations[i];
			int index = i;

			menuSlider.CustomOptionAdd(lang, langEvent);
			menuSlider.customOptions[i].customValueInt = i;
			_logMan.TryLog($"Add translate to tab: {lang}", LogType.Info);

			if (lang.Equals(selectedLang, StringComparison.OrdinalIgnoreCase))
			{
				selectedIndex = i;
				_logMan.TryLog($"Finded selected language code: {lang}", LogType.Info);
			}
		}

		if (selectedIndex >= 0 && menuSlider.customOptions.Count > 1)
		{
			float normalizedValue = (float)selectedIndex / (menuSlider.customOptions.Count - 1);
			menuSlider.SetBar(normalizedValue);
			menuSlider.SetBarScaleInstant();
			menuSlider.UpdateSegmentTextAndValue();
		}

		currentLangCount = translations.Count;
	}
}