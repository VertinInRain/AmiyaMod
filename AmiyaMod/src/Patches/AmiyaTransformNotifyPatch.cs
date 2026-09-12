using System;
using System.Threading.Tasks;
using Amiya.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Patches;

/// <summary>
/// 变形通知：官方没有 AfterCardTransformed 钩子，这里给 CardCmd.Transform 加异步后置补丁，
/// 变形完成后通知 拒绝哀悼 / 终焉之影（"每消耗或变化一张牌"的"变化"）。
/// </summary>
[HarmonyPatch(typeof(CardCmd), "Transform", new Type[] { typeof(CardModel), typeof(CardModel), typeof(CardPreviewStyle) })]
internal static class AmiyaTransformNotifyPatch
{
    [HarmonyPostfix]
    private static async Task<CardPileAddResult?> Postfix(Task<CardPileAddResult?> __result, CardModel original)
    {
        CardPileAddResult? res = await __result;
        await AmiyaTransformNotify.OnCardTransformed(original);
        return res;
    }
}

/// <summary>拒绝哀悼 / 终焉之影 的"变化"结算入口。</summary>
internal static class AmiyaTransformNotify
{
    public static async Task OnCardTransformed(CardModel original)
    {
        try
        {
            Creature? owner = original.Owner?.Creature;
            if (owner == null || owner.CombatState == null)
            {
                return;
            }
            var context = new ThrowingPlayerChoiceContext();
            if (owner.GetPower<Amiya.Cards.RefuseMournPower>() is { } refuse)
            {
                await DealDamageToAllEnemies(context, owner, refuse.Amount);
            }
            if (owner.GetPower<Amiya.Cards.FinalShadowPower>() != null && FormManagerPower.Current != null)
            {
                await FormManagerPower.Current.GainDemonLordCall(context, original, null);
            }
        }
        catch (Exception)
        {
            // 变形通知失败不阻断变形本身
        }
    }

    /// <summary>拒绝哀悼伤害：对所有敌人造成固定值伤害（不吃力量/易伤）。</summary>
    public static async Task DealDamageToAllEnemies(PlayerChoiceContext choiceContext, Creature owner, decimal amount)
    {
        if (owner.CombatState == null)
        {
            return;
        }
        await CreatureCmd.Damage(choiceContext, owner.CombatState.Enemies, amount, ValueProp.Move | ValueProp.Unpowered, owner, null, null);
        await MegaCrit.Sts2.Core.Combat.CombatManager.Instance.CheckWinCondition();
    }
}
