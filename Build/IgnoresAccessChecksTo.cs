// Compiled into every assembly in this solution (see Directory.Build.props).
//
// The mod compiles against *publicized* copies of the game assemblies (References\Valheim) so it can reach
// private members. At runtime the real, non-publicized assemblies are loaded and Unity's Mono JIT verifies
// member accessibility, throwing FieldAccessException / MethodAccessException, unless the calling assembly
// asks to skip verification. Two mechanisms are declared so both Mono flavours are covered:
//
//   * SecurityPermission(SkipVerification) + UnverifiableCode: Mono skips IL verification (including
//     visibility checks) for assemblies that request it (mini_assembly_can_skip_verification).
//   * IgnoresAccessChecksTo: honoured by runtimes that implement the .NET Core attribute; the type is not
//     part of the BCL, so it is declared here by its well-known name.

using System;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;

#pragma warning disable CS0618 // Code Access Security is obsolete on modern .NET, but Mono still honours the request.
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
[module: UnverifiableCode]

[assembly: IgnoresAccessChecksTo("assembly_valheim")]
[assembly: IgnoresAccessChecksTo("assembly_guiutils")]
[assembly: IgnoresAccessChecksTo("assembly_utils")]
[assembly: IgnoresAccessChecksTo("gui_framework")]
[assembly: IgnoresAccessChecksTo("assembly_postprocessing")]
[assembly: IgnoresAccessChecksTo("assembly_sunshafts")]

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
            AssemblyName = assemblyName;
        }

        public string AssemblyName { get; }
    }
}
