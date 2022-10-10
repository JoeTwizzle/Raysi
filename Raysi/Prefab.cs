using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raysi
{
    public class Prefab
    {
        public Dictionary<GameObject, List<GameScript>> GO_Scripts { get; private set; }
        public GameObject Root { get; private set; }
        Prefab()
        {
            GO_Scripts = new Dictionary<GameObject, List<GameScript>>();
        }

        public Prefab(Dictionary<GameObject, List<GameScript>> go_scripts, GameObject root)
        {
            Root = root;
            GO_Scripts = go_scripts;
        }

        public static Prefab Create(GameObject root)
        {
            Prefab prefab = new Prefab
            {
                Root = root
            };
            TraverseNodes(prefab, root.Transform);
            return prefab;
        }

        static void TraverseNodes(Prefab prefab, Transform transform)
        {
            prefab.GO_Scripts[transform.GameObject] = transform.GameObject.Scripts;
            for (int i = 0; i < transform.Children.Count; i++)
            {
                TraverseNodes(prefab, transform.Children[i]);
            }
        }
    }
}
