# 阿米娅模组实现计划

## 当前状态
✅ 核心框架已完成
✅ 项目可以成功编译
✅ 已理解所有游戏机制和设计文档

## 待实现内容

### 1. 形态系统 Powers (高优先级)

#### 1.1 FormManagerPower (已创建框架)
- ✅ 基础结构
- ⚠️ 需要修复：使用正确的API获取和设置Power状态
- ⚠️ 需要添加：战斗开始时初始化为近卫形态

#### 1.2 魔王形态相关Powers
创建以下Power类：

**DemonLordCallPower.cs** (魔王之唤)
```csharp
- 层数：0-7
- 达到7层时触发进入魔王形态
- 进入魔王形态时：
  - 添加BlackCrownPower
  - 添加EmberPower  
  - 添加JudgmentPower
```

**BlackCrownPower.cs** (黑冠)
```csharp
- 造成的伤害翻倍
- 每出一张牌，受到3点伤害
```

**EmberPower.cs** (燃烬)
```csharp
- 触发形态转换时，改为选择消耗手牌中的一张牌
```

**JudgmentPower.cs** (裁决)
```csharp
- 获得魔王之唤时，对所有敌人造成9点伤害
```

### 2. 遗物系统

#### 2.1 PaleBlessingRelic.cs (苍白赐福)
```csharp
- 除第一回合外，每回合开始时触发一次形态转换
- 可被先古遗物升级
```

#### 2.2 PaleCrownRelic.cs (苍白花冠)
```csharp
- 苍白赐福的升级版
- 除第一回合外，每回合开始时触发两次形态转换
```

### 3. 卡牌系统 (86张卡牌)

#### 3.1 攻击牌 (33张)

**初始卡牌 (2张)**
1. ✅ Strike.cs (打击) - 已实现
2. Resolve.cs (决意) - 卡图2

**普通攻击牌 (9张)**
3. TacticalChant.cs (战术咏唱) - 卡图4
4. OneStrikeEndsAll.cs (一断皆断) - 卡图2
5. EmotionAbsorption.cs (情绪吸收) - 卡图1
6. TruthRemnant.cs (真理残余) - 卡图1
7. StormAssault.cs (风暴突击) - 卡图2 - **感染**
8. RoaringLight.cs (怒号光明) - 卡图2 - **感染**
9. Return.cs (洄还) - 卡图1 - **领袖**
10. AbandonAbyss.cs (背弃深渊) - 卡图2 - **墓园**
11. DemonLordSword.cs (魔王的誓剑) - 卡图6 - **魔王**

**罕见攻击牌 (12张)**
12. RunningNight.cs (奔夜) - 卡图1
13. SwordNeverSleeps.cs (剑不眠) - 卡图2 - **领袖**
14. RedDawnDraw.cs (赤霄拔刀) - 卡图2
15. RageWhirlwind.cs (盛怒旋风) - 卡图2
16. PainConnection.cs (痛觉相连) - 卡图2
17. TacticalBombardment.cs (战术轰炸) - 卡图1
18. RabbitKick.cs (兔兔之踢) - 卡图3
19. EndlessFlood.cs (洪流不息) - 卡图6 - **魔王**
20. SufferingCradle.cs (苦难摇篮) - 卡图2 - **固有**
21. SupportFuture.cs (支援未来) - 卡图1 - **感染**
22. BloodForBlood.cs (以血还血) - 卡图6 - **魔王**

**稀有攻击牌 (10张)**
23. LongSpearNightFire.cs (长枪夜火) - 卡图2 - **领袖**
24. BloodBladeSuspended.cs (血刃高悬) - 卡图2 - **墓园**
25. BlackCrownShock.cs (黑冠震荡) - 卡图6 - **魔王**
26. ReturnToBeginning.cs (重返初识) - 卡图1 - **感染**
27. SoulTenacitySong.cs (神魂坚韧之歌) - 卡图1 - **保留**
28. UntunedRage.cs (待调弦的怒火) - 卡图2 - **领袖**
29. Behead.cs (斩首) - 卡图1 - **消耗**
30. UnattainableDesire.cs (求而不得之物) - 卡图2 - **虚无**

#### 3.2 技能牌 (33张)

**初始技能牌 (2张)**
31. ✅ Defend.cs (防御) - 已实现
32. ✅ FormSwitch.cs (思念) - 已实现，需重命名

**普通技能牌 (7张)**
33. InsightEye.cs (洞见之眼) - 卡图4
34. SurvivalOmen.cs (存续先兆) - 卡图6 - **魔王**
35. TurnBackSmile.cs (回身一笑) - 卡图5
36. GriefEmpathy.cs (哀恸共情) - 卡图4 - **感染**
37. ArmorUp.cs (披甲在身) - 卡图4
38. GracefulHeart.cs (蕙质兰心) - 卡图5
39. BodyAsWall.cs (以身铸墙) - 卡图5

**罕见技能牌 (15张)**
40. TomorrowAgain.cs (明日再来) - 卡图4
41. Her.cs (她？) - 卡图9
42. SeeWhatISee.cs (见我所见) - 卡图5 - **魔王**
43. TimeHasCome.cs (时辰已到) - 卡图6 - **墓地** **魔王**
44. BelieveTomorrow.cs (相信明天) - 卡图5 - **感染**
45. Pursuit.cs (追寻) - 卡图4 - **感染**
46. SculptureInstallation.cs (立体艺术装置) - 卡图4
47. BrokenHorn.cs (残缺长角) - 卡图5
48. HorizonFireCloud.cs (天边的火烧云) - 卡图4
49. BoneVoice.cs (骸骨传声) - 卡图4
50. SeeThrough.cs (勘破虚妄) - 卡图4
51. ActOrdinary.cs (演绎平凡) - 卡图5
52. GoodNightDying.cs (良夜将死) - 卡图4
53. DanceInFlowers.cs (于花丛中轻舞) - 卡图5
54. BorrowTomorrow.cs (预借明日) - 卡图4

**稀有技能牌 (9张)**
55. Hello.cs (你好！) - 卡图5 - **固有**
56. StormWatch.cs (风暴瞭望) - 卡图4
57. WhereToGo.cs (去往何方) - 卡图5 - **领袖**
58. PhaseChange.cs (相变不息) - 卡图4
59. MyHeartFollows.cs (我心永随) - 卡图5
60. ShowMeTruth.cs (示我以真？) - 卡图4
61. MercyVision.cs (慈悲愿景) - 卡图5
62. CyanRage.cs (青色怒火) - 卡图4 - **领袖**
63. CannotRefuse.cs (不容拒绝) - 卡图6

**先古技能牌 (1张)**
64. ThousandVisions.cs (万千愿景) - 卡图7

#### 3.3 能力牌 (20张)

**普通能力牌 (1张)**
65. OrdinaryJoy.cs (平凡亦是喜乐) - 卡图10

**罕见能力牌 (13张)**
66. Wildfire.cs (燎原) - 卡图10
67. RefuseMourn.cs (拒绝哀悼) - 卡图10
68. SoulFortress.cs (灵魂堡垒) - 卡图10
69. IronGuard.cs (铁卫) - 卡图10
70. Turbulence.cs (湍流) - 卡图10
71. DemonLordVessel.cs (魔王的祭器) - 卡图6
72. DemonLordBanner.cs (魔王的旗帜) - 卡图6
73. NursingCard.cs (疗养特供卡) - 卡图10
74. ProphecyImage.cs (预言显像) - 卡图6
75. VoidFragment.cs (虚空残片) - 卡图10
76. TodayTomorrowDeviation.cs (今时明日的偏差) - 卡图10

**稀有能力牌 (5张)**
77. Arrived.cs (已至) - 卡图6 - **魔王**
78. Spread.cs (蔓延) - 卡图10
79. Sowing.cs (播种) - 卡图10
80. FinalShadow.cs (终焉之影) - 卡图6
81. BuriedUnderground.cs (深埋地底) - 卡图10
82. WildRoar.cs (旷野轰鸣) - 卡图10 - **墓园**

**先古能力牌 (1张)**
83. SeeLight.cs (得见光芒) - 卡图8 - **固有**

### 4. 卡图映射表

```
1.png -> 打击, 情绪吸收, 真理残余, 洄还, 奔夜, 战术轰炸, 支援未来, 斩首, 重返初识, 神魂坚韧之歌
2.png -> 决意, 一断皆断, 风暴突击, 怒号光明, 背弃深渊, 剑不眠, 赤霄拔刀, 盛怒旋风, 痛觉相连, 长枪夜火, 血刃高悬, 待调弦的怒火, 求而不得之物
3.png -> 兔兔之踢
4.png -> 战术咏唱, 洞见之眼, 哀恸共情, 披甲在身, 明日再来, 追寻, 立体艺术装置, 天边的火烧云, 骸骨传声, 勘破虚妄, 良夜将死, 预借明日, 风暴瞭望, 相变不息, 示我以真？, 青色怒火
5.png -> 回身一笑, 蕙质兰心, 以身铸墙, 见我所见, 相信明天, 残缺长角, 演绎平凡, 于花丛中轻舞, 你好！, 去往何方, 我心永随, 慈悲愿景
6.png -> 魔王的誓剑, 洪流不息, 以血还血, 黑冠震荡, 存续先兆, 时辰已到, 不容拒绝, 魔王的祭器, 魔王的旗帜, 预言显像, 已至, 终焉之影
7.png -> 万千愿景
8.png -> 得见光芒
9.png -> 她？
10.png -> 平凡亦是喜乐, 燎原, 拒绝哀悼, 灵魂堡垒, 铁卫, 湍流, 疗养特供卡, 虚空残片, 今时明日的偏差, 蔓延, 播种, 深埋地底, 旷野轰鸣
```

### 5. 本地化系统

需要为每张卡牌创建：
- 卡牌名称
- 卡牌描述
- 升级后描述（如果不同）

需要为每个关键词创建：
- 关键词名称
- 关键词描述

需要为角色创建：
- 角色名称
- 角色描述

### 6. 项目文件更新

#### 6.1 更新 Amiya.csproj
添加所有新文件到编译列表

#### 6.2 更新 AmiyaCardPool.cs
注册所有86张卡牌

#### 6.3 更新 AmiyaRelicPool.cs
注册苍白赐福遗物

#### 6.4 更新 AmiyaCharacter.cs
- 添加苍白赐福到StartingRelics
- 确保战斗开始时处于近卫形态

### 7. 资源文件组织

```
AmiyaMod/
├── Resources/
│   ├── Images/
│   │   ├── Cards/
│   │   │   ├── 1.png
│   │   │   ├── 2.png
│   │   │   ├── ...
│   │   │   └── 10.png
│   │   ├── Character/
│   │   │   └── icon.png
│   │   └── Relics/
│   │       └── pale_blessing.png
│   └── Localization/
│       └── zh_CN.json
```

### 8. 编译和打包

1. 编译C#代码生成DLL
2. 使用Godot导出.pck文件
3. 创建最终模组包

## 实现顺序建议

1. ✅ 核心框架 (已完成)
2. **形态系统Powers** (当前)
3. **遗物系统**
4. **卡牌系统** (分批实现)
   - 先实现初始卡牌
   - 再实现普通卡牌
   - 最后实现罕见和稀有卡牌
5. **本地化**
6. **资源整合**
7. **测试和调试**

## 注意事项

1. 所有类都需要`partial`修饰符
2. 使用`ModelDb`注册和获取模型
3. 卡牌效果通过`OnPlay`方法实现
4. 使用`DynamicVar`管理卡牌数值
5. 使用`Command`模式执行游戏动作
6. 参考Typhon的实现方式
7. 确保所有API调用正确

## 下一步行动

继续实现魔王形态相关的Power类，然后创建遗物系统。
