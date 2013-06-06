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
                result._loadedFromFileGuids.Add(path, guid);
            }
            return result;
        }

        // Loaded from file are just for reference, current ones will be re-saved.
        // Values are moved from loaded to current when used.
        // Thus old values that were not used in current session will not be re-saved again.
        private readonly Dictionary<string, Guid> _loadedFromFileGuids = new Dictionary<string, Guid>();
        private readonly Dictionary<string, Guid> _currentGuids = new Dictionary<string, Guid>();

        public Guid Get(SwixGuidType type, string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            path = path.Replace("\"", "\"\"");
            var key = String.Format("{0}\\\"{1}\"", type, path);

            Guid result;
            if (!_currentGuids.TryGetValue(key, out result))
            {
                if (_loadedFromFileGuids.TryGetValue(key, out result))
                {
                    _currentGuids[key] = result;
                    return result;
                }
                result = Guid.NewGuid();
                _currentGuids[key] = result;
            }
            return result;
        }

        public void SaveToStream(StreamWriter stream)
        {
            foreach (var pair in _currentGuids.OrderBy(pair => pair.Key))
            {
                var key = pair.Key;
                var guid = pair.Value;
                stream.WriteLine("{0}={1:D}", key, guid);
            }
        }
    }
}