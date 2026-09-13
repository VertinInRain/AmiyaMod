using System;
using System.Collections.Generic;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Amiya.Boss;

/// <summary>
/// 克雷松的战斗立绘：用运行时从 mods 目录读入的 PNG（不经 pck/ctex，和卡图同一套做法），
/// 交给 BaseLib 的 NCreatureVisuals 工厂生成「贴图版」生物视觉（Sprite2D = %Visuals，
/// 自动补 Bounds / IntentPos / CenterPos 等必需节点）。
/// 三个状态（无敌 / 解除无敌 / 死亡）只是换 Sprite2D.Texture，不需要额外节点。
/// </summary>
public static class KresonVisuals
{
    public enum State
    {
        Invincible,
        Released,
        Dead
    }

    private static readonly string Dir = AmiyaPaths.SpineDir + "/boss";

    private static readonly Dictionary<string, Texture2D?> Cache = new();

    /// <summary>地图 boss 节点图标（游戏会在后面自动接 .png / _outline.png）。</summary>
    public const string IconNodePath = "res://Amiya/boss/kreson_icon";

    public const string IconPath = IconNodePath + ".png";

    public const string IconOutlinePath = IconNodePath + "_outline.png";

    /// <summary>
    /// 把图标 PNG 注册进资源缓存：地图节点 / 顶部血条 / 历史记录都是用
    /// res:// 路径直接加载的，mod 的 pck 里没有这些图，所以用 TakeOverPath
    /// 让引擎按路径加载时直接命中我们运行时读入的纹理（与卡图同一套做法）。
    /// </summary>
    public static void RegisterIcons()
    {
        RegisterIcon("kreson_icon.png", IconPath);
        RegisterIcon("kreson_icon_outline.png", IconOutlinePath);
    }

    private static void RegisterIcon(string file, string resourcePath)
    {
        try
        {
            Image img = Image.LoadFromFile(Dir + "/" + file);
            if (img == null || img.IsEmpty())
            {
                Log.Error($"[Amiya] kreson icon missing: {Dir}/{file}");
                return;
            }
            Texture2D tex = ImageTexture.CreateFromImage(img);
            tex.TakeOverPath(resourcePath);
            Log.Info($"[Amiya] kreson icon registered: {file} -> {resourcePath}");
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] kreson icon register failed ({file}): {ex}");
        }
    }

    private static string FileFor(State state) => state switch
    {
        State.Released => "kreson_released.png",
        State.Dead => "kreson_dead.png",
        _ => "kreson_invincible.png"
    };

    /// <summary>由 MonsterModel.CreateCustomVisuals 调用（主线程）。失败返回 null 时游戏会退回占位怪物，不会崩。</summary>
    public static NCreatureVisuals? Build()
    {
        try
        {
            Texture2D? tex = Load(State.Invincible);
            if (tex == null)
            {
                Log.Error("[Amiya] kreson visuals: 立绘缺失，使用游戏占位怪物");
                return null;
            }
            NCreatureVisuals visuals = NodeFactory<NCreatureVisuals>.CreateFromResource(tex);
            Log.Info("[Amiya] kreson visuals built from texture");
            return visuals;
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] kreson visuals build failed: {ex}");
            return null;
        }
    }

    /// <summary>切换立绘状态（无敌 ⇄ 解除无敌 ⇄ 死亡）。</summary>
    public static void SetState(Creature creature, State state)
    {
        try
        {
            NCreature? node = creature.GetCreatureNode();
            NCreatureVisuals? visuals = node?.Visuals;
            if (visuals?.Body is not Sprite2D sprite)
            {
                return;
            }
            Texture2D? tex = Load(state);
            if (tex != null && sprite.Texture != tex)
            {
                sprite.Texture = tex;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] kreson visuals swap failed: {ex}");
        }
    }

    private static Texture2D? Load(State state)
    {
        string file = FileFor(state);
        if (Cache.TryGetValue(file, out Texture2D? cached))
        {
            return cached;
        }
        Texture2D? tex = null;
        try
        {
            Image img = Image.LoadFromFile(Dir + "/" + file);
            if (img != null && !img.IsEmpty())
            {
                tex = ImageTexture.CreateFromImage(img);
                Log.Info($"[Amiya] kreson art loaded: {file} ({img.GetWidth()}x{img.GetHeight()})");
            }
            else
            {
                Log.Error($"[Amiya] kreson art missing or empty: {Dir}/{file}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] kreson art load failed ({file}): {ex}");
        }
        Cache[file] = tex;
        return tex;
    }
}
