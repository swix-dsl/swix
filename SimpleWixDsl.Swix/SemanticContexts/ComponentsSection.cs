﻿using System.Collections.Generic;
using SimpleWixDsl.Ahl;

namespace SimpleWixDsl.Swix
{
    public class ComponentsSection : BaseSwixSemanticContext
    {
        private readonly List<WixComponent> _components;
        private readonly List<WixComponent> _toAdd;

        public ComponentsSection(IAttributeContext attributeContext, List<WixComponent> components) 
            : base(attributeContext)
        {
            _components = components;
            _toAdd = new List<WixComponent>();
        }

        [ItemHandler]
        public ISemanticContext Component(string key, IAttributeContext itemContext)
        {
            return new StubSwixElement(CurrentAttributeContext, () => _toAdd.Add(WixComponent.FromContext(key, itemContext)));
        }

        protected override void FinishItemCore()
        {
            _components.AddRange(_toAdd);
        }
    }
}