using Amiya.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Amiya.Powers;

/// <summary>
/// 临时力量（阿米娅用）：本回合结束前获得等量力量（官方 TemporaryStrengthPower 子类，同 CoordinatePower 模式）。
/// 用于 盛怒旋风 / 燎原。
/// </summary>
public sealed class AmiyaTempStrengthPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<RageWhirlwind>();
}
