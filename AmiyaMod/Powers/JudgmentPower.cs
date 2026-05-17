using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Amiya.Powers;

/// <summary>
/// 裁决 - 获得魔王之唤时，对所有敌人造成9点伤�?
/// </summary>
public partial class JudgmentPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    
    // 这个Power的效果在DemonLordCallPower中检查和处理
}
