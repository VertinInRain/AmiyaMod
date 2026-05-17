using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Amiya.Cards;

[Pool(typeof(AmiyaCardPool))]

/// <summary>
/// 阿米娅卡牌基�?
/// </summary>
public abstract class BaseAmiyaCard : CustomCardModel
{
    public override string PortraitPath => "res://Amiya/images/cards/" + ReplaceID(Id.Entry.ToLowerInvariant()) + ".png";
    
    private string ReplaceID(string path)
    {
        return path.Replace("amiya-", "");
    }
    
    protected BaseAmiyaCard(int baseCost, CardType type, CardRarity rarity, TargetType target)
        : base(baseCost, type, rarity, target)
    {
    }
    
    protected BaseAmiyaCard(int baseCost, CardType type, CardRarity rarity, TargetType target, bool showInCardLibrary, bool autoAdd)
        : base(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
    {
    }
}
