using System;

namespace SimpleWixDsl.Swix
{
    public class Service
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Args { get; set; }
        public ServiceStartupType Start { get; set; }
        public string Vital { get; set; }
        public ServiceHostingType Type { get; set; }
        public ServiceErrorControl ErrorControl { get; set; }

        public static Service FromContext(string key, IAttributeContext attributes)
        {
            var result = new Service(key);

            var id = attributes.GetInheritedAttribute("id");
            if (id != null)
                result.Id = id;

            var displayName = attributes.GetInheritedAttribute("displayName");
            if (displayName != null)
                result.DisplayName = displayName;

            var description = attributes.GetInheritedAttribute("description");
            if (description != null)
                result.Description = description;

            var args = attributes.GetInheritedAttribute("args");
            if (args != null)
                result.Args = args;

            var startStr = attributes.GetInheritedAttribute("start");
            if (startStr != null)
            {
                ServiceStartupType start;
                if (!Enum.TryParse(startStr, true, out start))
                    throw new SwixSemanticException("'start' attribute should be one of these values: 'auto', 'demand' or 'disabled'");
                result.Start = start;
            }

            var vital = attributes.GetInheritedAttribute("vital");
            if (vital != null && vital != "yes" && vital != "no")
                throw new SwixSemanticException("'vital' attribute should be either 'yes' or 'no'");
            result.Vital = vital;

            var typeStr = attributes.GetInheritedAttribute("type");
            if (startStr != null)
            {
                ServiceHostingType type;
                if (!Enum.TryParse(typeStr, true, out type))
                    throw new SwixSemanticException("'type' attribute should be one of these values: 'ownProcess', 'sharedProcess', 'systemDriver' or 'kernelDriver'");
                result.Type = type;
            }

            var errorControlStr = attributes.GetInheritedAttribute("errorControl");
            if (errorControlStr != null)
            {
                ServiceErrorControl errorControl;
                if (!Enum.TryParse(errorControlStr, true, out errorControl))
                    throw new SwixSemanticException("'errorControl' attribute should be one of these values: 'ignore', 'normal' or 'critical'");
                result.ErrorControl = errorControl;
            }

            return result;
        }

        private Service(string name)
        {
            Name = name;
        }
    }

    public enum ServiceHostingType
    {
        Unset = 0,
        OwnProcess,
        SharedProcess,
        KernelDriver,
        SystemDriver,
    }

    public enum ServiceErrorControl
    {
        Unset = 0,
        Ignore,
        Normal,
        Critical,
    }

    public enum ServiceStartupType
    {
        Unset = 0,
        Auto,
        Demand,
        Disabled,
    }
}