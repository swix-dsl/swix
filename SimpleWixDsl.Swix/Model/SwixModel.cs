using System.Collections.Generic;

namespace SimpleWixDsl.Swix
{
    public class SwixModel
    {
        public SwixModel()
        {
            CabFiles = new List<CabFile>();
        }

        public List<CabFile> CabFiles { get; private set; }    
    }
}