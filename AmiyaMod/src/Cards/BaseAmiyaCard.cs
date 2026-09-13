using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Art;
using Amiya.Models;
using Amiya.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Cards;

/// <summary>
/// All Amiya cards inherit this: attaches them to AmiyaCardPool via BaseLib PoolAttribute,
/// exposes the shared "trigger a form switch" helper, the custom keyword tag (AmiyaTag),
/// and auto-registers keyword tooltip texts (魔王/感染/领袖/墓园) for the hover box.
/// </summary>
[Pool(typeof(AmiyaCardPool))]
public abstract class BaseAmiyaCard : CustomCardModel
{
    /// <summary>自定义词条标签（魔王/感染/领袖/墓园），由各卡覆写声明。</summary>
    public virtual AmiyaTag AmiyaTags => AmiyaTag.None;

    /// <summary>实例级附加词条（相信明天/不容拒绝 动态添加；克隆随实例复制）。</summary>
    public AmiyaTag InstanceTags { get; set; }

    public bool HasAmiyaTag(AmiyaTag tag) => ((AmiyaTags | InstanceTags) & tag) != 0;

    /// <summary>子类在此提供卡牌文本（title/description），基类会自动追加词条工具提示文本。</summary>
    protected virtual List<(string, string)>? CardLocalization => null;

    /// <summary>sealed：文本 = 卡牌自身文本 + 各词条的标题/描述（悬停框）+ 描述内金色高亮词条行。</summary>
    public sealed override List<(string, string)>? Localization
    {
        get
        {
            var list = CardLocalization ?? new List<(string, string)>();
            // 词条文本无条件注册：相信明天/不容拒绝会动态添加实例词条，
            // 悬停提示的 LocString 键必须对所有卡牌存在，否则显示原始键名（一串英文）
            foreach (var tag in new[] { AmiyaTag.DemonLord, AmiyaTag.Infection, AmiyaTag.Leader, AmiyaTag.Graveyard })
            {
                string suffix = tag switch
                {
                    AmiyaTag.DemonLord => "demon_lord",
                    AmiyaTag.Infection => "infection",
                    AmiyaTag.Leader => "leader",
                    AmiyaTag.Graveyard => "graveyard",
                    _ => null
                };
                if (suffix == null)
                {
                    continue;
                }
                list.Add(($"kw_{suffix}_title", KeywordTitle(tag)));
                list.Add(($"kw_{suffix}_desc", KeywordDescription(tag)));
            }

            // 描述第一行前置金色高亮关键词（仅词条名，不展开功能说明；功能说明保留在悬停框）
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Item1 != "description" || !list[i].Item2.StartsWith('#'))
                {
                    continue;
                }
                var names = new List<string>();
                foreach (var tag in new[] { AmiyaTag.DemonLord, AmiyaTag.Infection, AmiyaTag.Leader, AmiyaTag.Graveyard })
                {
                    if (HasAmiyaTag(tag))
                    {
                        names.Add("*" + KeywordTitle(tag) + "*");
                    }
                }
                if (names.Count > 0)
                {
                    list[i] = (list[i].Item1, "#" + string.Join(" ", names) + "\n" + list[i].Item2.Substring(1));
                }
            }
            return list;
        }
    }

    internal static string KeywordTitle(AmiyaTag tag) => tag switch
    {
        AmiyaTag.DemonLord => "魔王",
        AmiyaTag.Infection => "感染",
        AmiyaTag.Leader => "领袖",
        AmiyaTag.Graveyard => "墓园",
        _ => ""
    };

    private static string KeywordDescription(AmiyaTag tag) => tag switch
    {
        AmiyaTag.DemonLord => "打出后获得 1 层魔王之唤。",
        AmiyaTag.Infection => "回合结束时，若此牌在弃牌堆中，将其变为打击或防御。",
        AmiyaTag.Leader => "若此牌是本回合第奇数张打出的牌，将其放入抽牌堆。",
        AmiyaTag.Graveyard => "战斗开始时，此牌位于弃牌堆。",
        _ => ""
    };

    /// <summary>悬停框内展示自定义词条（标题高亮 + 描述）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var tips = new List<IHoverTip>();
            foreach (var tag in new[] { AmiyaTag.DemonLord, AmiyaTag.Infection, AmiyaTag.Leader, AmiyaTag.Graveyard })
            {
                if (!HasAmiyaTag(tag))
                {
                    continue;
                }
                string suffix = tag switch
                {
                    AmiyaTag.DemonLord => "demon_lord",
                    AmiyaTag.Infection => "infection",
                    AmiyaTag.Leader => "leader",
                    AmiyaTag.Graveyard => "graveyard",
                    _ => null
                };
                if (suffix == null)
                {
                    continue;
                }
                tips.Add(new HoverTip(
                    new LocString("cards", base.Id.Entry + ".kw_" + suffix + "_title"),
                    new LocString("cards", base.Id.Entry + ".kw_" + suffix + "_desc")));
            }
            return tips;
        }
    }

    /// <summary>
    /// 卡图 = 运行时从 mods 目录加载的定制立绘（mods/Amiya/spine/card_art/<卡牌类名>.png）。
    /// 多级升级卡（如存续先兆）支持按等级取图：优先 <类名>_<等级>.png，不存在则回退 <类名>.png。
    /// 走 BaseLib CustomPortrait(Texture2D) 官方钩子 + Image.LoadFromFile，
    /// 绕开"游戏构建无 PNG 资源加载器、mod pck 只认 .ctex"的限制。
    ///
    /// 注意：运行时创建的 ImageTexture 的 ResourcePath 默认为空。其他 mod（如新版 RitsuLib）
    /// 会通过 Model.PortraitPath 再次加载卡图，空路径会被引擎解析成 "res://" 报错
    /// "No loader found for resource: res://"，并导致卡图被清掉。
    /// 因此每张加载成功的卡图都 TakeOverPath 注册进资源缓存（res://Amiya/card_art/…），
    /// 任何后续按路径加载的代码都会直接命中缓存拿到同一张纹理。
    /// </summary>
    public override string? CustomPortraitPath => "res://Amiya/card_art/" + GetType().Name + ".png";

    private static readonly System.Collections.Generic.Dictionary<string, Texture2D?> _portraitCache = new();

    /// <summary>多级升级卡是否使用按等级区分的卡图（如存续先兆）。默认 false：单图，不做无谓的文件探测。</summary>
    protected virtual bool UsesLevelPortraits => false;

    public override Texture2D? CustomPortrait
    {
        get
        {
            string name = GetType().Name;
            string key = name + "_" + CurrentUpgradeLevel;
            if (_portraitCache.TryGetValue(key, out var cached))
            {
                return cached;
            }
            Texture2D? tex = null;
            // 只有多级卡图才探测 <名>_<级>.png；普通卡直接读 <名>.png，
            // 避免每张卡一次失败的磁盘探测 + 引擎错误日志（卡顿来源）。
            string[] candidates = UsesLevelPortraits
                ? new[] { name + "_" + CurrentUpgradeLevel + ".png", name + ".png" }
                : new[] { name + ".png" };
            foreach (string candidate in candidates)
            {
                try
                {
                    var img = Image.LoadFromFile(AmiyaPaths.CardArtDir + "/" + candidate);
                    // 打开失败时 Godot 返回空 Image（不抛异常）——必须判空，否则空纹理会被当作卡图
                    if (img != null && !img.IsEmpty())
                    {
                        tex = ImageTexture.CreateFromImage(img);
                        // 注册进资源缓存：PortraitPath/其他 mod 按路径加载时直接命中，不再产生空路径加载错误
                        string cachePath = "res://Amiya/card_art/" + candidate;
                        tex.TakeOverPath(cachePath);
                        Log.Info($"[Amiya] portrait loaded: {candidate} -> {cachePath}");
                        break;
                    }
                }
                catch (Exception)
                {
                    // 文件缺失时尝试下一个候选
                }
            }
            _portraitCache[key] = tex;
            return tex;
        }
    }

    protected BaseAmiyaCard(int baseCost, CardType type, CardRarity rarity, TargetType target)
        : base(baseCost, type, rarity, target)
    {
    }

    protected BaseAmiyaCard(int baseCost, CardType type, CardRarity rarity, TargetType target, bool showInCardLibrary, bool autoAdd)
        : base(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
    {
    }

    /// <summary>Trigger one form switch through the battle's FormManager (no-op outside combat).
    /// 等待完成：燃烬形态下的选牌消耗 UI 需要动作上下文存活期间完成。</summary>
    protected async Task TriggerFormSwitch(PlayerChoiceContext choiceContext)
    {
        if (FormManagerPower.Of(Owner) is { } fm)
        {
            await fm.Trigger(choiceContext);
        }
    }

    /// <summary>
    /// 实例词条打标后强制卡面重渲染：选牌容器收回卡牌期间 FindOnTable 可能找不到节点，
    /// 带重试等待（最多 2 秒）。重渲染会走 GetDescriptionForPile → 实例词条金色行补丁生效。
    /// </summary>
    internal static async Task RefreshCardVisualsAsync(CardModel card)
    {
        for (int i = 0; i < 20; i++)
        {
            var node = MegaCrit.Sts2.Core.Nodes.Cards.NCard.FindOnTable(card);
            if (node != null)
            {
                node.UpdateVisuals(card.Pile.Type, MegaCrit.Sts2.Core.Entities.Cards.CardPreviewMode.Normal);
                return;
            }
            await Task.Delay(100);
        }
    }
}
