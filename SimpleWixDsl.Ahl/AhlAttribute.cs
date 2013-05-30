namespace SimpleWixDsl.Ahl
{
    public class AhlAttribute
    {
        public AhlAttribute(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; set; } 
        public string Value { get; set; }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Key, Value);
        }
    }
}