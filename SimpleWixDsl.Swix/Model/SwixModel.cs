﻿using System.Collections.Generic;

namespace SimpleWixDsl.Swix
{
    public class SwixModel
    {
        public SwixModel()
        {
            CabFiles = new List<CabFile>();
            RootDirectory = new WixTargetDirectory("root", null);
            Components = new List<WixComponent>();
        }

        public List<CabFile> CabFiles { get; private set; }

        public WixTargetDirectory RootDirectory { get; private set; }

        public List<WixComponent> Components { get; private set; }

        public int DiskIdStartFrom { get; set; }
    }
}