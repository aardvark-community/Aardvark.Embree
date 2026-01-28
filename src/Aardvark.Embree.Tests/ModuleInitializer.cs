using System.Runtime.CompilerServices;

namespace Aardvark.Embree.Tests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Aardvark.Base.Aardvark.Init();
    }
}
