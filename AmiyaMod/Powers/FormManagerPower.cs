using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Powers;

/// <summary>
/// 形态管理器：同时处理形态切换、感染、墓�?
/// Amount固定�?（游戏要�?0），形态用内部数据存储
/// </summary>
public partial class FormManagerPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private int _currentForm = 0;
    public int CurrentForm => _currentForm;
    public int TotalFormSwitches { get; private set; }

    /// <summary>回合结束时处理感�?/summary>
    public override async Task AfterTurnEnd(PlayerChoiceContext cc, CombatSide side)
    {
        await base.AfterTurnEnd(cc, side);
        if (side != Owner.Side) return;
        await ProcessInfection(cc);
    }

    /// <summary>全局魔王检测：打出带魔王词条的牌增加魔王之�?/summary>
    public override async Task AfterCardPlayedLate(PlayerChoiceContext cc, CardPlay cardPlay)
    {
        await base.AfterCardPlayedLate(cc, cardPlay);
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (cardPlay.Card.Keywords.Contains(AmiyaKeywords.DemonLord))
        {
            if (!Owner.HasPower<DemonLordCallPower>())
                await PowerCmd.Apply<DemonLordCallPower>(cc, new[] { Owner }, 1m, Owner, null);
            var dlc = Owner.GetPower<DemonLordCallPower>();
            if (dlc != null) await dlc.AddStack(cc);
        }
    }

    private async Task ProcessInfection(PlayerChoiceContext cc)
    {
        if (Owner.Player == null) return;
        var discardPile = PileType.Discard.GetPile(Owner.Player);
        var infected = discardPile.Cards.Where(c => c.Keywords.Contains(AmiyaKeywords.Infection)).ToList();
        foreach (var card in infected)
        {
            bool upgraded = card.IsUpgraded;
            await CardPileCmd.RemoveFromCombat(card, true);
            CardModel replacement = card.Type == CardType.Attack
                ? CombatState.CreateCard<Strike>(Owner.Player)
                : CombatState.CreateCard<Defend>(Owner.Player);
            if (upgraded) replacement.UpgradeInternal();
            await CardPileCmd.AddGeneratedCardToCombat(replacement, PileType.Discard, Owner.Player, CardPilePosition.Bottom);
        }
        if (infected.Count > 0 && Owner.HasPower<RefuseMournPower>())
        {
            var rmp = Owner.GetPower<RefuseMournPower>();
            if (rmp != null)
                foreach (var enemy in CombatState.HittableEnemies)
                    await CreatureCmd.Damage(cc, enemy, rmp.Amount * infected.Count, ValueProp.Unpowered, Owner);
        }
    }

    /// <summary>触发形态转�?/summary>
    public async Task TriggerFormSwitch(PlayerChoiceContext cc)
    {
        if (Owner.HasPower<EmberPower>()) { Flash(); return; }
        int next = (_currentForm + 1) % 3;
        _currentForm = next;
        TotalFormSwitches++;
        Flash();
        await ApplyFormEffect(cc, next);
        await NotifyListeners(cc);
        await CheckRunningNight(cc);
    }

    private async Task ApplyFormEffect(PlayerChoiceContext cc, int form)
    {
        switch (form)
        {
            case 0:
                await CreatureCmd.GainBlock(Owner, 3m, ValueProp.Move, null);
                await PowerCmd.Apply<VigorPower>(cc, new[] { Owner }, 3m, Owner, null);
                break;
            case 1:
                await PowerCmd.Apply<WeakPower>(cc, CombatState.HittableEnemies, 1m, Owner, null);
                await PowerCmd.Apply<VulnerablePower>(cc, CombatState.HittableEnemies, 1m, Owner, null);
                break;
            case 2:
                if (Owner.Player != null) await CardPileCmd.Draw(cc, 2m, Owner.Player);
                break;
        }
    }

    private async Task NotifyListeners(PlayerChoiceContext cc)
    {
        foreach (var power in Owner.Powers.ToList())
        {
            switch (power)
            {
                case SoulFortressPower sfp: await sfp.OnFormSwitchTriggered(cc); break;
                case WildfirePower wfp: await wfp.OnFormSwitchTriggered(cc); break;
                case SpreadPower sp: await sp.OnFormSwitchTriggered(cc); break;
                case IronGuardPower igp: await igp.OnFormSwitchTriggered(cc); break;
            }
        }
    }

    /// <summary>奔夜效果：形态转换时将RunningNight从任意位置放入手�?/summary>
    public async Task CheckRunningNight(PlayerChoiceContext cc)
    {
        if (Owner.Player == null) return;
        // 搜索所有牌堆找到RunningNight
        foreach (var card in PileType.Draw.GetPile(Owner.Player).Cards.ToList())
            if (card is Amiya.Cards.RunningNight && card.Pile?.Type != PileType.Hand)
                await CardPileCmd.Add(card, PileType.Hand);
        foreach (var card in PileType.Discard.GetPile(Owner.Player).Cards.ToList())
            if (card is Amiya.Cards.RunningNight && card.Pile?.Type != PileType.Hand)
                await CardPileCmd.Add(card, PileType.Hand);
        foreach (var card in PileType.Exhaust.GetPile(Owner.Player).Cards.ToList())
            if (card is Amiya.Cards.RunningNight && card.Pile?.Type != PileType.Hand)
                await CardPileCmd.Add(card, PileType.Hand);
    }
}