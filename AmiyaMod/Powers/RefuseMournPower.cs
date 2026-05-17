using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Amiya.Powers;

/// <summary>
/// 拒绝哀�?- 每当感染牌变为打击防御时对所有敌人造成伤害
/// Amount: 每张感染牌造成的伤害�?
/// 触发逻辑在AmiyaMechanicsPower�?
/// </summary>
public partial class RefuseMournPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}