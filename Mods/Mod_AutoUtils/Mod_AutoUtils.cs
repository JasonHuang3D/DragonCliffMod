using UnityModManagerNet;
using Harmony12;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System;


namespace Mod_AutoUtils
{
    public class Settings : UnityModManager.ModSettings
    {
        public bool autoQuestEnabled = false;
      
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

    }

    public static class Main
    {
        public static Settings settings;
        public static UnityModManager.ModEntry.ModLogger Logger;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            settings = Settings.Load<Settings>(modEntry);

            Logger = modEntry.Logger;

            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUIStyle txtFieldStyle = GUI.skin.textField;
            txtFieldStyle.alignment = TextAnchor.MiddleCenter;

            GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();

                    settings.autoQuestEnabled = GUILayout.Toggle(settings.autoQuestEnabled, "Toggle Auto Quest", new GUILayoutOption[0]);
                    GUILayout.Space(10);

                  
                   

            GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                    GUILayout.Label("JasonHuang<616267056@qq.com>");
                GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

    }


    [HarmonyPatch(typeof(QuestMenuController), "TryUpdate")]
    public static class QuestMenuController_TryUpdate_Patch
    {
        private static float questUpdateTimer = 0.0f;
        private static float adventureWaitTimer = 0.0f;

        public static AutoAdventureState currentState = AutoAdventureState.Finished;

        private static void updateQuest(QuestMenuController __instance)
        {

            if (GameWorld.instance.GetCurrentAdventure() != null || currentState != AutoAdventureState.Finished) return;

            var questList = GameWorld.instance.PlayerProfile.GetProgress(null).Quests;
            foreach (var quest in questList)
            {
                if (quest.Completed)
                {
                    //Main.Logger.Log($"Compelete Quest: {quest.GetDescription().Title}");
                    GameWorld.instance.PlayerProfile.CompleteQuest(quest);
                    __instance.CompleteQuestPanel.gameObject.SetActive(false);
                    return;
                }
            }

            var questRequirements = GameWorld.instance.PlayerProfile.GetProgress(null).Quests.SelectMany((Quest q) => from qe in q.QuestRequirements
                                                                                                                      where qe.CorrespondingQuestRequirementType == QuestRequirementType.DungeonCompletion || qe.CorrespondingQuestRequirementType == QuestRequirementType.CustomizedDungeonHuntRequirement
                                                                                                                      select qe).ToList<QuestRequirementBase>();

            AdventureType type = AdventureType.None;
            int level = -1;

            foreach (QuestRequirementBase questRequirementBase in questRequirements)
            {

                if (questRequirementBase is DungeonCompletionRequirementLogic)
                {
                    var dungeonCompletionRequirementLogic = questRequirementBase as DungeonCompletionRequirementLogic;
                    type = dungeonCompletionRequirementLogic.DungeonType;
                    level = dungeonCompletionRequirementLogic.LevelNumber;
                }
                else if (questRequirementBase is DungeonExplorationRequirementLogic)
                {
                    var dungeonExplorationRequirementLogic = questRequirementBase as DungeonExplorationRequirementLogic;
                    type = dungeonExplorationRequirementLogic.DungeonType;
                    level = dungeonExplorationRequirementLogic.LevelNumber;
                }
                else if (questRequirementBase is CustomizedDungeonThroughRequirementLogic)
                {
                    var customizedDungeonThroughRequirementLogic = questRequirementBase as CustomizedDungeonThroughRequirementLogic;
                    type = customizedDungeonThroughRequirementLogic.DungeonType;
                    level = customizedDungeonThroughRequirementLogic.Configuration.LevelNumber;
                }

                if (type == AdventureType.TwistedPalace) break;
            }

            if (type != AdventureType.None && level != -1)
            {
                //Main.Logger.Log($"find quest: {type.ToString()} {level.ToString()}");
                AutoAdventureController.Instance.StopAutoAdventure();

                var worldMap = TownManager.Instance.Ui.WorldMap;

                if (type == AdventureType.TwistedPalace) type = AdventureType.Special;

                worldMap.SelectMap(type);
                worldMap.SelectLevelFromChessLevelItem(type, level);
                currentState = GameWorld.instance.GetCurrentAdventure() != null ? AutoAdventureState.InBattle : AutoAdventureState.Finished;
            }
        }
        public static void Postfix(QuestMenuController __instance)
        {
            if (!Main.settings.autoQuestEnabled)
            {
                questUpdateTimer = 0.0f;
                adventureWaitTimer = 0.0f;
                currentState = AutoAdventureState.Finished;
                return;
            }

            questUpdateTimer += Time.deltaTime;

            if (questUpdateTimer >= 10.0f)
            {
                questUpdateTimer = 0.0f;
                updateQuest(__instance);
            }


            if (currentState == AutoAdventureState.WaitToOpenChest)
            {
                BattleManager.instance.Spawner.AutoSelectChest();
                currentState = AutoAdventureState.OnRewardPanel;
            }
            if (currentState == AutoAdventureState.OnRewardPanel)
            {
                adventureWaitTimer += Time.deltaTime;
                if (adventureWaitTimer >= AutoAdventureController.Instance.OnRewardPanelWaitingTime)
                {
                    currentState = AutoAdventureState.Finished;
                    adventureWaitTimer = 0.0f;
                    BattleManager.instance.CompeletionPanelConfirmButton();
                }
            }
            if (currentState == AutoAdventureState.Failed)
            {
                adventureWaitTimer += Time.deltaTime;
                if (adventureWaitTimer >= AutoAdventureController.Instance.OnRewardPanelWaitingTime / 3.0f)
                {
                    currentState = AutoAdventureState.Finished;
                    adventureWaitTimer = 0.0f;
                    BattleManager.instance.CompeletionPanelConfirmButton();
                }
            }

        }
    }

    [HarmonyPatch(typeof(AutoAdventureController), "WaitToOpenChest")]
    public static class AutoAdventureController_WaitToOpenChest_Patch
    {
        public static void Postfix(AutoAdventureController __instance)
        {
            if (!Main.settings.autoQuestEnabled) return;

            QuestMenuController_TryUpdate_Patch.currentState = AutoAdventureState.WaitToOpenChest;
        }
    }

    [HarmonyPatch(typeof(AutoAdventureController), "AdventureFailed")]
    public static class AutoAdventureController_AdventureFailed_Patch
    {
        public static void Postfix(AutoAdventureController __instance)
        {
            if (!Main.settings.autoQuestEnabled) return;

            QuestMenuController_TryUpdate_Patch.currentState = AutoAdventureState.Failed;
        }
    }

    [HarmonyPatch(typeof(AutoAdventureController), "StopShowingBattleScene")]
    public static class AutoAdventureController_StopShowingBattleScene_Patch
    {
        private static bool showed = false;

        public static bool new_StopShowingBattleScene()
        {
            if (Main.settings.autoQuestEnabled && showed)
            {
                return true;
            }

            showed = true;

            var _this = Traverse.Create(AutoAdventureController.Instance);

            return AutoAdventureController.Instance.AutoAdventure != null && AutoAdventureController.Instance.AutoAdventure.IsOn && ((int)_this.Field("_numberOfAutoBattle").GetValue() > 0);
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int startIndex = 0;

            var injectedCodes = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Call,typeof(AutoAdventureController_StopShowingBattleScene_Patch).GetMethod("new_StopShowingBattleScene")),
                new CodeInstruction(OpCodes.Ret)
            };

            codes.InsertRange(startIndex, injectedCodes);

            return codes.AsEnumerable();
        }
    }


   
}

