using Aardvark.Base;
using Aardvark.Embree;
using Aardvark.Rendering;
using Aardvark.SceneGraph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable IDE0060 // Remove unused parameter

namespace Test;

class Program
{
    static void Main(string[] args)
    {
        //RenderSimpleTest.Run();

        BvhTest.Run();
    }
}
