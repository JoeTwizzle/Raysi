
using System;
using System.Collections.Generic;
using System.Linq;

namespace Raysi
{
    public sealed class GameObject
    {
        public string UIDText { get; internal set; }
        public Guid UID { get; internal set; }
        public List<ComponentData> ComponentData { get; } = new List<ComponentData>();
        public List<GameScript> Scripts { get; } = new List<GameScript>();
        public Transform Transform { get; }
        public Scene? Scene { get; internal set; }
        public GameLoop GameLoop { get; internal set; }
        public string Name { get; set; } = "New GameObject";
        public bool Initialized { get; internal set; }


        private bool isActive = false;

        public bool IsActive
        {
            get { return isActive; }
            set
            {
                //Toggled on
                if (value && !isActive)
                {
                    //Set IsActiveAndEnabled and Call OnEnable/Disable
                    for (int i = 0; i < Scripts.Count; i++)
                    {
                        bool last = Scripts[i].IsActiveAndEnabled;
                        Scripts[i].IsActiveAndEnabled = value && Scripts[i].Enabled;
                        Scripts[i].CheckEnabled(Scripts[i].IsActiveAndEnabled, last);
                    }
                    //Propagate down the tree
                    for (int i = 0; i < Transform.Children.Count; i++)
                    {
                        Transform.Children[i].GameObject.IsActive = true;
                    }
                }
                //Toggled off
                else if (!value && isActive)
                {
                    //Set IsActiveAndEnabled and Call OnEnable/Disable
                    for (int i = 0; i < Scripts.Count; i++)
                    {
                        bool last = Scripts[i].IsActiveAndEnabled;
                        Scripts[i].IsActiveAndEnabled = value && Scripts[i].Enabled;
                        Scripts[i].CheckEnabled(Scripts[i].IsActiveAndEnabled, last);
                    }
                    for (int i = 0; i < Transform.Children.Count; i++)
                    {
                        Transform.Children[i].GameObject.IsActive = false;
                    }
                }
                isActive = value;
            }
        }

        public GameObject(string name = "New GameObject")
        {
            Name = name;
            UID = Guid.NewGuid();
            UIDText = UID.ToString();
            Transform = Transform.Create();
            AddComponent(Transform);
        }

        public void AddComponent<T>(T data) where T : ComponentData
        {
            data.GameObject = this;
            ComponentData.Add(data);
        }

        public void AddScript<T>(T script) where T : GameScript
        {
            script.GameObject = this;
            Scripts.Add(script);
            if (GameLoop != null && Initialized)
            {
                GameLoop.Add(script);
            }
        }

        public void RemoveScript<T>(T script) where T : GameScript
        {

            if (script.GameObject == this)
            {
                Scripts.Remove(script);
                if (GameLoop != null)
                {
                    GameLoop.Remove(script);
                }
            }
            else
            {
                throw new InvalidOperationException("The script does not belong to the current GameObject.");
            }
        }

        public void RemoveComponent<T>(T data) where T : ComponentData
        {
            data.GameObject = null!; //xDDDD
            ComponentData.Remove(data);
        }

        public T? GetScript<T>() where T : GameScript
        {
            return (T?)Scripts.FirstOrDefault(x => x.GetType() == typeof(T));
        }

        public T? GetComponent<T>() where T : ComponentData
        {
            return (T?)ComponentData.FirstOrDefault(x => x.GetType() == typeof(T));
        }
    }
}
