﻿using System.Collections.Generic;

namespace SimpleWixDsl.Swix
{
    public class SwixModel
    {
        private int? _diskIdStartFrom;

        public SwixModel()
        {
            CabFiles = new List<CabFile>();
            RootDirectory = new WixTargetDirectory("root", null);
            Components = new List<WixComponent>();
        }

        public List<CabFile> CabFiles { get; private set; }

        public WixTargetDirectory RootDirectory { get; private set; }

        public List<WixComponent> Components { get; private set; }

        public int DiskIdStartFrom
        {
            get { return _diskIdStartFrom ?? 1; }
            set
            {
                if (_diskIdStartFrom != null && _diskIdStartFrom != value)
                    throw new SwixException("DiskIdStartFrom property is already set");
                _diskIdStartFrom = value;
            }
        }
    }
}