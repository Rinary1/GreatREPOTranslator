using UnityEngine;
using UnityEngine.Events;
using Content.Client.Translator;
using System.Linq;
using SingularityGroup.HotReload;
using System;

namespace Content.Client.MenuSliderElement;

public class MenuSliderLanguage : MonoBehaviour
{
	private MenuSlider? menuSlider;

	public UnityEvent? langEvent;

	private int currentLangCount = 0;

	private static Translator.REPO_Translator _Translator = Translator.REPO_Translator.PluginInstance;

	private static LanguageManager.LanguageManager _langMan = Translator.REPO_Translator._langMan;

	private static REPO_Translator_Config _config = Translator.REPO_Translator.ConfigInstance;

	private void Awake()
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

		if (_langMan == null)
		{
			InitializeLangMan();
		}
		else if (_langMan.GetSelectedLanguage() == null)
		{
			Debug.LogError("Selected Language not founded! NULL: Disabling Translator!");
			return;
		}


		if (_langMan.Languages == null || _langMan.Languages.Count == 0)
		{
			InitializeLangMan();
			if (_langMan.Languages == null || _langMan.Languages.Count == 0)
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

	private void InitializeLangMan()
	{
		_langMan = LanguageManager.LanguageManager.ManagerInstance ?? new LanguageManager.LanguageManager();
		_langMan.InitializeLanguages();
	}

	private void SetOptions()
	{
		if (menuSlider == null)
			return;

		// Clear
		menuSlider.customOptions.Clear();
		menuSlider.hasCustomOptions = true;

		// Find Selected
		if (_langMan == null)
			InitializeLangMan();
		int selectedIndex = 0;
		string selectedLang = _langMan.GetSelectedLanguage();
		Translator.REPO_Translator.Log.LogInfo($"Selected language code: {selectedLang}");

		// Find
		var translations = _Translator.GetAllAvailableTranslations();
		Translator.REPO_Translator.Log.LogInfo($"Found {translations.Count} translations.");

		// Add
		for (int i = 0; i < translations.Count; i++)
		{
			string lang = translations[i];
			Translator.REPO_Translator.Log.LogInfo($"Add translate to tab: {lang}");
			menuSlider.CustomOptionAdd(lang, langEvent);

			if (lang.Equals(selectedLang, StringComparison.OrdinalIgnoreCase))
			{
				selectedIndex = i;
				Translator.REPO_Translator.Log.LogInfo($"Finded selected language code: {lang}");
			}
		}

		foreach (MenuSlider.CustomOption customOption in menuSlider.customOptions)
		{
			customOption.customValueInt = menuSlider.customOptions.IndexOf(customOption);
		}

		if (selectedIndex >= 0 && menuSlider.customOptions.Count > 1)
		{
			float normalizedValue = (float)selectedIndex / (menuSlider.customOptions.Count - 1);
			menuSlider.settingsBar.localScale = new Vector3(normalizedValue, menuSlider.settingsBar.localScale.y, menuSlider.settingsBar.localScale.z);
			menuSlider.SetBar(normalizedValue);
		}

		currentLangCount = translations.Count;
	}
}