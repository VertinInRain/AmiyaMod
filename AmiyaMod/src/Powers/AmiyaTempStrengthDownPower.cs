using Amiya.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Amiya.Powers;

/// <summary>
/// 临时力量下降（怒号光明）：本回合失去等量力量，回合结束自动恢复。
/// 官方 ShacklingPotionPower 同款（TemporaryStrengthPower + IsPositive=false）：
/// 施加时力量 -Amount，回合结束 Power 移除并力量 +Amount 复原。
/// </summary>
public sealed class AmiyaTempStrengthDownPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<RoaringLight>();

    protected override bool IsPositive => false;
}
