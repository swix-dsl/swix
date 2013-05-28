using System.Collections.Generic;

namespace SimpleWixDsl.Swix.Model
{
    public class Directory
    {
        private readonly List<Directory> _children = new List<Directory>();

        public Directory(string name)
        {
            Id = Name = name;
        }

        public string Id { get; set; }
        public string Name { get; set; }

        public List<Directory> Children
        {
            get { return _children; }
        }
    }
}