using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Amiya.Powers;

/// <summary>
/// 燃烬 - 触发形态转换时，将效果改为选择消耗手牌中的一张牌
/// </summary>
public partial class EmberPower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    
    // 这个Power的效果在FormManagerPower中检查和处理
}
