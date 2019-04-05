using UnityModManagerNet;
using Harmony12;
using System.Reflection;
using UnityEngine;

namespace Mod_Display
{
    public class Settings : UnityModManager.ModSettings
    {

        public bool previewAmuletEnalbled = false;
        //public bool displayEnemyDetailsEnabled = false;

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

            settings.previewAmuletEnalbled = GUILayout.Toggle(settings.previewAmuletEnalbled, "Toggle Preview Amulet", new GUILayoutOption[0]);
            GUILayout.Space(10);

            //settings.displayEnemyDetailsEnabled = GUILayout.Toggle(settings.displayEnemyDetailsEnabled, "Toggle Preview Enemy Details", new GUILayoutOption[0]);
            //GUILayout.Space(10);

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

    public static class PreviewAmulet
    {
        public static TooltipItem GetToolTip(Item item, TooltipItem result = null)
        {
            if (!Main.settings.previewAmuletEnalbled) return result;

            var resourceCategory = item.Type.GetResourceCategory();

            if (resourceCategory != ResourceCategory.Amulet) return result;

            var text = string.Empty;

            var teamSet = item.Type.GetTeamSetBase();
            var originalLevel = item.Level;


            int maxLevel = 100;
            while (item.Level < maxLevel)
            {
                teamSet.Upgrade(item, item.Level + 1);
            }


            foreach (var attributeModifier in item.GetAttributeModifiers())
            {
                string tempText = text;
                text = string.Concat(new string[]
                {
                    tempText,
                    "* ",
                    attributeModifier.AttributeType.GetDescription().Title,
                    " ",
                    (attributeModifier.Value <= 0.0) ? string.Empty : "+ ",
                    attributeModifier.GetDisplayValue().ToDisplayValueFormat().ToColor(ColorPicker.Grey),
                    "\n"
                });
            }
            foreach (ISpecialEffectDataLoad specialEffectDataLoad in item.GetSpecialEffects())
            {
                text += "* " + ColorPicker.GetHaxString(ColorPicker.Grey, specialEffectDataLoad.GetDescription().Details1 + "\n");
            }

            teamSet.Upgrade(item, 0);
            while (item.Level < originalLevel)
            {
                teamSet.Upgrade(item, item.Level + 1);
            }

            var title = $"Level:{maxLevel} Potential:\n";
            if (result == null)
            {
                result = new TooltipItem
                {
                    Title = title,
                    Description = text,
                    TitleColor = ColorPicker.White
                };
            }
            else
            {
                result.Description += title + text;
            }

            return result;
        }
    }

    [HarmonyPatch(typeof(ItemController), "OnPointerEnter")]
    public static class ItemController_OnPointerEnter_Patch
    {
        public static bool Prefix(ItemController __instance)
        {
            if (__instance.NormalItem == null) return false;

            var secondToolTip = PreviewAmulet.GetToolTip(__instance.NormalItem.Item,
                                                         null);

            __instance.OpenTooltip(__instance.SetupTooltipItem(), secondToolTip, TooltipPosition.None, 0f, 0f);

            return false;
        }
    }

    [HarmonyPatch(typeof(EquipmentItemController), "OnPointerEnter")]
    public static class EquipmentItemController_OnPointerEnter_Patch
    {
        public static bool Prefix(EquipmentItemController __instance)
        {
            if (__instance.NormalItem == null) return false;


            var secondToolTip = PreviewAmulet.GetToolTip(__instance.NormalItem.Item,
                                                        __instance.NormalItem.GetInventoryEquipmentSecondTooltip());

            __instance.OpenTooltip(__instance.SetupTooltipItem(), secondToolTip, TooltipPosition.None, 0f, 0f);

            return false;
        }
    }

    [HarmonyPatch(typeof(EquipedItemController), "OnPointerEnter")]
    public static class EquipedItemController_OnPointerEnter_Patch
    {
        public static bool Prefix(EquipedItemController __instance)
        {
            if (__instance.NormalItem == null) return false;

            var componentInParent = __instance.GetComponentInParent<WorldMapHeroDetailsController>();
            var componentInParent2 = __instance.GetComponentInParent<HeroMenuController>();
            var resourceCategory = __instance.NormalItem.Item.Type.GetResourceCategory();


            if (componentInParent != null)
            {
                if (resourceCategory != ResourceCategory.Amulet && resourceCategory != ResourceCategory.Device)
                {
                    __instance.OpenTooltip(__instance.SetupTooltipItem(), componentInParent.GetGemSetTooltip(), TooltipPosition.None, 0f, 0f);
                }
                else
                {
                    __instance.OpenTooltip(__instance.SetupTooltipItem(), PreviewAmulet.GetToolTip(__instance.NormalItem.Item, null), TooltipPosition.None, 0f, 0f);
                }
            }
            else if (componentInParent2 != null)
            {
                if (resourceCategory != ResourceCategory.Amulet && resourceCategory != ResourceCategory.Device)
                {
                    __instance.OpenTooltip(__instance.SetupTooltipItem(), componentInParent2.InventoryPanel.GetGemSetTooltip(), TooltipPosition.None, 0f, 0f);
                }
                else
                {
                    __instance.OpenTooltip(__instance.SetupTooltipItem(), PreviewAmulet.GetToolTip(__instance.NormalItem.Item, null), TooltipPosition.None, 0f, 0f);
                }
            }
            return false;
        }
    }


    [HarmonyPatch(typeof(JustDroppedItemController), "OnPointerEnter")]
    public static class JustDroppedItemController_OnPointerEnter_Patch
    {
        public static bool Prefix(JustDroppedItemController __instance)
        {
            if (__instance.NormalItem == null) return false;

            var resourceCategory = __instance.NormalItem.Item.Type.GetResourceCategory();

            if (resourceCategory == ResourceCategory.Amulet)
            {
                __instance.OpenTooltip(__instance.SetupTooltipItem(),
                    PreviewAmulet.GetToolTip(__instance.NormalItem.Item, __instance.NormalItem.GetInventoryEquipmentSecondTooltip()),
                    TooltipPosition.None, 0f, 0f);
                return false;
            }
            
            return true;
        }
    }

}
