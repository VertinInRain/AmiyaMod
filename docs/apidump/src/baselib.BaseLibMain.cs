using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BaseLib.Abstracts;
using BaseLib.Config;
using BaseLib.Extensions;
using BaseLib.Patches.Content;
using BaseLib.Patches.Saves;
using BaseLib.Patches.Utils;
using BaseLib.Utils;
using BaseLib.Utils.NodeFactories;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace BaseLib;

[ModInitializer("Initialize")]
public static class BaseLibMain
{
	[ThreadStatic]
	public static bool IsMainThread;

	public const string ModId = "BaseLib";

	[CompilerGenerated]
	private static Harmony <MainHarmony>k__BackingField;

	private static nint _holder;

	public static Logger Logger { get; } = new Logger("BaseLib", LogType.Generic);

	internal static Harmony MainHarmony
	{
		get
		{
			if (<MainHarmony>k__BackingField == null)
			{
				<MainHarmony>k__BackingField = new Harmony("BaseLib");
			}
			return <MainHarmony>k__BackingField;
		}
	}

	public static void Initialize()
	{
		Libgcc();
		IsMainThread = true;
		OS.AddLogger((Logger)(object)new LogListener());
		try
		{
			NodeFactory.Init();
		}
		catch (Exception ex)
		{
			Logger.Error(ex.ToString());
		}
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		ScriptManagerBridge.LookupScriptsInAssembly(executingAssembly);
		ModConfigRegistry.Register("BaseLib", new BaseLibConfig());
		try
		{
			ExtendedSavePatches.Patch(MainHarmony);
			TheBigPatchToCardPileCmdAdd.Patch(MainHarmony);
			CustomBadgesPatch.Patch(MainHarmony);
		}
		catch (Exception ex2)
		{
			Logger.Error(ex2.ToString());
		}
		MainHarmony.TryPatchAll(executingAssembly);
		CustomLocTableManager.Register("card_modifiers");
		ModCredits.Register("BaseLib", new ModCredits.Section("TEAM"));
	}

	[DllImport("libdl.so.2")]
	private static extern nint dlopen(string filename, int flags);

	[DllImport("libdl.so.2")]
	private static extern nint dlerror();

	[DllImport("libdl.so.2")]
	private static extern nint dlsym(nint handle, string symbol);

	private static void Libgcc()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			Logger.Info("Running on Linux, manually dlopen libgcc for Harmony");
			_holder = dlopen("libgcc_s.so.1", 258);
			if (_holder == IntPtr.Zero)
			{
				Logger.Info("Or Nor: " + Marshal.PtrToStringAnsi(dlerror()));
			}
		}
	}
}
