# M0 API 速查（游戏 v0.111.0 + BaseLib v3.4.5，2026-09 反编译实录）

> 完整源码见 `docs/apidump/src/*.cs`；公开成员签名全量见 `docs/apidump/api-signatures.txt`。
> 本文只收"阿米娅模组实现"当前需要的关键事实；`【M1核】`=M1 开工前需继续核实的点。

---

## 1. 模组打包与加载（官方加载器，实锤）

### 1.1 目录与文件
- 本地开发安装路径：`<游戏>\mods\<任意子目录>\`（递归扫描；`ModSource.ModsDirectory`）。
  例：`D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\Amiya\`
- 同目录需放：**清单 JSON（文件名任意）** + **`<id>.dll`** +（has_pck=true 时）**`<id>.pck`**。
- 创意工坊版另由 SteamUGC 扫描（`mods_STEAMTEST` 目录可模拟工坊布局测试）。
- 同一 id 同时存在于 mods 与工坊时，按版本号取高、禁用另一方。

### 1.2 清单 schema（ModManifest 反编译实锤）
```json
{
  "id": "Amiya",
  "name": "阿米娅",
  "author": "...",
  "description": "...",
  "version": "0.2.0",
  "has_dll": true,
  "has_pck": false,
  "affects_gameplay": true,
  "min_game_version": "0.111.0",
  "dependencies": [ { "id": "BaseLib", "min_version": "3.4.5" } ]
}
```
- `dependencies[].min_version` 为字符串；旧式（纯字符串数组）已弃用但暂兼容。
- 缺失 `id` 的 JSON 会被跳过；`min_game_version` 用于版本兼容检查（SemanticVersion）。

### 1.3 入口（ModManager 反编译实锤）
- 扫描程序集内带 `[ModInitializer("方法名")]` 的类 → 反射调用该**静态方法**（无参）。
- **没有任何 [ModInitializer] 时**：加载器自动执行 `Harmony.PatchAll(assembly)`。
- BaseLib 自身写法（`BaseLib.BaseLibMain`）：静态类 + 属性 + `ScriptManagerBridge.LookupScriptsInAssembly(...)`。
- 建议入口骨架：
```csharp
using System.Reflection;
using Godot.Bridge;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Logging;

namespace Amiya;

[ModInitializer("Init")]
public static class Entry
{
    public static void Init()
    {
        ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
        Log.Info("Amiya Mod initialized!");
        // M1+: 注册池内容 ModHelper.AddModelToPool<...>(); Harmony 补丁等
    }
}
```

### 1.4 ModHelper（游戏内建集成面）
- `ModHelper.AddModelToPool<TPoolType, TModelType>()`：把自定义模型挂进指定池（须在游戏初始化前调用，即入口期）。
- `ModHelper.SubscribeForRunStateHooks(id, RunHookSubscriptionDelegate)`：`IEnumerable<AbstractModel> f(RunState)`
- `ModHelper.SubscribeForCombatStateHooks(id, CombatHookSubscriptionDelegate)`：`IEnumerable<AbstractModel> f(CombatState)`
（Hook 委托返回的 AbstractModel 集合会注入 run/combat 状态——形态机等全局 Power 可走这里或直接 ApplySelf。）

---

## 2. BaseLib.Abstracts 自定义基类（3.4.5 反编译实锤）

| 基类 | 继承 | 关键成员 |
|---|---|---|
| `CustomCardModel` | `CardModel` | ctor `(int baseCost, CardType type, CardRarity rarity, TargetType target, bool showInCardLibrary=true, bool autoAdd=true)`；`autoAdd`→`CustomContentDictionary.AddModel(GetType())` 自动注册；`virtual List<(string,string)>? Localization`；`CustomPortraitPath/CustomPortrait/CustomFrame`；`GainsBlock` 由 DynamicVars 自动判定；静态工厂 `MakeCalculatedVar/MakeCalculatedDamage/MakeCalculatedBlock`（DynamicVar 数值体系） |
| `CustomCharacterModel` | `CharacterModel` | ctor 自动注册角色；`virtual Localization`；`HideFromVanillaCharacterSelect / AllowInVanillaRandomCharacterSelect / HideInCompendium`；`CustomVisualPath / CustomIconTexturePath / CustomIconPath / CustomCharacterSelectBg / CustomCharacterSelectIconPath / CustomCharacterSelectLockedIconPath` 等资源路径族；`StartingGold`（默认99）；`CreateCustomVisuals() / SetupCustomAnimationStates()` + 静态 `SetupAnimationState(...)` |
| `CustomPowerModel` | `PowerModel` | `ICustomPower + IHealthBarForecastSource`；`CustomPackedIconPath / CustomBigIconPath / Localization`；`GetHealthBarForecastSegments(...)` |
| `CustomRelicModel` | `RelicModel` | ctor `(bool autoAdd=true)`；`Localization`；**`virtual RelicModel? GetUpgradeReplacement()`** ← 遗物升级钩子（苍白赐福→苍白花冠 可走这里） |
| `CustomCardPoolModel` | `CardPoolModel` | `IsShared`；`EnergyColorName` 自动由 id 映射；`SeenByDefault`；`GenerateAllCards()` 默认空 |
| `CustomRelicPoolModel` / `CustomPotionPoolModel` | 对应池 | 同上结构（GenerateAllRelics/GenerateAllPotions 默认空） |
| `CustomAncientModel` | `AncientEventModel` | ctor `(bool autoAdd=true, bool logDialogueLoad=false)`；`OptionPools`；`static WeightedList<AncientOption> MakePool(params RelicModel[] options)`；`RelicOption(relic)`；`CustomScenePath/CustomMapIconPath...`（先古事件/遗物升级内容可基于此） |

`【M1核】` 卡牌/遗物与"哪个池"的归属如何声明：`ICustomModel`（BaseLib.Abstracts 内接口）与 `CustomContentDictionary`（BaseLib.Patches.Content）的机制 M1 反编译确认；池共享（IsShared）走 `ModelDbShared*PoolsPatch.Register`。

> **【M10 实机教训，2026-09 冒烟测试】`[Pool]` 是强制项，不是可选项**：`CustomContentDictionary.AddModel(Type)` 对 Card/Relic/Potion 模型**必须**能取到 `PoolAttribute`，否则抛 `Model ... must be marked with a PoolAttribute`，经 `ModelDb.Init` 传播导致游戏启动卡死（死循环进程，普通方式杀不掉）。`[Pool]` 属性可继承（85 张卡只在 BaseAmiyaCard 基类挂一次即可）；Character 模型豁免（走角色注册管线）。若某遗物"只应通过升级获得"（如苍白花冠），仍须挂 `[Pool]`，靠稀有度排除：`RelicRarity.Starter => 999999999`（官方权重表），不会被随机奖励/商店选中。

> **【M11 实机教训，2026-09 回合结束卡死】** 战斗内生成卡必须用 `CombatState.CreateCard<T>(owner)`（内部 `ToMutable` + `AddCard` 注册进 `_allCards` + `AfterCreated`），**不能用 `RunState.CreateCard<T>`**：后者只做 run 级创建，卡首次进战斗牌堆时 `IsInCombat=false` 侥幸放行，之后任何牌堆移动（如回合结束 `FlushPlayerHand`）触发 `CombatState.ContainsCard` 校验，抛 `XXX must be added to a CombatState before adding it to this pile`，回合无法结束。受影响的官方入口：`CardCmd.Transform`（AddInternal 不注册）、`CardPileCmd.Add`、`AddGeneratedCardToCombat`（也不注册！）。`CardModel.CreateClone()` 例外——走 `CardScope.CloneCard`=CombatState.CloneCard，内部已注册。同理，规范实例（canonical）不可变：重放数 `BaseReplayCount` 等实例状态改在 `AfterCreated()` 覆写里设置（官方 SpoilsMap 先例），别写进构造函数。

---

## 3. 常用效果命令（BaseLib.Utils.CommonActions，反编译实锤）

- 伤害：`CardAttack(CardModel card, Creature? target, int hitCount=1, ...)` 及多段/`CalculatedDamageVar`/`ValueProp` 重载（≈ 一次"命中"管线：力量/易伤/格挡全含；多段=hitCount）
- 格挡：`CardBlock(CardModel card, CardPlay? play)` / `(card, BlockVar, play)` / `(card, DynamicVar, play, fast)`
- 抽牌：`Draw(CardModel card, PlayerChoiceContext context)`
- 施加状态：`Apply<T>(...) / ApplySelf<T>(...)`（T: PowerModel，支持 amount/DynamicVarSource/多个目标/context）
- 选牌 UI：`SelectCards(...) / SelectSingleCard(...)`（配 `CardSelectorPrefs` 或 LocString 提示 + PileType + 过滤器 + min/max 数量）→ **勘破虚妄/相信明天/燃烬选牌 用这套**
- 生成牌：`GenerateCards(card, count, filter) / GenerateSingleCard(...)`
- 本地化表注册：`BaseLib.Utils.CustomLocTableManager.Register("表名")`（BaseLib 自用 `"card_modifiers"`；卡牌文字走 ILocalizationProvider.Localization 列表）

---

## 4. 数值与文本体系（游戏内建，DynamicVars）

- 命名空间 `MegaCrit.Sts2.Core.Localization.DynamicVars`：`DynamicVar / DamageVar / BlockVar / CalculatedDamageVar / CalculatedBlockVar / CalculatedVar / IntVar / EnergyVar / PowerVar / HealVar / MaxHpVar / HpLossVar / GoldVar / RepeatVar / IfUpgradedVar / StringVar ...`
- 升级分支：`IfUpgradedVar`（升级前后不同文本/数值）；BaseLib 追加 `CustomCalculatedVar / CustomCalculatedDamageVar / CustomCalculatedBlockVar / CustomExtraDamageVar` 等（`BaseLib.Cards.Variables`）。
- 卡面数字写变量、文本引用变量 → 升级只改变量，文本自动刷新（与 00 卷约定一致）。

---

## 5. 模型数据库

- `MegaCrit.Sts2.Core.Models.ModelDb`（16KB 源码在 `src/sts2.ModelDb.cs`）：`Get<T>() / GetById<T>(id) / GetId(Type)` 等（ModHelper 内部即用 `ModelDb.GetById(ModelDb.GetId(t))`）。
- 角色/卡池/遗物池/药水池关联走 `CharacterModel` 的对应属性（见 `src/sts2.CharacterModel.cs`，11KB）。

---

## 6. 反编译事实对设计文档的修正/确认

1. **Q2 先古升级**：游戏存在 Ancient 事件体系（`AncientEventModel`）+ `CustomRelicModel.GetUpgradeReplacement()` 升级替换钩子 → 苍白赐福→苍白花冠 按官方升级管线实现。
2. **Q26 重放**：官方关键字（重放 X），具体实现待反编译官方卡（`【M1核】`）；防递归规则照设计已裁。
3. **领袖/感染/墓园/魔王** 词条为自创 → 在卡牌模型/实例标签 + FormManagerPower 事件总线上实现（00 卷 §3 蓝图不变）。
4. `PlaceholderCharacterModel` 类在 3.4.5 中已不存在（旧代码用它是 3.1.2 时代）——新代码全部用 `CustomCharacterModel`。
5. 自伤"受到X点伤害"可格挡：用 `CardAttack` 对自身目标？——伤害命令若按 CardAttack 走管线即可被自身格挡吸收（`【M1核】` 验证对 Owner 目标的行为；黑冠 ×2 需按 Q5 排除自伤）。

---

## 7. 下一步（M1 准备）核对清单

- [ ] 反编译 `BaseLib.Abstracts.ICustomModel` 与 `BaseLib.Patches.Content.CustomContentDictionary` → 池归属声明方式
- [ ] 反编译 `sts2.CharacterModel.cs` 全文 → 起始牌组/HP/能量/池属性确切成员名
- [ ] 反编译 `PowerModel`（游戏）钩子列表（OnTurnStart/AfterCardPlayed/…）→ 对应 00 卷事件总线映射
- [ ] 反编译 1~2 张官方卡（带 重放/斩杀/虚无 的）作为效果样板
- [ ] 对照 `api-signatures.txt` 补齐 CardSelectorPrefs/PileType 常量
