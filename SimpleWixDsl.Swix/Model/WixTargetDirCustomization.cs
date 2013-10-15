using System;

namespace SimpleWixDsl.Swix
{
    public enum RegistryRoot
    {
// ReSharper disable InconsistentNaming
        HKLM,
        HKCU,
        HKCR,
        HKU,
// ReSharper restore InconsistentNaming
    }

    public class WixTargetDirCustomization
    {
        public WixTargetDirCustomization(WixTargetDirectory parent, string registryStorageKey, string wixPublicPropertyName)
        {
            Parent = parent;
            WixPublicPropertyName = wixPublicPropertyName;

            int idx = registryStorageKey.IndexOf('\\');
            if (idx == -1)
                throw new SwixItemParsingException("Invalid registry path");
            var regRootStr = registryStorageKey.Substring(0, idx);
            RegistryRoot regRoot;
            if (!Enum.TryParse(regRootStr, true, out regRoot))
                throw new SwixItemParsingException("Invalid registry root");
            RegistryRoot = regRoot;
            RegistryStorageKey = registryStorageKey.Substring(idx + 1);
        }

        public WixTargetDirectory Parent { get; private set; }

        /// <summary>
        /// This is the name of public property, that allows user to customize value during install via msiexec
        /// arguments if he wants. Should be ALL CAPS. Mandatory.
        /// </summary>
        public string WixPublicPropertyName { get; private set; }

        public RegistryRoot RegistryRoot { get; private set; }

        /// <summary>
        /// This is the key in the Registry (key, not the value! - i.e. it should be path to Registry folder)
        /// where value of the property will be preserved after install. It is restored then during uninstall,
        /// reinstall and others. Mandatory.
        /// </summary>
        public string RegistryStorageKey { get; private set; }

        /// <summary>
        /// This the default value of the path to the target WIX dir during install. May contain WIX expandable
        /// properties like [NAME]. The value is set/expanded before CostFinalize, so the properties better be
        /// set by this time. Optional. If not set - standard WIX value will be used as it is in the directory tree.
        /// </summary>
        public string DefaultValue { get; set; }
    }
}