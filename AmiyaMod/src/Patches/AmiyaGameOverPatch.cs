using System;
using Amiya.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;

namespace Amiya.Patches;

/// <summary>
/// 结算画面死亡立绘：阿米娅在某形态被击杀后，把 GameOver 屏幕的
/// %CreatureContainer 里的生物形象替换为该形态的死亡图片（全幅、保持比例覆盖）。
/// </summary>
[HarmonyPatch(typeof(NGameOverScreen), "_Ready")]
internal static class AmiyaGameOverPatch
{
    private static void Postfix(NGameOverScreen __instance)
    {
        try
        {
            AmiyaForm? form = FormManagerPower.AmiyaDeathForm;
            if (form == null)
            {
                return;
            }
            Control? container = __instance.GetNodeOrNull<Control>("%CreatureContainer");
            if (container == null)
            {
                Log.Error("[Amiya] game over: %CreatureContainer not found");
                return;
            }
            string name = form switch
            {
                AmiyaForm.Guard => "guard",
                AmiyaForm.Caster => "caster",
                AmiyaForm.Medic => "medic",
                _ => "demon"
            };
            Texture2D? tex = ResourceLoader.Load<Texture2D>("res://Amiya/images/death/" + name + ".png");
            if (tex == null)
            {
                Log.Error("[Amiya] game over: death image load failed for " + name);
                return;
            }
            foreach (Node child in container.GetChildren())
            {
                child.QueueFree();
            }
            var rect = new TextureRect
            {
                Texture = tex,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            container.AddChild(rect);
            Log.Info("[Amiya] game over: death image shown for " + name);
        }
        catch (Exception ex)
        {
            Log.Error("[Amiya] game over patch failed: " + ex);
        }
    }
}
