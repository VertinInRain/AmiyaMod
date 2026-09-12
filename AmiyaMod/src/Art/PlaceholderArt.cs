namespace Amiya.Art;

/// <summary>
/// 占位美术：全部指向游戏官方已存在的贴图（名字取自 SlayTheSpire2.pck 资源清单 docs/apidump/pck_sprite_list.txt）。
/// 后续接入真实阿米娅立绘时，只需替换这里的路径。
/// </summary>
public static class PlaceholderArt
{
    private const string CardBase = "res://images/atlases/card_atlas.sprites/ironclad/";
    private const string PowerBase = "res://images/atlases/power_atlas.sprites/";
    private const string RelicBase = "res://images/atlases/relic_atlas.sprites/";

    /// <summary>官方卡牌"缺图"兜底贴图（不在 ironclad 池内，单独存放于卡图集根目录）。</summary>
    public const string CardFallback = "res://images/atlases/card_atlas.sprites/beta.tres";

    public static string Card(string name) => CardBase + name + ".tres";

    public static string Power(string name) => PowerBase + name + ".tres";

    public static string Relic(string name) => RelicBase + name + ".tres";
}
