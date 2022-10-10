using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raysi
{
    public abstract class NamedObject
    {
        public string UIDText { get; internal set; }
        public Guid UID { get; internal set; }
        public NamedObject()
        {
            UID = Guid.NewGuid();
            UIDText = UID.ToString();
        }
    }
}
