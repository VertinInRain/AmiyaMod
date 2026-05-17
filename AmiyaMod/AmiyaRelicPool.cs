using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using Amiya.Relics;

namespace Amiya;

/// <summary>
/// 阿米娅遗物池
/// </summary>
public partial class AmiyaRelicPool : CustomRelicPoolModel
{
    public override string EnergyColorName => "ironclad";
    
    /// <summary>
    /// 注册遗物到池中
    /// </summary>
    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        // 起始遗物 - 使用ModelDb避免重复创建
        yield return ModelDb.Relic<PaleBlessingRelic>();
        
        // 稀有遗物
        // TODO: 添加更多遗物
    }
}
