using OpenTK.Windowing.Desktop;

using System;
using System.Collections.Generic;
using System.Text;

namespace Raysi
{
    public class GameLoop
    {
        //TODO: Multiple scene support
        public readonly Scene MasterScene;

        public float DeltaTime;
        public float RenderDeltaTime;
        public readonly NativeWindow NativeWindow;

        public GameLoop(NativeWindow nativeWindow)
        {
            NativeWindow = nativeWindow;
            MasterScene = new Scene();
        }

        internal void Add<T>(T gameScript) where T : GameScript
        {
            MasterScene.Add(gameScript);
            gameScript.Init();
            gameScript.Enabled = true;
            gameScript.Start();
        }

        internal void Remove<T>(T gameScript) where T : GameScript
        {
            gameScript.Enabled = false;
            gameScript.Destroy();
            MasterScene.Remove(gameScript);
        }

        public void Add(GameObject GO)
        {
            GO.GameLoop = this;
            GO.Scene = MasterScene;
            MasterScene.Add(GO);
            GO.IsActive = true;
            for (int i = 0; i < GO.Scripts.Count; i++)
            {
                GO.Scripts[i].Init();
                GO.Scripts[i].Enabled = true;
                GO.Scripts[i].Start();
            }
            GO.Initialized = true;
        }

        public void Remove(GameObject GO)
        {
            for (int i = 0; i < GO.Scripts.Count; i++)
            {
                GO.Scripts[i].Enabled = false;
                GO.Scripts[i].Destroy();
            }
            GO.IsActive = false;
            GO.GameLoop = null!;
            GO.Scene = null!;
            GO.Initialized = false;
            MasterScene.Remove(GO);
        }

        public void Instantiate(Prefab prefab, Transform? parent)
        {
            prefab.Root.Transform.Parent = parent;
            foreach (var GO_Script in prefab.GO_Scripts)
            {
                Add(GO_Script.Key);
            }
        }


        //void ProcessAddedScripts()
        //{
        //    for (int i = 0; i < AddedScripts.Count; i++)
        //    {
        //        MasterScene.Add(AddedScripts[i]);
        //    }
        //    for (int i = 0; i < AddedScripts.Count; i++)
        //    {
        //        AddedScripts[i].Init();
        //    }
        //    for (int i = 0; i < AddedScripts.Count; i++)
        //    {
        //        AddedScripts[i].Enabled = true;
        //    }
        //    for (int i = 0; i < AddedScripts.Count; i++)
        //    {
        //        AddedScripts[i].Start();
        //    }
        //    AddedScripts.Clear();
        //}

        //void ProcessRemovedScripts()
        //{
        //    for (int i = 0; i < RemovedScripts.Count; i++)
        //    {
        //        RemovedScripts[i].Enabled = false;
        //    }
        //    for (int i = 0; i < RemovedScripts.Count; i++)
        //    {
        //        RemovedScripts[i].Destroy();
        //    }
        //    for (int i = 0; i < RemovedScripts.Count; i++)
        //    {
        //        MasterScene.Remove(RemovedScripts[i]);
        //        RemovedScripts[i].GameObject = null!;
        //    }
        //    RemovedScripts.Clear();
        //}

        //void ProcessAddedGOs()
        //{
        //    for (int i = 0; i < AddedGOs.Count; i++)
        //    {
        //        MasterScene.Add(AddedGOs[i]);
        //        AddedGOs[i].IsActive = true;
        //        var go = AddedGOs[i];
                
        //        for (int j = 0; j < go.Scripts.Count; j++)
        //        {
        //            go.Scripts[j].Init();
        //        }
        //        for (int j = 0; j < go.Scripts.Count; j++)
        //        {
        //            go.Scripts[j].Enabled = true;
        //        }
        //        for (int j = 0; j < go.Scripts.Count; j++)
        //        {
        //            go.Scripts[j].Start();
        //        }
        //        AddedGOs[i].Initialized = true;
        //    }
        //    AddedGOs.Clear();
        //}

        //void ProcessRemovedGOs()
        //{
        //    for (int i = 0; i < RemovedGOs.Count; i++)
        //    {
        //        for (int j = 0; j < RemovedGOs[i].Scripts.Count; j++)
        //        {
        //            RemovedGOs[i].Scripts[j].Enabled = false;
        //            RemovedGOs[i].Scripts[j].Destroy();
        //        }
        //        MasterScene.Remove(RemovedGOs[i]);
        //        RemovedGOs[i].GameLoop = null!;
        //        RemovedGOs[i].Scene = null;
        //    }
        //    RemovedGOs.Clear();
        //}

        public void Update(float dt)
        {
            DeltaTime = dt;

            ProcessUpdates();
        }

        void ProcessUpdates()
        {
            foreach (var scriptList in MasterScene.Scripts)
            {
                for (int i = 0; i < scriptList.Value.Count; i++)
                {
                    if (scriptList.Value[i].IsActiveAndEnabled)
                    {
                        scriptList.Value[i].Update();
                    }
                }
            }

            foreach (var scriptList in MasterScene.Scripts)
            {
                for (int i = 0; i < scriptList.Value.Count; i++)
                {
                    if (scriptList.Value[i].IsActiveAndEnabled)
                    {
                        scriptList.Value[i].PostUpdate();
                    }
                }
            }
        }

        #region Draws
        void ProcessDraws()
        {
            foreach (var scriptList in MasterScene.Scripts)
            {
                for (int i = 0; i < scriptList.Value.Count; i++)
                {
                    if (scriptList.Value[i].IsActiveAndEnabled)
                    {
                        scriptList.Value[i].Draw();
                    }
                }
            }
        }

        //void ProcessDrawAddedScripts()
        //{
        //    for (int i = 0; i < DrawAddedScripts.Count; i++)
        //    {
        //        DrawAddedScripts[i].DrawInit();
        //    }
        //    for (int i = 0; i < DrawAddedScripts.Count; i++)
        //    {
        //        DrawAddedScripts[i].DrawStart();
        //    }
        //    DrawAddedScripts.Clear();
        //}

        //void ProcessDrawRemovedScripts()
        //{
        //    for (int i = 0; i < DrawRemovedScripts.Count; i++)
        //    {
        //        DrawRemovedScripts[i].DrawDestroy();
        //    }
        //    DrawRemovedScripts.Clear();
        //}

        //void ProcessDrawAddedGOs()
        //{
        //    for (int i = 0; i < DrawAddedGOs.Count; i++)
        //    {
        //        var go = DrawAddedGOs[i];
        //        for (int j = 0; j < go.Scripts.Count; j++)
        //        {
        //            go.Scripts[j].DrawInit();
        //        }
        //        for (int j = 0; j < go.Scripts.Count; j++)
        //        {
        //            go.Scripts[j].DrawStart();
        //        }
        //    }
        //    DrawAddedGOs.Clear();
        //}

        //void ProcessDrawRemovedGOs()
        //{
        //    for (int i = 0; i < DrawRemovedGOs.Count; i++)
        //    {
        //        for (int j = 0; j < DrawRemovedGOs[i].Scripts.Count; j++)
        //        {
        //            DrawRemovedGOs[i].Scripts[j].DrawDestroy();
        //        }
        //    }
        //    DrawRemovedGOs.Clear();
        //}

        public void Draw(float dt)
        {
            RenderDeltaTime = dt;
            ProcessDraws();
        }

        public void PostDraw()
        {
            foreach (var scriptList in MasterScene.Scripts)
            {
                for (int i = 0; i < scriptList.Value.Count; i++)
                {
                    if (scriptList.Value[i].IsActiveAndEnabled)
                    {
                        scriptList.Value[i].PostDraw();
                    }
                }
            }
        }
        #endregion

        //#region Physics
        //void ProcessPhysics()
        //{
        //    foreach (var scriptList in MasterScene.Scripts)
        //    {
        //        for (int i = 0; i < scriptList.Value.Count; i++)
        //        {
        //            if (scriptList.Value[i].IsActiveAndEnabled)
        //            {
        //                scriptList.Value[i].PhysicsUpdate();
        //            }
        //        }
        //    }
        //}

        //void ProcessPhysicsAddedScripts()
        //{
        //    for (int i = 0; i < PhysicsAddedScripts.Count; i++)
        //    {
        //        PhysicsAddedScripts[i].PhysicsInit();
        //    }
        //    for (int i = 0; i < PhysicsAddedScripts.Count; i++)
        //    {
        //        PhysicsAddedScripts[i].PhysicsStart();
        //    }
        //    PhysicsAddedScripts.Clear();
        //}

        //void ProcessPhysicsRemovedScripts()
        //{
        //    for (int i = 0; i < PhysicsRemovedScripts.Count; i++)
        //    {
        //        PhysicsRemovedScripts[i].PhysicsDestroy();
        //    }
        //    PhysicsRemovedScripts.Clear();
        //}

        //void ProcessPhysicsAddedGOs()
        //{
        //    for (int i = 0; i < PhysicsAddedGOs.Count; i++)
        //    {
        //        var go = PhysicsAddedGOs[i];
        //        for (int j = 0; j < go.Scripts.Count; j++)
        //        {
        //            go.Scripts[j].PhysicsInit();
        //        }
        //        for (int j = 0; j < go.Scripts.Count; j++)
        //        {
        //            go.Scripts[j].PhysicsStart();
        //        }
        //    }
        //    PhysicsAddedGOs.Clear();
        //}

        //void ProcessPhysicsRemovedGOs()
        //{
        //    for (int i = 0; i < PhysicsRemovedGOs.Count; i++)
        //    {
        //        for (int j = 0; j < PhysicsRemovedGOs[i].Scripts.Count; j++)
        //        {
        //            PhysicsRemovedScripts[i].PhysicsDestroy();
        //        }
        //    }
        //    PhysicsRemovedGOs.Clear();
        //}

        //public void Physics()
        //{
        //    ProcessPhysicsAddedGOs();
        //    ProcessPhysicsAddedScripts();

        //    ProcessPhysics();

        //    ProcessPhysicsRemovedScripts();
        //    ProcessPhysicsRemovedGOs();
        //}

        //#endregion

    }
}
