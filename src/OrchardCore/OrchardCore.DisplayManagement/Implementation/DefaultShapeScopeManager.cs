using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using OrchardCore.DisplayManagement.Descriptors;
using OrchardCore.DisplayManagement.Theming;
using Microsoft.AspNetCore.Html;

namespace OrchardCore.DisplayManagement.Implementation
{
    public class DefaultShapeScopeManager : IShapeScopeManager
    {
        private readonly Stack<ShapeScopeContext> _scopes;

        public DefaultShapeScopeManager()
        {
            _scopes = new Stack<ShapeScopeContext>();
        }

        public void EnterScope(ShapeScopeContext context)
        {
            _scopes.Push(context);
        }

        public void ExitScope()
        {
            var childScope = _scopes.Pop();

            // if (_scopes.Count > 0)
            // {
            //     MergeCacheContexts(_scopes.Peek(), childScope);
            // }
        }

        public void AddSlot(string slot, dynamic content)
        {
            if (_scopes.Count > 0)
            {
                _scopes.Peek().AddSlot(slot, content);
            }
        }

        public void AddShape(IShape shape)
        {
            if (_scopes.Count > 0)
            {
                _scopes.Peek().AddShape(shape);
            }
        }

        public dynamic GetSlot(string slot)
        {
            if (_scopes.Count > 0)
            {
                return _scopes.Peek().GetSlot(slot);
            }

            return HtmlString.Empty;
        }

        public IShape GetCurrentShape()
        {

            if (_scopes.Count > 0)
            {
                return _scopes.Peek().GetCurrentShape();
            }

            return null; // I think we have a null shape we could return here if that makes more sense.
        }
    }
}
