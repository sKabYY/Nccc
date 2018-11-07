using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nccc.Tests
{
    static class Utils
    {
        public static string ReadFromAssembly(string path)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

    }
}
