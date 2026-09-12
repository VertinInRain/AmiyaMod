using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using NecrobinderCardPortraits.NecrobinderCardPortraitsCode;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: TargetFramework(".NETCoreApp,Version=v9.0", FrameworkDisplayName = ".NET 9.0")]
[assembly: AssemblyCompany("NecrobinderCardPortraits")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]
[assembly: AssemblyProduct("NecrobinderCardPortraits")]
[assembly: AssemblyTitle("NecrobinderCardPortraits")]
[assembly: AssemblyHasScripts(new Type[] { typeof(MainFile) })]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("1.0.0.0")]
[module: UnverifiableCode]
[module: RefSafetyRules(11)]
namespace GodotPlugins.Game
{
	internal static class Main
	{
		[UnmanagedCallersOnly(EntryPoint = "godotsharp_game_main_init")]
		private static godot_bool InitializeFromGameProject(nint godotDllHandle, nint outManagedCallbacks, nint unmanagedCallbacks, int unmanagedCallbacksSize)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Expected O, but got Unknown
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				DllImportResolver resolver = new GodotDllImportResolver((IntPtr)godotDllHandle).OnResolveDllImport;
				NativeLibrary.SetDllImportResolver(typeof(GodotObject).Assembly, resolver);
				NativeFuncs.Initialize((IntPtr)unmanagedCallbacks, unmanagedCallbacksSize);
				ManagedCallbacks.Create((IntPtr)outManagedCallbacks);
				ScriptManagerBridge.LookupScriptsInAssembly(typeof(Main).Assembly);
				return (godot_bool)1;
			}
			catch (Exception value)
			{
				Console.Error.WriteLine(value);
				return GodotBoolExtensions.ToGodotBool(false);
			}
		}
	}
}
namespace NecrobinderCardPortraits.NecrobinderCardPortraitsCode
{
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	public static class CardPortraitReplacementPatch
	{
		private const string BasePath = "res://NecrobinderCardPortraits/card_portraits/necrobinder/";

		private static readonly Dictionary<Type, string> Replacements = new Dictionary<Type, string>
		{
			{
				typeof(Unleash),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/Unleash.png"
			},
			{
				typeof(StrikeNecrobinder),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/StrikeNecrobinder.png"
			},
			{
				typeof(DefendNecrobinder),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/DefendNecrobinder.png"
			},
			{
				typeof(Bodyguard),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/Bodyguard.png"
			},
			{
				typeof(Transfigure),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/Transfigure.png"
			},
			{
				typeof(Flatten),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/Flatten.png"
			},
			{
				typeof(Afterlife),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/Afterlife.png"
			},
			{
				typeof(RightHandHand),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/RightHandHand.png"
			},
			{
				typeof(HighFive),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/HighFive.png"
			},
			{
				typeof(BansheesCry),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/BansheesCry.png"
			},
			{
				typeof(Dirge),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/Dirge.png"
			},
			{
				typeof(CaptureSpirit),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/CaptureSpirit.png"
			},
			{
				typeof(Cleanse),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/Cleanse.png"
			},
			{
				typeof(SharedFate),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/SharedFate.png"
			},
			{
				typeof(Sacrifice),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/Sacrifice.png"
			},
			{
				typeof(Hang),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/Hang.png"
			},
			{
				typeof(BorrowedTime),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/BorrowedTime.png"
			},
			{
				typeof(DanseMacabre),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/DanseMacabre.png"
			},
			{
				typeof(Friendship),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/Friendship.png"
			},
			{
				typeof(LegionOfBone),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/LegionOfBone.png"
			},
			{
				typeof(SculptingStrike),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/SculptingStrike.png"
			},
			{
				typeof(Melancholy),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/Melancholy.png"
			},
			{
				typeof(Eidolon),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/Eidolon.png"
			},
			{
				typeof(Defy),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/Defy.png"
			},
			{
				typeof(Neurosurge),
				"res://NecrobinderCardPortraits/card_portraits/necrobinder/Neurosurge.png"
			}
		};

		private static void Postfix(CardModel __instance, ref string __result)
		{
			if (__instance != null)
			{
				Type type = ((object)__instance).GetType();
				if (Replacements.TryGetValue(type, out string value) && ResourceLoader.Exists(value, ""))
				{
					__result = value;
				}
			}
		}
	}
	[ModInitializer("Initialize")]
	[ScriptPath("res://NecrobinderCardPortraitsCode/MainFile.cs")]
	public class MainFile : Node
	{
		public class MethodName : MethodName
		{
			public static readonly StringName Initialize = StringName.op_Implicit("Initialize");
		}

		public class PropertyName : PropertyName
		{
		}

		public class SignalName : SignalName
		{
		}

		public const string ModId = "NecrobinderCardPortraits";

		public static Logger Logger { get; }

		public static void Initialize()
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			new Harmony("NecrobinderCardPortraits").PatchAll();
			Logger.Info("Necrobinder card portrait replacements initialized.", 1);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<MethodInfo> GetGodotMethodList()
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			return new List<MethodInfo>(1)
			{
				new MethodInfo(MethodName.Initialize, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null)
			};
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			if ((ref method) == MethodName.Initialize && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				Initialize();
				ret = default(godot_variant);
				return true;
			}
			return ((Node)this).InvokeGodotClassMethod(ref method, args, ref ret);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			if ((ref method) == MethodName.Initialize && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				Initialize();
				ret = default(godot_variant);
				return true;
			}
			ret = default(godot_variant);
			return false;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool HasGodotClassMethod(in godot_string_name method)
		{
			if ((ref method) == MethodName.Initialize)
			{
				return true;
			}
			return ((Node)this).HasGodotClassMethod(ref method);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void SaveGodotObjectData(GodotSerializationInfo info)
		{
			((GodotObject)this).SaveGodotObjectData(info);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void RestoreGodotObjectData(GodotSerializationInfo info)
		{
			((GodotObject)this).RestoreGodotObjectData(info);
		}

		static MainFile()
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Expected O, but got Unknown
			Logger = new Logger("NecrobinderCardPortraits", (LogType)0);
		}
	}
}
