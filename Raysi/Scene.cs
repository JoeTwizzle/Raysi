using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Raysi
{
    public class Scene
    {
        public string UIDText { get; }
        public Guid UID { get; }
        public event Action? OnGraphChanged;
        internal readonly Dictionary<Type, List<ComponentData>> Components;
        internal readonly Dictionary<Type, List<GameScript>> Scripts;
        internal readonly HashSet<GameObject> GameObjects;
        public ReadOnlyCollection<GameObject> RootObjects;

        public Scene()
        {
            UID = Guid.NewGuid();
            UIDText = UID.ToString();
            Components = new Dictionary<Type, List<ComponentData>>();
            Scripts = new Dictionary<Type, List<GameScript>>();
            GameObjects = new HashSet<GameObject>();
        }

        #region Add / Remove

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(GameObject gameObject)
        {
            //Assign Context to this GO
            if (gameObject.Scene == null)
            {
                gameObject.Scene = this;
            }
            else
            {
                if (gameObject.Scene != this)
                {
                    throw new Exception("Gameobject already exist in a different GameContext.");
                }
            }

            GameObjects.Add(gameObject);
            for (int i = 0; i < gameObject.Scripts.Count; i++)
            {
                var s = gameObject.Scripts[i];
                bool success = Scripts.TryGetValue(s.GetType(), out var list);
                if (!success || list == null)
                {
                    list = new List<GameScript>();
                }
                list.Add(s);
                Scripts[s.GetType()] = list;
            }
            InvokeGraphChange();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(GameObject GO)
        {
            if (GO.Scene == null)
            {
                throw new Exception("Gameobject does not exist in a GameContext.");
            }
            if (GO.Scene != this)
            {
                throw new Exception("Gameobject does not exist in this GameContext.");
            }

            for (int i = 0; i < GO.Scripts.Count; i++)
            {
                Scripts[GO.Scripts[i].GetType()].Remove(GO.Scripts[i]);
            }
            for (int i = 0; i < GO.Transform.Children.Count; i++)
            {
                GO.Transform.Children[i].Parent = null;
            }

            bool b = GameObjects.Remove(GO);
            GO.Scene = null;
            InvokeGraphChange();
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add<T>(T gameScript) where T : GameScript
        {
            if (gameScript.GameObject.Scene == null)
            {
                throw new Exception("GameScript does not exist in a GameContext.");
            }
            if (gameScript.GameObject.Scene != this)
            {
                throw new Exception("GameScript does not exist in this GameContext.");
            }

            bool success = Scripts.TryGetValue(gameScript.GetType(), out var list);
            if (!success || list == null)
            {
                list = new List<GameScript>();
            }
            list.Add(gameScript);
            Scripts[gameScript.GetType()] = list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove<T>(T gameScript) where T : GameScript
        {
            if (gameScript?.GameObject?.Scene == null)
            {
                throw new Exception("GameScript does not exist in a GameContext.");
            }
            if (gameScript?.GameObject?.Scene != this)
            {
                throw new Exception("GameScript does not exist in this GameContext.");
            }
            Scripts[gameScript.GetType()].Remove(gameScript);
        }

        #endregion

        List<GameObject> rootObjects = new List<GameObject>();
        public void InvokeGraphChange()
        {
            rootObjects.Clear();
            foreach (var go in GameObjects)
            {
                if(go.Transform.Parent == null)
                {
                    rootObjects.Add(go);
                }
            }
            RootObjects = new ReadOnlyCollection<GameObject>(rootObjects);
            OnGraphChanged?.Invoke();
        }

        #region Queries

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<GameScript> GetLikeScripts(Type t)
        {
            Scripts.TryGetValue(t, out var list);
            return list!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<GameScript> GetLikeScripts<T>() where T : GameScript
        {
            Scripts.TryGetValue(typeof(T), out var list);
            return list!;
        }

        #endregion
    }
}
