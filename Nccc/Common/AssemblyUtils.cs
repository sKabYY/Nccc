using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Nccc.Common
{
    public static class AssemblyUtils
    {
        public static string ReadString(this Assembly assembly, string path)
        {
            using (var stream = assembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"resource not found: {path}");
                }
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
