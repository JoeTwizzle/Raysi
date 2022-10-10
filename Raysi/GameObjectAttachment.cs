
using System;
using System.Collections.Generic;
using System.Text;

namespace Raysi
{
    public abstract class GameObjectAttachment : NamedObject
    {
        public GameObject GameObject { get; internal set; }
    }
}
