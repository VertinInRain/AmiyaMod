using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace Amiya;

[ModInitializer("Init")]
public class Entry
{
    private static Harmony? _harmony;
    
    public static void Init()
    {
        _harmony = new Harmony("sts2.amiya");
        _harmony.PatchAll();
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
        Log.Debug("Amiya Mod initialized!");
    }
}
