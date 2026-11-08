using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

// Usage: MetaDump <dll> <typeNameRegex> [membersRegex]
// Reads metadata only (no assembly load), so no resolver / stack overflow issues.
var path = args[0];
var typeRx = new System.Text.RegularExpressions.Regex(args[1]);
var memRx = args.Length > 2 ? new System.Text.RegularExpressions.Regex(args[2]) : null;

using var fs = File.OpenRead(path);
using var pe = new PEReader(fs);
var md = pe.GetMetadataReader();

foreach (var h in md.TypeDefinitions)
{
    var t = md.GetTypeDefinition(h);
    var name = md.GetString(t.Name);
    var ns = md.GetString(t.Namespace);
    var fullName = t.GetDeclaringType().IsNil 
        ? $"{(ns.Length > 0 ? ns + "." : "")}{name}"
        : $"{md.GetString(md.GetTypeDefinition(t.GetDeclaringType()).Name)}+{name}";
    if (!typeRx.IsMatch(fullName) && !typeRx.IsMatch(name)) continue;
    Console.WriteLine($"TYPE {fullName}");
    if (memRx == null) continue;
    foreach (var f in t.GetFields())
    {
        var fn = md.GetString(md.GetFieldDefinition(f).Name);
        if (memRx.IsMatch(fn)) Console.WriteLine($"   F {fn}");
    }
    foreach (var m in t.GetMethods())
    {
        var md2 = md.GetMethodDefinition(m);
        var mn = md.GetString(md2.Name);
        if (!memRx.IsMatch(mn)) continue;
        var ps = string.Join(", ", md2.GetParameters()
            .Select(p => md.GetParameter(p))
            .Where(p => p.SequenceNumber > 0)
            .OrderBy(p => p.SequenceNumber)
            .Select(p => md.GetString(p.Name)));
        Console.WriteLine($"   M {mn}({ps})");
    }
}
