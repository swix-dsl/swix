using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public delegate ISemanticContext MetaHandler(object callTarget, string key, IAttributeContext context);
    public delegate ISemanticContext SectionHandler(object callTarget, IAttributeContext sectionContext);
    public delegate ISemanticContext ItemHandler(object callTarget, string key, IAttributeContext itemContext);

    public class SemanticContextReflectionInfo
    {
        private static readonly ConditionalWeakTable<Type, SemanticContextReflectionInfo> Cache = new ConditionalWeakTable<Type, SemanticContextReflectionInfo>();

        public static SemanticContextReflectionInfo Get(Type target)
        {
            SemanticContextReflectionInfo result;
            if (Cache.TryGetValue(target, out result))
                return result;

            result = new SemanticContextReflectionInfo(target);
            Cache.Add(target, result);
            return result;
        }

        private readonly Dictionary<string, SectionHandler> _sectionHandlers = new Dictionary<string, SectionHandler>();
        private readonly Dictionary<string, MetaHandler> _metaHandlers = new Dictionary<string, MetaHandler>();
        private ItemHandler _itemHandler;

        private SemanticContextReflectionInfo(Type targetType)
        {
            DoForMarkedMethods<SectionHandlerAttribute, SectionHandler>(targetType, (attr, caller) => _sectionHandlers.Add(attr.SectionName, caller));
            DoForMarkedMethods<ItemHandlerAttribute, ItemHandler>(targetType, (_, caller) => _itemHandler = caller);
            DoForMarkedMethods<MetaHandlerAttribute, MetaHandler>(targetType, (attr, caller) => _metaHandlers[attr.MetaName] = caller);
        }

        private static void DoForMarkedMethods<TAttr, TDelegate>(Type targetType, Action<TAttr, TDelegate> action)
            where TAttr : class
        {
            var results = from mi in targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                                  let attr = mi.GetSingleAttributeOrNull<TAttr>()
                                  where attr != null &&
                                        mi.CompatibleWithGenericCallerDelegate(typeof(TDelegate))
                                  select new
                                  {
                                      attr,
                                      handler = mi.CreateCallDelegate<TDelegate>(targetType)
                                  };

            foreach (var info in results)
            {
                action(info.attr, info.handler);
            }
        }

        public SectionHandler GetSectionHandler(string sectionName)
        {
            SectionHandler result;
            return _sectionHandlers.TryGetValue(sectionName, out result) ? result : null;
        }

        public MetaHandler GetMetaHandler(string metaName)
        {
            MetaHandler result;
            return _metaHandlers.TryGetValue(metaName, out result) ? result : null;
        }

        public ItemHandler GetItemHandler()
        {
            return _itemHandler;
        }
    }
}
