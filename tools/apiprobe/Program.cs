// 游戏程序集（sts2.dll / BaseLib.dll）API 速查小工具：不加载程序集，直接读 .NET 元数据，
// 因此不需要游戏运行、也不会因为依赖缺失而失败。
//
// 用法（在仓库根目录下）：
//   dotnet run --project tools/apiprobe -- <类型名片段> [成员名片段|-] [dll路径]
//       例：dotnet run --project tools/apiprobe -- "Models.PowerModel" "Description"
//   dotnet run --project tools/apiprobe -- callers <成员名> [声明类型片段|-] [dll路径]
//       例：dotnet run --project tools/apiprobe -- callers get_MaxCardsInHand CardPile
// 默认 dll = AmiyaMod/ref/sts2.dll；查 BaseLib 时传 AmiyaMod/ref/BaseLib.dll。
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

const string DefaultDll = @"AmiyaMod\ref\sts2.dll";
string typeFilter = args.Length > 0 ? args[0] : "PowerModel";
string memberFilter = args.Length > 1 && args[1] != "-" ? args[1] : "";
string dll = (typeFilter != "callers" && args.Length > 2 && args[2] != "-") ? args[2] : DefaultDll;

if (!File.Exists(dll))
{
    Console.Error.WriteLine($"dll not found: {Path.GetFullPath(dll)} (run from the repo root)");
    return 2;
}

using var fs = File.OpenRead(dll);
using var pe = new PEReader(fs);
var md = pe.GetMetadataReader();

if (typeFilter == "callers")
{
    string memberName = args.Length > 1 ? args[1] : "get_Description";
    string declFilter = args.Length > 2 && args[2] != "-" ? args[2] : "";
    string dll2 = args.Length > 3 && args[3] != "-" ? args[3] : dll;
    if (dll2 != dll)
    {
        using var fs2 = File.OpenRead(dll2);
        using var pe2 = new PEReader(fs2);
        ScanCallers(pe2.GetMetadataReader(), pe2, memberName, declFilter);
        return 0;
    }
    ScanCallers(md, pe, memberName, declFilter);
    return 0;
}

foreach (var tdh in md.TypeDefinitions)
{
    var td = md.GetTypeDefinition(tdh);
    string full = FullName(td);
    if (!full.Contains(typeFilter, StringComparison.OrdinalIgnoreCase)) continue;
    string baseName = td.BaseType.IsNil ? "(none)" : TypeName(td.BaseType);
    Console.WriteLine($"=== {full} : {baseName}  [{td.Attributes}]");
    foreach (var mdh in td.GetMethods())
    {
        var m = md.GetMethodDefinition(mdh);
        string mn = md.GetString(m.Name);
        if (memberFilter.Length > 0 && !mn.Contains(memberFilter, StringComparison.OrdinalIgnoreCase)) continue;
        var sig = m.DecodeSignature(new SigProvider(), null);
        Console.WriteLine($"    {sig.ReturnType} {mn}({string.Join(", ", sig.ParameterTypes)})  [{m.Attributes}]");
    }
    foreach (var p in td.GetProperties())
    {
        var pd = md.GetPropertyDefinition(p);
        string pn = md.GetString(pd.Name);
        if (memberFilter.Length > 0 && !pn.Contains(memberFilter, StringComparison.OrdinalIgnoreCase)) continue;
        var psig = pd.DecodeSignature(new SigProvider(), null);
        Console.WriteLine($"    property {psig.ReturnType} {pn}");
    }
    foreach (var f in td.GetFields())
    {
        var fd = md.GetFieldDefinition(f);
        string fn = md.GetString(fd.Name);
        if (memberFilter.Length > 0 && !fn.Contains(memberFilter, StringComparison.OrdinalIgnoreCase)) continue;
        Console.WriteLine($"    field {fd.DecodeSignature(new SigProvider(), null)} {fn}");
    }
}
return 0;

// 反查调用方：把 IL 里的 MethodDef/MemberRef 标记跟目标成员比对（不做完整反汇编，够用且简单）。
static void ScanCallers(MetadataReader md, PEReader pe, string memberName, string declFilter)
{
    var targets = new HashSet<int>();
    foreach (var h in md.MemberReferences)
    {
        var mr = md.GetMemberReference(h);
        if (md.GetString(mr.Name) != memberName) continue;
        string owner = mr.Parent.Kind switch
        {
            HandleKind.TypeReference => RefNameOf(md, (TypeReferenceHandle)mr.Parent),
            HandleKind.TypeDefinition => TypeNameOf(md, (TypeDefinitionHandle)mr.Parent),
            _ => "?"
        };
        if (declFilter.Length > 0 && !owner.Contains(declFilter, StringComparison.OrdinalIgnoreCase)) continue;
        targets.Add(MetadataTokens.GetToken(h));
    }
    foreach (var tdh in md.TypeDefinitions)
    {
        var td = md.GetTypeDefinition(tdh);
        foreach (var mdh in td.GetMethods())
        {
            var mdef = md.GetMethodDefinition(mdh);
            if (md.GetString(mdef.Name) != memberName) continue;
            if (declFilter.Length > 0 && !TypeNameOf(md, tdh).Contains(declFilter, StringComparison.OrdinalIgnoreCase)) continue;
            targets.Add(MetadataTokens.GetToken(mdh));
        }
    }
    Console.WriteLine($"# target tokens for '{memberName}': {targets.Count}");
    foreach (var tdh in md.TypeDefinitions)
    {
        var td = md.GetTypeDefinition(tdh);
        string tname = NestedNameOf(md, tdh);
        foreach (var mdh in td.GetMethods())
        {
            var mdef = md.GetMethodDefinition(mdh);
            if (mdef.RelativeVirtualAddress == 0) continue;
            var body = pe.GetMethodBody(mdef.RelativeVirtualAddress);
            if (body.Size == 0) continue;
            var il = body.GetILBytes();
            bool hit = false;
            for (int i = 0; i + 4 <= il.Length && !hit; i++)
            {
                int tok = il[i] | (il[i + 1] << 8) | (il[i + 2] << 16) | (il[i + 3] << 24);
                if (targets.Contains(tok)) hit = true;
            }
            if (hit) Console.WriteLine($"  caller: {tname}.{md.GetString(mdef.Name)}");
        }
    }
}

// 嵌套类型（编译器生成的状态机 <Foo>d__1 等）要带上外层类型名，否则看不出是谁在调用
static string NestedNameOf(MetadataReader md, TypeDefinitionHandle h)
{
    var parts = new List<string>();
    var cur = h;
    while (!cur.IsNil)
    {
        var td = md.GetTypeDefinition(cur);
        string ns = md.GetString(td.Namespace);
        parts.Insert(0, (ns.Length > 0 ? ns + "." : "") + md.GetString(td.Name));
        cur = td.GetDeclaringType();
    }
    return string.Join(".", parts);
}

static string TypeNameOf(MetadataReader md, TypeDefinitionHandle h)
{
    var td = md.GetTypeDefinition(h);
    string ns = md.GetString(td.Namespace);
    return (ns.Length > 0 ? ns + "." : "") + md.GetString(td.Name);
}

static string RefNameOf(MetadataReader md, TypeReferenceHandle h)
{
    var tr = md.GetTypeReference(h);
    string ns = md.GetString(tr.Namespace);
    return (ns.Length > 0 ? ns + "." : "") + md.GetString(tr.Name);
}

string TypeName(EntityHandle h) => h.Kind switch
{
    HandleKind.TypeDefinition => FullName(md.GetTypeDefinition((TypeDefinitionHandle)h)),
    HandleKind.TypeReference => RefName(md.GetTypeReference((TypeReferenceHandle)h)),
    _ => h.Kind.ToString()
};

string FullName(TypeDefinition td) => (md.GetString(td.Namespace).Length > 0 ? md.GetString(td.Namespace) + "." : "") + md.GetString(td.Name);

string RefName(TypeReference tr) => (md.GetString(tr.Namespace).Length > 0 ? md.GetString(tr.Namespace) + "." : "") + md.GetString(tr.Name);

sealed class SigProvider : ISignatureTypeProvider<string, object?>
{
    public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[]";
    public string GetByReferenceType(string elementType) => "ref " + elementType;
    public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr";
    public string GetGenericInstantiation(string genericType, System.Collections.Immutable.ImmutableArray<string> typeArguments)
        => genericType + "<" + string.Join(",", typeArguments) + ">";
    public string GetGenericMethodParameter(object? genericContext, int index) => "!!" + index;
    public string GetGenericTypeParameter(object? genericContext, int index) => "!" + index;
    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
    public string GetPinnedType(string elementType) => elementType;
    public string GetPointerType(string elementType) => elementType + "*";
    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
    public string GetSZArrayType(string elementType) => elementType + "[]";
    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        => (reader.GetString(reader.GetTypeDefinition(handle).Namespace).Length > 0
            ? reader.GetString(reader.GetTypeDefinition(handle).Namespace) + "."
            : "") + reader.GetString(reader.GetTypeDefinition(handle).Name);
    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        => (reader.GetString(reader.GetTypeReference(handle).Namespace).Length > 0
            ? reader.GetString(reader.GetTypeReference(handle).Namespace) + "."
            : "") + reader.GetString(reader.GetTypeReference(handle).Name);
    public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        => "spec";
}
