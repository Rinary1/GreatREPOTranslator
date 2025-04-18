# Great REPO Translator
A mod for R.E.P.O. that can apply manual translation to the game on selected language, this mod based on realisation of QERT2002 and his mod [REPO_Translator](https://thunderstore.io/c/repo/p/QERT2002/REPO_Translator/).

Distinctions:

- New XAML Attributes for better translation, like:
  - size: changing font size for languages phrases that far outnumber the original
  - lineSpacing: for times when a line break is required but the original text does not contain lineSpacing
  - autoSizing: if you have not resized your text, it is enabled by default, this field is necessary for automatic resizing of the text, it may not be necessary in some places or on the contrary interfere (example: List of player's improvements, there it infinitely reduces the text).
  - autoSizingFontMin: Attribute for autoSizing, which sets the minimum text size.
  - part: this attribute is designed for PARTIAL string translation, for places where it is not possible to provide a precise key, like: LEVEL 1(Level number infinitely variable).
- Dev logs: Added a logging for untranslated text, is a replacement for the standard dev mode, since it just writes all strings in a row without filtering them out.
- Hot Reload(File watcher): This solution updates the localization in active game if the localization file has been deleted or modified. Perhaps later also will be added language selection within the game, which will be based on this.
- Basic Language file(Translate_RU) has been better translated, added comments for translation on another languages.

## MANUAL TRANSLATE

Initially, the plugin translates the game into Russian language.

But if you want, you can translate it into almost any language following these instructions:
- Install the mod and run game one time.
- Close your game.

At this point, you have two choices:

- Manual Translation (Better Translation): you open the root folder of the mod, copy the basic localization file and manually translate all the lines, then go into the game and check the operability, while you also need to look for not translated lines yourself
- Automatic Translation (Bad): pretty much the same, except you won't manually search for untranslated strings, the consequences of that could be:
  - Technical string captures
  - Capturing strings that may not be translatable (Example: TAXMAN, other names).

How To Enable Automatic String Record:

- Go to config and change TranslatorDevModeEnabled to true.
- Go to config and change SelectedTranslate to abbreviation of your language.
- Save the config file and run game.
- Play the game for 10-15 minutes, trying to get all possible variations of the text, press the buttons in the menu, start up and wander around a little, aim at different objects.
- Close the game.
- Go to plugins/REPO_Translator/ folder, in it you will see the file Translate_yourlang.xml
- Open the file, in it you will see many lines like key="blabla" translate="blabla"
- Change the translate="blabla" into your own language on every line, example: <Translate key="Start Game" translate="Start game" /> -> <Translate key="Start Game" translate="Начать игру" />
- After that, save the file and open the game.
- You will see that all the lines that you translated are displayed in your language.
- If there are some lines left without translation, check your file, perhaps you forgot to translate them, if there is no line in the file - alas, most likely it is a picture or dynamic text.
- When you are happy with the result, close the game, open the config and set TranslatorDevModeEnabled to false and now you can play the game in your favorite language.
- If you want, you can send the translation file with the config file to your friends and play together.

## Installation

- Install [BepInEx](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)
- Unzip this mod into your `REPO/BepInEx` folder

Or use the thunderstore mod manager to handle the installing for you.

## Configuration

- In the BepInEx config folder, you can find com.github.qert2002.REPO_Translator.cfg and enable or disable DEVMODE