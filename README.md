# 🌍 Great REPO Translator

**Great REPO Translator** is a mod for the game **R.E.P.O.**, allowing manual translation of the game into your selected language.  
Based on [REPO_Translator](https://thunderstore.io/c/repo/p/QERT2002/REPO_Translator/) by QERT2002, this version expands the functionality and improves the localization experience.

---

## 🌍 README on other languages

- [EN](https://github.com/Rinary1/GreatREPOTranslator/blob/main/README.md)
- [RU](https://github.com/Rinary1/GreatREPOTranslator/blob/main/README_RU.md)

---

## READ THIS IF YOU TRANSLATE CREATOR

If you create, have created or are going to create your own translation based on my mod, PLEASE add it as an dependency as the author's soul has been put into this mod and it will greatly support the author to keep updating it.

---

## 🚀 Features

🔤 **New XAML attributes for flexible translation**:
- `size` — adjust font size when your translated text is longer than the original.
- `lineSpacing` — set line spacing when the original text lacks breaks.
- `autoSizing` — auto-resize text (enabled by default).
- `autoSizingFontMin` — minimum font size for auto-sizing.
- `part` — partial translation for dynamic strings like `LEVEL 1`, where numbers change.
- `trim` — clears string from unnecessary `\n` and spaces.
- `newLine` — ONLY for `part` translation, adds a `<br>` after translation.

🪵 **Dev Logs** — logs untranslated strings in a readable format, unlike the default dev mode.

♻️ **Hot Reload** — updates localization live when a file is modified or deleted, no need to restart the game.

🌐 **Improved Russian base file** — better translated with helpful comments to assist in creating new translations.

🔎 **Action Bind Tags** - tags like `[jump]` to indicate player's action, which makes translator unbreakable when changing control buttons.

⚙️ **Convenient customization** - instead of editing the cfg file to change the language, just change it in the game settings.

📝 **Custom fonts** - fonts are specially modified to support Cyrillic characters and later other languages.

📁 **Multiple File Extantions** - you can change your translation file extansion to YAML or XAML. 

---

## 🛠 How to Translate the Game

By default, the mod translates the game into **English**.

## ⚙️ How to Change Game Language

Just open the settings and select the language you need in the current localization.

## 🛠 How to Add Your Own Language

### 🔍 Method 1: Manual Translation (Best Quality)

1. Install the mod and run the game once.
2. Close the game.
3. Copy `Translate_RU.xml` from the mod folder.
4. Translate all the lines manually.
5. Save the file and launch the game to test.

`This method does not give 100% results, you also need to upgrade your localization yourself, this is just a “starter” kit.`

### 🤖 Method 2: Automatic String Logging (Faster, Less Accurate)

1. In the config file:
   - Set `TranslatorDevModeEnabled = true`
   - Set `SelectedTranslate = XX` (your language code)
2. Create localization file near .dll with name `Translate_XX.xml`, where XX it's your language code(example: `RU`, `EN`, `UA`).
3. Run the game and explore menus and gameplay for ~10–15 minutes.
3. Close the game.
4. Open localization file what you created. Edit all `translate="..."` values to your language.
5. Disable Dev Mode and enjoy the game in your language!

`The author does not guarantee that the quality of localization using this function will be at the level of the original RU localization, because he made it manually over the game.`

---

## 💾 Installation

1. Install [BepInEx](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)
2. Unzip this mod into the `REPO/BepInEx` folder

📦 Or simply use the Thunderstore Mod Manager for hassle-free installation.

---

## 🤝 Want to Help Translate?

We're considering integration with [Crowdin](https://crowdin.com/) to make collaboration on translations easier.  
If you're interested, feel free to join the discussion on [GitHub](https://github.com/Rinary1/GreatREPOTranslator)!

---

## 🔗 Links

- 💻 GitHub: [Rinary1/GreatREPOTranslator](https://github.com/Rinary1/GreatREPOTranslator)
- 📥 Thunderstore: [Great REPO Translator](https://thunderstore.io/c/repo/p/Rinary/Great_REPO_Translator)