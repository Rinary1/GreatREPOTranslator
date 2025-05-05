using System;
using BepInEx.Logging;
using Content.Client.Translator;

namespace Content.Client.LogManager;

public class LogManager
{
    // Manager Instance
    public static LogManager ManagerInstance { get; internal set; }

    public static ManualLogSource LogInstance = Logger.CreateLogSource("Great Repo Translator");

    private void Awake()
    {
        ManagerInstance = this;
    }

    public bool TryLog(string? message, LogType type)
    {
        if (message == null)
            return false;
        try
        {
            Log(message, type);
            return true;
        }
        catch (ArgumentException ex)
        {
            LogInstance.LogError(ex.Message);
            return false;
        }
    }

    public void Log(string message, LogType type)
    {
        switch (type)
        {
            case LogType.Debug:
                if (REPO_Translator_Config.DevLogEnabled.Value)
                    LogInstance.LogDebug(message);
                break;
            case LogType.Warning:
                if (REPO_Translator_Config.DevLogEnabled.Value)
                    LogInstance.LogWarning(message);
                break;
            case LogType.Error:
                if (!REPO_Translator_Config.QuiteModeEnabled.Value)
                    LogInstance.LogError(message);
                break;
            case LogType.Fatal:
                LogInstance.LogFatal(message);
                break;
            case LogType.Info:
                if (!REPO_Translator_Config.QuiteModeEnabled.Value)
                    LogInstance.LogInfo(message);
                break;
            case LogType.Regular:
                if (!REPO_Translator_Config.QuiteModeEnabled.Value)
                    LogInstance.LogMessage(message);
                break;
            default:
                throw new ArgumentException("AH! I DON'T KNOW WHY YOU LOG NON AVAILABLE TYPE!");
        }
    }
}

public enum LogType
{
    Debug, // Only if DevLogEnabled == true
    Warning, // Only if DevLogEnabled == true
    Error, // When Ignore Non Fatal Errors(Quiet mode) == false
    Fatal, // Always
    Info, // When Ignore Non Fatal Errors(Quiet mode) == false
    Regular // When Ignore Non Fatal Errors(Quiet mode) == false
}