using System.Collections.Generic;
using Content.Client.Translator;
using HarmonyLib;
using UnityEngine;
using static ChatManager;

namespace Content.Client.ChatManagerTranslate;

public class ChatManagerTranslate
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ChatManager), "TumbleInterruption")]
    public static bool ChatManager_TumbleInterruption(ChatManager __instance)
    {
        List<DatasetEntry> Datasets = Translator.REPO_Translator.AllDatasets;

        DatasetEntry found = Datasets.Find(t => t.key == "TumbleInterruption");

        if (found != null)
            if (!(__instance.activePossessionTimer > 0f))
            {
                __instance.PossessionReset();
                if ((bool)__instance.playerAvatar && __instance.playerAvatar.voiceChatFetched && __instance.playerAvatar.voiceChat.ttsVoice.isSpeaking)
                {
                    List<string> list = found.translations;
                    int index = Random.Range(0, list.Count);
                    string message = list[index];
                    __instance.PossessChatScheduleStart(3);
                    __instance.PossessChat(PossessChatID.Ouch, message, 1f, Color.red);
                    __instance.PossessChatScheduleEnd();
                    return false;
                }
                else
                    return true;
            }
            else
                return true;
        else
            return true;
    }
}