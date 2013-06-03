using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimpleWixDsl.Swix
{
    public class GuidProvider
    {
        private static readonly Regex LineRegex = new Regex(@"^(?<path>\w+\\""(""""|[^""])*"")=(?<guid>[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})$",
                                                        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public static GuidProvider CreateFromStream(StreamReader source)
        {
            var result = new GuidProvider();
            string line;
            while ((line = source.ReadLine()) != null)
            {
                var match = LineRegex.Match(line);
                var path = match.Groups["path"].Value;
                var guid = Guid.Parse(match.Groups["guid"].Value);
                result._map.Add(path, guid);
            }
            return result;
        }

        private readonly Dictionary<string, Guid> _map = new Dictionary<string, Guid>();

        public Guid Get(SwixGuidType type, string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            path = path.Replace("\"", "\"\"");
            var key = String.Format("{0}\\\"{1}\"", type, path);

            Guid result;
            if (!_map.TryGetValue(key, out result))
            {
                result = Guid.NewGuid();
                _map[key] = result;
            }
            return result;
        }

        public void SaveToStream(StreamWriter stream)
        {
            foreach (var pair in _map.OrderBy(pair => pair.Key))
            {
                var key = pair.Key;
                var guid = pair.Value;
                stream.WriteLine("{0}={1:D}", key, guid);
            }
        }
    }
}