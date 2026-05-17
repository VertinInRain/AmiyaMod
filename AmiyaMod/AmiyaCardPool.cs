using BaseLib.Abstracts;
using Godot;

namespace Amiya;

/// <summary>
/// 阿米娅卡牌池
/// </summary>
public partial class AmiyaCardPool : CustomCardPoolModel
{
    public override string Title => "Amiya";
    
    public override string EnergyColorName => "ironclad";
    
    public override Color DeckEntryCardColor => new Color(0.4f, 0.7f, 1f, 1f);
    
    public override bool IsColorless => false;
}
