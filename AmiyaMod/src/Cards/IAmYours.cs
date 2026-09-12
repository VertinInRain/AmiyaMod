using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>我是你的了（稀有）：给予自身 99 层易伤、99 层虚弱、99 层脆弱；每次获得格挡时转化为等量再生（Q19：A 转化，不保留格挡）。</summary>
public sealed class IAmYours : BaseAmiyaCard
{
    public IAmYours() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "我是你的了"),
        ("description", "#给予自身 99 层易伤、99 层虚弱、99 层脆弱。每次获得格挡时转化为等量再生。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var self = Owner!.Creature;
        await PowerCmd.Apply<VulnerablePower>(choiceContext, self, 99m, self, null, silent: true);
        await PowerCmd.Apply<WeakPower>(choiceContext, self, 99m, self, null, silent: true);
        await PowerCmd.Apply<FrailPower>(choiceContext, self, 99m, self, null, silent: true);
        await PowerCmd.Apply<IAmYoursPower>(choiceContext, self, 1m, self, null, silent: true);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 → 0
    }
}

/// <summary>我是你的了 Power：格挡获得时全额转化为等量再生（由 FormManagerPower.AfterBlockGained 结算，Q19 A 不保留格挡）。</summary>
public sealed class IAmYoursPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/IAmYoursPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "我是你的了"),
        ("description", "每次获得格挡时转化为等量再生。")
    };
}
