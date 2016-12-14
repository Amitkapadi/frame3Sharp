﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g3;

namespace f3
{
    public abstract class BaseSO : GameObjectSet, TransformableSceneObject
    {
        protected FScene parentScene;
        protected string uuid;

        SOMaterial sceneMaterial;
        Material displaySceneMaterial;

        Material displayMaterial;
        List<Material> vMaterialStack;

        int _timestamp = 0;
        protected void increment_timestamp() { _timestamp++; }

        public BaseSO()
        {
            uuid = System.Guid.NewGuid().ToString();
            displayMaterial = null;
            vMaterialStack = new List<Material>();
        }


        //
        // SceneObject functions that subclass must implement
        //
        abstract public GameObject RootGameObject { get; }
        abstract public string Name { get; set; }
        abstract public SOType Type { get; }
        abstract public SceneObject Duplicate();


        //
        // SceneObject impl
        //
        virtual public string UUID {
            get { return uuid; }
        }

        virtual public int Timestamp{
            get { return _timestamp; }
        }


        virtual public bool IsTemporary {
            get { return false; }
        }

        virtual public FScene GetScene()
        {
            return parentScene;
        }
        virtual public void SetScene(FScene s)
        {
            parentScene = s;
        }


        virtual protected void set_material_internal(Material m)
        {
            SetAllGOMaterials(m);
        }


        virtual public void AssignSOMaterial(SOMaterial m) {
            sceneMaterial = m;
            displaySceneMaterial = MaterialUtil.ToUnityMaterial(m);
            if (vMaterialStack.Count > 0) {
                // material 0 is always our base material, higher levels of stack are
                // temp materials that will be popped eventually
                vMaterialStack[0] = displaySceneMaterial;
            } else { 
                displayMaterial = displaySceneMaterial;
                set_material_internal(displayMaterial);
            }
            increment_timestamp();
        }
        virtual public SOMaterial GetAssignedSOMaterial() {
            return sceneMaterial;
        }

        virtual public void PushOverrideMaterial(Material m) {
            vMaterialStack.Add(displayMaterial);
            displayMaterial = m;
            set_material_internal(displayMaterial);
            increment_timestamp();
        }

        virtual public void PopOverrideMaterial() {
            if (vMaterialStack.Count > 0) {
                Material m = vMaterialStack.Last();
                vMaterialStack.RemoveAt(vMaterialStack.Count - 1);
                displayMaterial = m;
                set_material_internal(displayMaterial);
                increment_timestamp();
            }
        }

        virtual public Material GetActiveMaterial() {
            return CurrentMaterial;
        }

        virtual public Material CurrentMaterial {
            get { return displayMaterial; }
        }


        public virtual void PreRender() {
            // nothing
        }


        virtual public Bounds GetTransformedBoundingBox() {
            return UnityUtil.GetBoundingBox(RootGameObject);
        }
        virtual public Bounds GetLocalBoundingBox() {
            return GetTransformedBoundingBox();
        }


        virtual public bool FindRayIntersection(Ray ray, out SORayHit hit)
        {
            hit = null;
            GameObjectRayHit hitg = null;
            if (FindGORayIntersection(ray, out hitg)) {
                if (hitg.hitGO != null) {
                    hit = new SORayHit(hitg, this);
                    return true;
                }
            }
            return false;
        }


        //
        // TransformableSceneObject impl
        //
        public event TransformChangedEventHandler OnTransformModified;

        virtual public Frame3f GetLocalFrame(CoordSpace eSpace) {
            return SceneUtil.GetSOLocalFrame(this,eSpace);
        }

        virtual public void SetLocalFrame(Frame3f newFrame, CoordSpace eSpace) {
            SceneUtil.SetSOLocalFrame(this, eSpace, newFrame);
            increment_timestamp();
            if (OnTransformModified != null)
                OnTransformModified(this);
        }


        virtual public bool SupportsScaling {
            get { return true; }
        }
        virtual public Vector3 GetLocalScale()
        {
            return RootGameObject.transform.localScale;
        }
        virtual public void SetLocalScale(Vector3 scale)
        {
            if (SupportsScaling) {
                RootGameObject.transform.localScale = scale;
                increment_timestamp();
                if (OnTransformModified != null)
                    OnTransformModified(this);
            }
        }

    }
}
