namespace SimpleWixDsl.Swix
{
    public class Shortcut
    {
        public static Shortcut FromContext(string name, IAttributeContext context)
        {
            var result = new Shortcut(name);

            var shortcutTargetDirRef = context.GetInheritedAttribute("shortcutTargetDirRef");
            if (shortcutTargetDirRef != null)
                result.TargetDirRef = shortcutTargetDirRef;
            else
                throw new SwixSemanticException("shortcutTargetDirRef attribute is mandatory for all shortcuts");

            var shortcutTargetDir = context.GetInheritedAttribute("shortcutTargetDir");
            if (shortcutTargetDir != null)
                result.TargetDir = shortcutTargetDir;

            var args = context.GetInheritedAttribute("args");
            if (args != null && string.IsNullOrWhiteSpace(args))
                throw new SwixSemanticException("Shortcut's 'args' cannot be empty. Either set it to some value or remove altogether");
            result.Args = args;

            return result;
        }

        private Shortcut(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public string Args { get; set; }

        public string TargetDirRef { get; set; }

        public string TargetDir { get; set; }
    }
}