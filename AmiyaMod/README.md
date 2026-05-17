# 阿米娅模组 (Amiya Mod)

## 项目概述

这是一个为《杀戮尖塔2》(Slay the Spire 2)开发的自定义角色模组，将明日方舟中的阿米娅带入游戏。

## 当前状态

✅ **已完成的核心框架**

### 1. 项目结构
- ✅ C# 项目配置 (Amiya.csproj)
- ✅ Godot 项目配置 (project.godot)
- ✅ 模组元数据 (Amiya.json)
- ✅ 成功编译生成 DLL

### 2. 核心类实现

#### 枚举系统
- ✅ `AmiyaKeywords.cs` - 自定义关键词枚举
  - BlackCrown (黑冠)
  - Ember (燃烬)
  - Judgment (裁决)
  - DemonLordCall (魔王之唤)

#### 角色系统
- ✅ `AmiyaCharacter.cs` - 角色定义
  - 起始生命值: 70
  - 起始卡组: 5张打击 + 5张防御 + 2张形态切换
  - 颜色主题: 蓝色系

#### 卡牌系统
- ✅ `BaseAmiyaCard.cs` - 卡牌基类
- ✅ `Strike.cs` - 打击 (造成6点伤害，升级+3)
- ✅ `Defend.cs` - 防御 (获得5点格挡，升级+3)
- ✅ `FormSwitch.cs` - 形态切换 (切换战斗形态)

#### 能力系统
- ✅ `FormManagerPower.cs` - 形态管理器
  - 追踪当前形态
  - 处理形态切换逻辑
  - 应用形态效果

#### 池系统
- ✅ `AmiyaCardPool.cs` - 卡牌池
- ✅ `AmiyaRelicPool.cs` - 遗物池
- ✅ `AmiyaPotionPool.cs` - 药水池

#### 入口系统
- ✅ `Entry.cs` - 模组入口点
  - Harmony 补丁系统
  - 内容注册框架

## 技术架构

### 依赖项
- **BaseLib 3.1.2** - 模组开发基础库
- **Godot 4.3** - 游戏引擎
- **.NET 9.0** - 运行时框架
- **HarmonyLib** - 运行时补丁库

### 命名空间结构
```
Amiya/
├── Amiya (根命名空间)
│   ├── Entry (入口)
│   ├── AmiyaCharacter (角色)
│   ├── AmiyaKeywords (关键词)
│   ├── FormManagerPower (形态管理)
│   ├── AmiyaCardPool (卡牌池)
│   ├── AmiyaRelicPool (遗物池)
│   └── AmiyaPotionPool (药水池)
└── Amiya.Cards (卡牌命名空间)
    ├── BaseAmiyaCard (基类)
    ├── Strike (打击)
    ├── Defend (防御)
    └── FormSwitch (形态切换)
```

## 待完成工作

### 高优先级
1. **资源文件**
   - [ ] 角色图标 (icon.png)
   - [ ] 角色视觉效果 (character_visual.tscn)
   - [ ] 卡牌图片
   - [ ] 音效文件

2. **本地化**
   - [ ] 卡牌名称和描述
   - [ ] 关键词说明
   - [ ] 角色介绍

3. **完整卡牌实现**
   - [ ] 实现所有设计文档中的卡牌
   - [ ] 添加卡牌效果和动画

### 中优先级
4. **形态系统完善**
   - [ ] 实现黑冠形态效果
   - [ ] 实现燃烬形态效果
   - [ ] 实现裁决形态效果
   - [ ] 实现魔王形态效果

5. **遗物系统**
   - [ ] 设计专属遗物
   - [ ] 实现遗物效果

6. **平衡性调整**
   - [ ] 测试卡牌数值
   - [ ] 调整形态切换机制

### 低优先级
7. **额外内容**
   - [ ] 自定义事件
   - [ ] 专属药水
   - [ ] 成就系统

## 编译说明

### 构建项目
```bash
cd AmiyaMod
dotnet build Amiya.csproj
```

### 输出位置
- DLL: `AmiyaMod/.godot/mono/temp/bin/Debug/Amiya.dll`

### 安装到游戏
1. 将 `Amiya.dll` 复制到游戏的 mods 目录
2. 将 `Amiya.json` 复制到同一目录
3. 创建 `Amiya.pck` 资源包（需要 Godot 导出）

## 设计参考

详细的角色设计请参考：`../Amiya/阿米娅模组设计.txt`

核心设计理念：
- **多形态战斗** - 4种不同的战斗形态
- **资源管理** - 燃烬资源系统
- **策略深度** - 形态切换时机选择
- **明日方舟风格** - 保留原作特色

## 开发笔记

### 从 Typhon 学到的经验
1. 使用 `CustomCharacterModel` 作为角色基类
2. 使用 `CustomCardModel` 作为卡牌基类
3. 使用 `CustomPowerModel` 作为能力基类
4. 通过 `ModelDb` 注册和获取模型
5. 使用 Harmony 进行运行时补丁

### 关键实现细节
- 所有自定义类都需要 `partial` 修饰符（Godot 要求）
- 卡牌效果通过 `OnPlay` 方法实现
- 使用 `DynamicVar` 系统管理卡牌数值
- 使用 `Command` 模式执行游戏动作

## 版本历史

### v0.1.0 (当前)
- ✅ 完成核心框架搭建
- ✅ 实现基础卡牌
- ✅ 实现形态管理器
- ✅ 成功编译

## 贡献者

- 开发者: [Your Name]
- 基于 Typhon 模组学习和参考

## 许可证

待定

---

**注意**: 这是一个开发中的项目，许多功能尚未实现。当前版本仅包含核心框架和基础功能。
