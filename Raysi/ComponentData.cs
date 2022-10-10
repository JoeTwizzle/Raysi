using System;
using System.Collections.Generic;
using System.Text;

namespace Raysi
{
    public abstract class ComponentData : GameObjectAttachment
    {
        protected T? GetComponent<T>() where T : GameScript { return GameObject?.GetScript<T>(); }
    }
}
