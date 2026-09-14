using System;
using System.Linq;
using System.Reflection;
using Amiya.Patches;
using Amiya.Powers;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace Amiya;

/// <summary>
/// Official loader entry: assembly is scanned for [ModInitializer] classes.
/// See docs/apidump/API速查.md §1.3 (verified against game v0.111.0 ModManager).
/// </summary>
[ModInitializer("Init")]
public static class Entry
{
    /// <summary>
    /// 挂载额外的 pck（可选的附加资源包）。文件不存在时静默跳过——
    /// 例如只换了 dll 没换附件的旧安装，也应该能正常启动。
    /// </summary>
    private static void MountExtraPck(string fileName)
    {
        try
        {
            string full = AmiyaPaths.ModDir + "/" + fileName;
            if (!System.IO.File.Exists(full))
            {
                Log.Info($"[Amiya] extra pck not present, skipped: {fileName}");
                return;
            }
            bool ok = ProjectSettings.LoadResourcePack(full, true, 0);
            Log.Info($"[Amiya] extra pck mounted: {fileName} -> {ok}");
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] mounting {fileName} failed: {ex}");
        }
    }

    public static void Init()
    {
        ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        // 兜底图标补丁（AmiyaPowerIconPatch 等 [HarmonyPatch] 类）
        var harmony = new Harmony("com.amiya.mod");
        harmony.PatchAll(typeof(AmiyaPowerIconPatch).Assembly);
        Log.Info($"[Amiya] harmony patches applied, count={harmony.GetPatchedMethods().Count()}");
        // 先古遗物补丁：方法级特性被 PatchAll 静默跳过，改为显式注册
        AncientRelicPatch.Apply(harmony);
        // 人工制品补丁：同上，显式注册并打印结果
        AmiyaArtifactPatch.Apply(harmony);
        // 克雷松 boss：强制替换三层 boss（新局 + 读档两个补丁点）
        KresonBossPatch.Apply(harmony);
        // 事件「在冰原之上」的任务链：放弃卡牌奖励的计数点
        AmiyaQuestPatch.Apply(harmony);
        // 克雷松的地图节点/血条图标：独立小 pck，必须在这里挂载（引擎按 res:// 路径加载）
        MountExtraPck("Amiya_boss.pck");
        // 诊断：确认这两张图真的能被引擎按路径加载（这两条路径被地图预加载使用，读不到会让开图崩溃）
        foreach (string p in new[] { Amiya.Boss.KresonVisuals.IconPath, Amiya.Boss.KresonVisuals.IconOutlinePath })
        {
            try
            {
                bool exists = ResourceLoader.Exists(p);
                Texture2D tex = ResourceLoader.Load<Texture2D>(p, null, ResourceLoader.CacheMode.Reuse);
                Log.Info($"[Amiya] DIAG icon {p}: exists={exists} loaded={(tex != null ? tex.GetWidth() + "x" + tex.GetHeight() : "NULL")}");
            }
            catch (Exception ex)
            {
                Log.Error($"[Amiya] DIAG icon {p} failed: {ex.Message}");
            }
        }
        // 类级 [HarmonyPatch] 的挂载确认（本环境下 PatchAll 有可能静默跳过某些条目）
        foreach (var type in typeof(AmiyaPowerIconPatch).Assembly.GetTypes())
        {
            foreach (var patch in type.GetCustomAttributes<HarmonyPatch>())
            {
                try
                {
                    Type? declaring = patch.info.declaringType;
                    if (declaring == null)
                    {
                        continue;
                    }
                    System.Reflection.MethodInfo? target = patch.info.methodType switch
                    {
                        MethodType.Getter => AccessTools.PropertyGetter(declaring, patch.info.methodName),
                        MethodType.Setter => AccessTools.PropertySetter(declaring, patch.info.methodName),
                        _ => patch.info.argumentTypes is { Length: > 0 } args
                            ? AccessTools.Method(declaring, patch.info.methodName, args)
                            : AccessTools.Method(declaring, patch.info.methodName)
                    };
                    bool attached = target != null && Harmony.GetPatchInfo(target) != null;
                    Log.Info($"[Amiya] patch(class) {declaring.Name}.{patch.info.methodName}: {(attached ? "OK" : "NOT ATTACHED")}");
                }
                catch (Exception ex)
                {
                    Log.Info($"[Amiya] patch(class) {type.Name}: 诊断失败 {ex.Message}");
                }
            }
        }
        foreach (var type in typeof(AmiyaPowerIconPatch).Assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                foreach (var patch in method.GetCustomAttributes<HarmonyPatch>())
                {
                    var candidates = patch.info.declaringType!.GetMethods(
                        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(m => m.Name == patch.info.methodName).ToList();
                    bool attached = candidates.Any(m => Harmony.GetPatchInfo(m) != null);
                    Log.Info($"[Amiya] patch {patch.info.declaringType.Name}.{patch.info.methodName}: {(attached ? "OK" : "NOT ATTACHED")}");
                }
            }
        }

        // The game registers every AbstractModel subtype (incl. ours) into ModelDb at startup,
        // so combat hook subscribers must use canonical instances — never "new" a model.
        // AmiyaCombatHarness is stateless: it applies FormManagerPower to the Amiya player
        // at the first turn start; the power's own hooks take over afterwards.
        ModHelper.SubscribeForCombatStateHooks("amiya.form_manager", combatState =>
        {
            AbstractModel harness =
                ModelDb.GetByIdOrNull<AbstractModel>(ModelDb.GetId<AmiyaCombatHarness>())
                ?? throw new InvalidOperationException("AmiyaCombatHarness was not registered in ModelDb.");
            return new AbstractModel[] { harness };
        });

        Log.Info("Amiya Mod initialized!");

        // 【临时诊断】确认游戏对 Amiya.pck 内各文件的可见性（pck 已挂载，初始器在其后执行）
        try
        {
            Log.Info("[Amiya] DIAG tscn exists=" + FileAccess.FileExists("res://Amiya/scenes/AmiyaSelectBg.tscn"));
            Log.Info("[Amiya] DIAG src png exists=" + FileAccess.FileExists("res://Amiya/images/icon/character_icon_amiya.png"));
            Log.Info("[Amiya] DIAG sidecar(.import) exists=" + FileAccess.FileExists("res://Amiya/images/icon/character_icon_amiya.png.import"));
            Log.Info("[Amiya] DIAG ctex exists=" + FileAccess.FileExists("res://.godot/imported/character_icon_amiya.png-adf7143c0277556bc9700b9ebdedb41b.ctex"));
            var tscnBytes = FileAccess.GetFileAsBytes("res://Amiya/scenes/AmiyaSelectBg.tscn");
            Log.Info("[Amiya] DIAG tscn bytes=" + (tscnBytes?.Length ?? -1));
            using var dir = DirAccess.Open("res://Amiya");
            if (dir != null)
            {
                Log.Info("[Amiya] DIAG dir res://Amiya -> [" + string.Join(", ", dir.GetFiles().ToArray()) + "]");
                using var dir2 = DirAccess.Open("res://Amiya/scenes");
                Log.Info("[Amiya] DIAG dir res://Amiya/scenes -> " + (dir2 == null ? "NULL" : "[" + string.Join(", ", dir2.GetFiles().ToArray()) + "]"));
                using var dir3 = DirAccess.Open("res://Amiya/images/icon");
                Log.Info("[Amiya] DIAG dir res://Amiya/images/icon -> " + (dir3 == null ? "NULL" : "[" + string.Join(", ", dir3.GetFiles().ToArray()) + "]"));
            }
            else
            {
                Log.Info("[Amiya] DIAG dir res://Amiya -> NULL");
            }
            bool ctexExists = ResourceLoader.Exists("res://.godot/imported/character_icon_amiya.png-adf7143c0277556bc9700b9ebdedb41b.ctex");
            Log.Info("[Amiya] DIAG ResourceLoader.Exists(ctex)=" + ctexExists);
            var tex = ResourceLoader.Load<Texture2D>("res://.godot/imported/character_icon_amiya.png-adf7143c0277556bc9700b9ebdedb41b.ctex");
            Log.Info("[Amiya] DIAG ctex load -> " + (tex == null ? "NULL" : ("OK " + tex.GetWidth() + "x" + tex.GetHeight())));
            var scene = ResourceLoader.Load<PackedScene>("res://Amiya/scenes/AmiyaSelectBg.tscn");
            Log.Info("[Amiya] DIAG tscn load -> " + (scene == null ? "NULL" : "OK"));
            var iconScene = ResourceLoader.Load<PackedScene>("res://Amiya/scenes/AmiyaIcon.tscn");
            Log.Info("[Amiya] DIAG icon scene load -> " + (iconScene == null ? "NULL" : "OK"));
        }
        catch (Exception ex)
        {
            Log.Error("[Amiya] DIAG failed: " + ex);
        }

        // 【临时诊断】spine-godot load_from_file 加载阿米娅骨架 + 完整解析验证
        try
        {
            var skelFile = Godot.ClassDB.Instantiate("SpineSkeletonFileResource").AsGodotObject();
            var loadResult = skelFile.Call("load_from_file", "D:/SteamLibrary/steamapps/common/Slay the Spire 2/mods/Amiya/spine/amiya3_std.skel");
            Log.Info($"[Amiya] spine skel load_from_file result: {loadResult}");
            var atlasFile = Godot.ClassDB.Instantiate("SpineAtlasResource").AsGodotObject();
            var atlasResult = atlasFile.Call("load_from_atlas_file", "D:/SteamLibrary/steamapps/common/Slay the Spire 2/mods/Amiya/spine/amiya3.atlas");
            Log.Info($"[Amiya] spine atlas load_from_atlas_file result: {atlasResult}");
            if ((int)loadResult == 0 && (int)atlasResult == 0)
            {
                var data = Godot.ClassDB.Instantiate("SpineSkeletonDataResource").AsGodotObject();
                data.Set("skeleton_file_res", skelFile);
                data.Set("atlas_res", atlasFile);
                var loaded = (bool)data.Call("is_skeleton_data_loaded");
                Log.Info($"[Amiya] spine skeleton parsed: {loaded}");
                if (loaded)
                {
                    var anims = data.Call("get_animations").AsGodotArray();
                    var names = string.Join(", ", anims.Select(a => (string)((GodotObject)a).Call("get_name")));
                    Log.Info($"[Amiya] spine animations: {names}");
                    Log.Info($"[Amiya] spine version={data.Call("get_version")} hash={data.Call("get_hash")} refScale={data.Call("get_reference_scale")}");
                }
                // 崩溃定位压测改在 AmiyaVisualSwapPatch 的 spine-ready 回调里同步执行。
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] spine load_from_file failed: {ex}");
        }
    }
}