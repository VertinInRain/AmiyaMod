using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace Amiya.Patches;

/// <summary>
/// 多级升级卡（MaxUpgradeLevel &gt; 1）在牌组检视界面（NInspectCardScreen）的「查看升级」交互：
///   点击不直接打勾，而是直接跳到下一级升级后的版本；
///   到最高升级档时才打勾；再点一次取消打勾、完全从头再来。
/// 非多级卡保持原版勾选行为。
/// </summary>
[HarmonyPatch(typeof(NInspectCardScreen), "UpdateCardDisplay")]
internal static class AmiyaMultiUpgradePreviewPatch
{
    private static int _cycle;                 // 当前预览档位（0 = 原样；1..max = 升 N 级预览）
    private static CardModel? _currentCard;

    private static readonly FieldInfo _cardsField =
        AccessTools.Field(typeof(NInspectCardScreen), "_cards");
    private static readonly FieldInfo _indexField =
        AccessTools.Field(typeof(NInspectCardScreen), "_index");
    private static readonly FieldInfo _cardField =
        AccessTools.Field(typeof(NInspectCardScreen), "_card");

    public static void ResetCycle()
    {
        _cycle = 0;
        _currentCard = null;
    }

    /// <summary>点击「查看升级」时推进/重置档位，并接管勾选框状态（IsTicked 赋值不触发 Toggled，安全）。</summary>
    public static void OnUpgradeToggle(NInspectCardScreen instance, NTickbox tickbox)
    {
        try
        {
            CardModel? card = CurrentCard(instance);
            if (card == null || card.MaxUpgradeLevel <= 1)
            {
                return;
            }
            if (!ReferenceEquals(card, _currentCard))
            {
                _currentCard = card;
                _cycle = 0;
            }
            int maxLevels = Math.Max(1, card.MaxUpgradeLevel - card.CurrentUpgradeLevel);
            // 原版勾选框每次点击都会翻转自身状态，所以前缀里看到的是翻转后的状态：
            // 勾上 = 本次点击前未勾 → 推进一级；取消勾 = 点击前已勾（即之前在最高档）→ 从头。
            if (tickbox.IsTicked)
            {
                _cycle++;
                if (_cycle >= maxLevels)
                {
                    _cycle = maxLevels;
                    // 最高档：保持打勾
                }
                else
                {
                    tickbox.IsTicked = false; // 中间档不打勾
                }
            }
            else
            {
                _cycle = 0; // 完全从头
            }
        }
        catch (Exception ex)
        {
            Log.Error("[Amiya] multi upgrade preview tick failed: " + ex);
        }
    }

    private static CardModel? CurrentCard(NInspectCardScreen instance)
    {
        if (_cardsField.GetValue(instance) is List<CardModel> cards)
        {
            int index = (int)(_indexField.GetValue(instance) ?? 0);
            if (index >= 0 && index < cards.Count)
            {
                return cards[index];
            }
        }
        return null;
    }

    private static void Postfix(NInspectCardScreen __instance)
    {
        try
        {
            CardModel? baseCard = CurrentCard(__instance);
            NCard? cardNode = _cardField.GetValue(__instance) as NCard;
            if (baseCard == null || cardNode == null || baseCard.MaxUpgradeLevel <= 1)
            {
                return;
            }
            if (!ReferenceEquals(baseCard, _currentCard))
            {
                _currentCard = baseCard;
                _cycle = 0;
            }
            if (_cycle <= 0)
            {
                return; // 原版已按普通形态渲染
            }
            int levels = Math.Clamp(_cycle, 1, Math.Max(1, baseCard.MaxUpgradeLevel - baseCard.CurrentUpgradeLevel));
            var clone = (CardModel)baseCard.MutableClone();
            clone.UpgradePreviewType = CardUpgradePreviewType.Deck;
            for (int i = 0; i < levels; i++)
            {
                clone.UpgradeInternal();
            }
            // 展示克隆的 RunState 为空，游戏会跳过 UpdateDynamicVarPreview（数值不会刷新）。
            // 手动刷新每个变量：UpgradeLevelVar/UpgradeLevelTextVar 按克隆的等级重算 PreviewValue/StringValue。
            foreach (DynamicVar v in clone.DynamicVars.Values)
            {
                v.UpdateCardPreview(clone, CardPreviewMode.Upgrade, null, false);
            }
            // 【临时诊断】打印克隆等级与 V/W 变量值，便于定位"数值不变"问题
            decimal? vv = null;
            decimal? ww = null;
            if (clone.DynamicVars.TryGetValue("V", out var vVar))
            {
                vv = vVar.PreviewValue;
            }
            if (clone.DynamicVars.TryGetValue("W", out var wVar))
            {
                ww = wVar.PreviewValue;
            }
            Log.Info($"[Amiya] preview cycle={_cycle} levels={levels} cloneLevel={clone.CurrentUpgradeLevel} V={vv} W={ww}");
            cardNode.Model = clone;
            cardNode.ShowUpgradePreview();
        }
        catch (Exception ex)
        {
            Log.Error("[Amiya] multi upgrade preview failed: " + ex);
        }
    }
}

/// <summary>点击「查看升级」：先接管档位/勾选状态，再让原版渲染。</summary>
[HarmonyPatch(typeof(NInspectCardScreen), "ToggleShowUpgrade")]
internal static class AmiyaMultiUpgradePreviewTickPatch
{
    // 注意：原方法的参数名是 "_"，Harmony 按参数名匹配，这里也必须叫 "_"
    private static void Prefix(NInspectCardScreen __instance, NTickbox _)
    {
        AmiyaMultiUpgradePreviewPatch.OnUpgradeToggle(__instance, _);
    }
}

/// <summary>打开/切换检视卡片时重置预览档位。</summary>
[HarmonyPatch(typeof(NInspectCardScreen), "SetCard")]
internal static class AmiyaMultiUpgradePreviewResetPatch
{
    private static void Postfix()
    {
        AmiyaMultiUpgradePreviewPatch.ResetCycle();
    }
}
