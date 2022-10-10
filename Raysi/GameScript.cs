
using System;
using System.Collections.Generic;
using System.Text;

namespace Raysi
{
    public class GameScript : GameObjectAttachment
    {

        private bool enabled = false;

        public bool IsActiveAndEnabled { get; internal set; }

        internal void CheckEnabled(bool current, bool past)
        {
            if (current && !past) { OnEnable(); }
            else if (!current && past) { OnDisable(); }
        }

        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;

                if (GameObject != null)
                {
                    bool activeAndEnabled = GameObject.IsActive && value;
                    CheckEnabled(activeAndEnabled, IsActiveAndEnabled);
                    IsActiveAndEnabled = activeAndEnabled;
                }
                else
                {
                    bool activeAndEnabled = value;
                    CheckEnabled(activeAndEnabled, IsActiveAndEnabled);
                    IsActiveAndEnabled = activeAndEnabled;
                }
            }
        }

        protected T? GetComponent<T>() where T : GameScript { return GameObject?.GetScript<T>(); }

        public virtual void OnEnable() { }
        public virtual void OnDisable() { }

        public virtual void Init() { }
        public virtual void Start() { }
        public virtual void Update() { }
        public virtual void PostUpdate() { }
        public virtual void Destroy() { }

        public virtual void Draw() { }
        public virtual void PostDraw() { }
    }
}
