// 反编译游戏程序集里的指定类型/方法，用于搞清引擎语义（联机同步、抽牌上限等）。
// 用法（仓库根目录）：dotnet run --project tools/ildump -- <输出目录> <dll> <类型名片段> [方法名片段]
// 例：dotnet run --project tools/ildump -- out AmiyaMod/ref/sts2.dll CardPileCmd DrawInternal
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

if (args.Length < 3)
{
    Console.Error.WriteLine("usage: ildump <outDir> <dll> <typeFilter> [methodFilter]");
    return 2;
}
string outDir = args[0];
string dll = args[1];
string typeFilter = args[2];
string methodFilter = args.Length > 3 ? args[3] : "";

Directory.CreateDirectory(outDir);
var settings = new DecompilerSettings(LanguageVersion.Latest)
{
    ThrowOnAssemblyResolveErrors = false,
    ShowXmlDocumentation = false
};
var decompiler = new CSharpDecompiler(Path.GetFullPath(dll), settings);
var name = new FullTypeName(typeFilter);
int files = 0;
foreach (var type in decompiler.TypeSystem.MainModule.TypeDefinitions)
{
    string full = type.FullName;
    if (!full.Contains(typeFilter, StringComparison.OrdinalIgnoreCase)) continue;

    var sb = new StringBuilder();
    if (methodFilter.Length == 0)
    {
        sb.AppendLine(decompiler.DecompileTypeAsString(type.FullTypeName));
    }
    else
    {
        foreach (var m in type.Methods)
        {
            if (!m.Name.Contains(methodFilter, StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                sb.AppendLine("// ==== " + m.FullName);
                sb.AppendLine(decompiler.DecompileAsString(m.MetadataToken));
            }
            catch (Exception ex)
            {
                sb.AppendLine("// decompile failed: " + ex.Message);
            }
        }
    }
    if (sb.Length == 0) continue;
    string safe = full;
    foreach (char bad in Path.GetInvalidFileNameChars()) safe = safe.Replace(bad, '_');
    safe = safe.Replace('.', '_').Replace('`', '_');
    string file = Path.Combine(outDir, safe + ".cs");
    File.WriteAllText(file, sb.ToString(), new UTF8Encoding(false));
    Console.WriteLine($"wrote {file} ({sb.Length} chars)");
    files++;
}
Console.WriteLine($"types written: {files}");
return files > 0 ? 0 : 1;
