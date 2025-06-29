using System.Collections.Generic;
using System.Xml.Serialization;
using Steamworks.ServerList;

namespace Content.Client.Translator;

public class TranslationDataFile
{
    public List<Translate> translations { get; set; } = new();
    public List<DatasetEntry> datasets { get; set; } = new();
}

public abstract class TranslationEntry
{
    public string type { get; set; }
}


public class Translate : TranslationEntry
{
    // Only For YAML, maybe... Font replace on this? Or another things, like external assets for texture translations?
    public Translate()
    {
        type = "Translation";
    }

    [XmlAttribute]
    public string key { get; set; } = "";

    [XmlAttribute]
    public string translate { get; set; } = "";

    [XmlAttribute]
    public float size { get; set; } = 0.0f;

    [XmlAttribute]
    public float lineSpacing { get; set; } = 0.0f;

    [XmlAttribute]
    public bool autoSizing { get; set; } = true;
    
    [XmlAttribute]
    public float autoSizingMax { get; set; } = 0f;

    [XmlAttribute]
    public float autoSizingMin { get; set; } = 0f;

    [XmlAttribute]
    public bool part { get; set; } = false;

    [XmlAttribute]
    public bool trim { get; set; } = false;

    // Only for part
    [XmlAttribute]
    public bool newLine { get; set; } = false;

    [XmlAttribute]
    public bool chatMessage { get; set; } = false;
    
    [XmlAttribute]
    public bool ignoreCase { get; set; } = false;
}

public class DatasetEntry : TranslationEntry
{
    public DatasetEntry()
    {
        type = "Dataset";
    }

    public string key { get; set; } = "";

    public List<string> translations { get; set; } = new List<string>();
}

public class TranslatedTextInfo
{
    public string TranslatedText;
    public Translate? Translation = null;
    public string LanguageCode;

    public TranslatedTextInfo(string translatedText, string languageCode, Translate? translation = null)
    {
        TranslatedText = translatedText;
        Translation = translation;
        LanguageCode = languageCode;
    }
}