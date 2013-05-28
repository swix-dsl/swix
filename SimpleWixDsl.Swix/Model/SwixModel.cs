using System.Collections.Generic;

namespace SimpleWixDsl.Swix.Model
{
    public class SwixModel
    {
        public SwixModel()
        {
            ComponentGroups = new List<ComponentGroup>();
        }

        public List<ComponentGroup> ComponentGroups { get; set; }
    }
}