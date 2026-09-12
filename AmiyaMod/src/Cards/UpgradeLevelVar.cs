using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Cards;

/// <summary>
/// 按卡牌升级等级取值的动态变量：绕过官方"乘区仅在战斗中计算"的限制，
/// 篝火升级后、卡组界面等任意场景都按 card.CurrentUpgradeLevel 实时取值。
/// 注意：官方描述渲染会走 ToString()/BaseValue 而非 IConvertible，
/// 因此必须同时重写 ToString 并在 SetOwner 时把实时值写进 BaseValue（否则显示 0）。
/// </summary>
public sealed class UpgradeLevelVar : DynamicVar
{
    private readonly Func<CardModel, decimal> _value;

    public UpgradeLevelVar(string name, Func<CardModel, decimal> value) : base(name, 0m)
    {
        _value = value;
    }

    public override void SetOwner(AbstractModel owner)
    {
        base.SetOwner(owner);
        if (owner is CardModel card)
        {
            base.BaseValue = _value(card);
        }
    }

    public override string ToString()
    {
        return (_owner is CardModel card ? _value(card) : 0m).ToString("0");
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
        base.PreviewValue = _value(card);
    }

    protected override decimal GetBaseValueForIConvertible()
    {
        return _owner is CardModel card ? _value(card) : 0m;
    }
}

/// <summary>
/// 按卡牌升级等级取值的字符串动态变量（存续先兆等"每级文本不同"的卡用）：
/// 描述里写 {EFFECT}。直接重写 ToString 按 _owner 卡实时取值——
/// 不依赖 UpdateCardPreview 是否被调用，任何渲染路径都正确。
/// </summary>
public sealed class UpgradeLevelTextVar : StringVar
{
    private readonly Func<CardModel, string> _value;

    public UpgradeLevelTextVar(string name, Func<CardModel, string> value) : base(name)
    {
        _value = value;
    }

    public override string ToString()
    {
        return _owner is CardModel card ? _value(card) : "";
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
        StringValue = _value(card);
    }
}
