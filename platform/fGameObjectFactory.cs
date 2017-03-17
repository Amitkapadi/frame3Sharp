﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;


namespace f3
{
    public static partial class GameObjectFactory
    {
        public static CurveRendererFactory CurveRendererSource = new UnityCurveRendererFactory();





        public static fGameObject CreateParentGO(string sName)
        {
            GameObject go = new GameObject(sName);
            return new fGameObject(go);
        }


        static void initialize_meshgo(GameObject go, fMesh mesh, bool bCollider, bool bShared)
        {
            go.AddComponent<MeshFilter>();
            if (mesh != null) {
                if ( bShared )
                    go.SetSharedMesh(mesh);
                else
                    go.SetMesh(mesh);
            }
            go.AddComponent<MeshRenderer>();
            if (bCollider) {
                var collider = go.AddComponent<MeshCollider>();
                collider.enabled = false;
            }
        }


        public static fMeshGameObject CreateMeshGO(string sName, Mesh mesh = null, bool bCollider = false, bool bShared = false)
        {
            GameObject go = new GameObject(sName);
            initialize_meshgo(go, new fMesh(mesh), bCollider, bShared);
            return new fMeshGameObject(go, new fMesh(go.GetSharedMesh()) );
        }
        public static fMeshGameObject CreateMeshGO(string sName, fMesh mesh, bool bCollider = false, bool bShared = false)
        {
            GameObject go = new GameObject(sName);
            initialize_meshgo(go, mesh, bCollider, bShared);
            return new fMeshGameObject(go, new fMesh(go.GetSharedMesh()) );
        }

        // unit rectangle lying in plane
        public static fRectangleGameObject CreateRectangleGO(string sName, float fWidth, float fHeight, Colorf color, bool bCollider)
        {
            fMaterial mat = MaterialUtil.CreateFlatMaterialF(color);
            return CreateRectangleGO(sName, fWidth, fHeight, mat, false, bCollider);
        }
        public static fRectangleGameObject CreateRectangleGO(string sName, float fWidth, float fHeight, fMaterial useMaterial, bool bShareMaterial, bool bCollider)
        {
            GameObject go = new GameObject(sName);
            Mesh rectMesh = UnityUtil.GetPrimitiveMesh(PrimitiveType.Quad);
            UnityUtil.RotateMesh(rectMesh, Quaternionf.AxisAngleD(Vector3f.AxisX, 90), Vector3f.Zero);
            initialize_meshgo(go, new fMesh(rectMesh), bCollider, true);
            go.SetMaterial(useMaterial, bShareMaterial);
            return new fRectangleGameObject(go, fWidth, fHeight);
        }

        // equilateral triangle lying in plane centered at (0,0) 
        // with height = 1 (ie vertical extent is [-0.5,0.5], so base width is 2/sqrt(3)
        public static fTriangleGameObject CreateTriangleGO(string sName, float fWidth, float fHeight, Colorf color, bool bCollider)
        {
            GameObject go = new GameObject(sName);
            Mesh triMesh = new Mesh();
            float h = 1;
            float w = (float)(2 / Math.Sqrt(3));
            //float h = (float)(Math.Sqrt(3) / 2);      // width=1 instead (expose this as parameter?)
            //float w = 1;
            triMesh.vertices = new Vector3[3] {
                new Vector3(-w/2, 0.0f, -h/2), new Vector3(w/2, 0.0f, -h/2), new Vector3(0, 0, h/2) };
            triMesh.triangles = new int[3] { 0, 2, 1 };
            initialize_meshgo(go, new fMesh(triMesh), bCollider, true);
            go.SetMaterial(MaterialUtil.CreateFlatMaterialF(color));
            return new fTriangleGameObject(go, fWidth, fHeight);
        }


        // disc with radius=1, lying in plane, centered at (0,0)
        public static fDiscGameObject CreateDiscGO(string sName, float fRadius, Colorf color, bool bCollider)
        {
            return CreateDiscGO(sName, fRadius, MaterialUtil.CreateFlatMaterialF(color), false, bCollider);
        }
        public static fDiscGameObject CreateDiscGO(string sName, float fRadius, fMaterial material, bool bShareMaterial, bool bCollider)
        {
            GameObject go = new GameObject(sName);
            Mesh discMesh = PrimitiveCache.GetPrimitiveMesh(fPrimitiveType.Disc);
            initialize_meshgo(go, new fMesh(discMesh), bCollider, true);
            go.SetMaterial(material, bShareMaterial);
            return new fDiscGameObject(go, new fMesh(go.GetSharedMesh()),  fRadius);
        }


        public static fLineGameObject CreateLineGO(string sName, Colorf color, float fLineWidth)
        {
            GameObject go = new GameObject(sName);
            CurveRendererImplementation curveRen = CurveRendererSource.Build();
            curveRen.initialize(go, new Colorf(Colorf.Black, 0.75f) );
            fLineGameObject lgo = new fLineGameObject(go, curveRen);
            lgo.SetColor(color);
            lgo.SetLineWidth(fLineWidth);
            return lgo;
        }



        public static fCircleGameObject CreateCircleGO(string sName, float fRadius, Colorf color, float fLineWidth)
        {
            GameObject go = new GameObject(sName);
            CurveRendererImplementation curveRen = CurveRendererSource.Build();
            curveRen.initialize(go, new Colorf(Colorf.Black, 0.75f) );
            fCircleGameObject fgo = new fCircleGameObject(go, curveRen);
            fgo.SetColor(color);
            fgo.SetLineWidth(fLineWidth);
            fgo.SetSteps(32);
            fgo.SetRadius(fRadius);
            return fgo;
        }



        public static fPolylineGameObject CreatePolylineGO(string sName, List<Vector3f> vVertices, Colorf color, float fLineWidth)
        {
            GameObject go = new GameObject(sName);
            CurveRendererImplementation curveRen = CurveRendererSource.Build();
            curveRen.initialize(go, new Colorf(Colorf.Black, 0.75f) );
            fPolylineGameObject fgo = new fPolylineGameObject(go, curveRen);
            fgo.SetColor(color);
            fgo.SetLineWidth(fLineWidth);
            fgo.SetVertices(vVertices);
            return fgo;
        }


        public static fTextGameObject CreateTextMeshGO(
            string sName, string sText, 
            Colorf textColor, float fTextHeight, 
            BoxPosition textOrigin = BoxPosition.Center, 
            float fOffsetZ = -0.1f )
        {
            return TextMeshProUtil.HaveTextMeshPro ?
                  TextMeshProUtil.CreateTextMeshProGO(sName, sText, textColor, fTextHeight, textOrigin, fOffsetZ)
                : CreateUnityTextMeshGO(sName, sText, textColor, fTextHeight, textOrigin, fOffsetZ);
        }


        public static fTextGameObject CreateUnityTextMeshGO(
            string sName, string sText, 
            Colorf textColor, float fTextHeight, 
            BoxPosition textOrigin = BoxPosition.Center, 
            float fOffsetZ = -0.1f)
        {
            GameObject textGO = new GameObject(sName);
            TextMesh tm = textGO.AddComponent<TextMesh>();
            tm.text = sText;
            tm.color = textColor;
            tm.fontSize = 50;
            tm.offsetZ = fOffsetZ;
            tm.alignment = TextAlignment.Left;
            // ignore material changes when we add to GameObjectSet
            textGO.AddComponent<IgnoreMaterialChanges>();
            // use our textmesh material instead
            MaterialUtil.SetTextMeshDefaultMaterial(tm);

            Vector2f size = UnityUtil.EstimateTextMeshDimensions(tm);
            float fScaleH = fTextHeight / size.y;
            tm.transform.localScale = new Vector3(fScaleH, fScaleH, fScaleH);
            float fTextWidth = fScaleH * size.x;

            // by default text origin is top-left
            if ( textOrigin == BoxPosition.Center )
                tm.transform.Translate(-fTextWidth / 2.0f, fTextHeight / 2.0f, 0);
            else if ( textOrigin == BoxPosition.BottomLeft )
                tm.transform.Translate(0, fTextHeight, 0);
            else if ( textOrigin == BoxPosition.TopRight )
                tm.transform.Translate(-fTextWidth, 0, 0);
            else if ( textOrigin == BoxPosition.BottomRight )
                tm.transform.Translate(-fTextWidth, fTextHeight, 0);
            else if ( textOrigin == BoxPosition.CenterLeft )
                tm.transform.Translate(0, fTextHeight/2.0f, 0);
            else if ( textOrigin == BoxPosition.CenterRight )
                tm.transform.Translate(-fTextWidth, fTextHeight/2.0f, 0);
            else if ( textOrigin == BoxPosition.CenterTop )
                tm.transform.Translate(-fTextWidth / 2.0f, 0, 0);
            else if ( textOrigin == BoxPosition.CenterBottom )
                tm.transform.Translate(-fTextWidth / 2.0f, fTextHeight, 0);

            textGO.GetComponent<Renderer>().material.renderQueue = SceneGraphConfig.TextRendererQueue;

            return new fTextGameObject(textGO, new fText(tm, TextType.UnityTextMesh),
                new Vector2f(fTextWidth, fTextHeight) );
        }





        public static fGameObject Duplicate(fGameObject go)
        {
            GameObject copy = GameObject.Instantiate<GameObject>(go);
            fGameObject fcopy = new fGameObject(copy);

            // have to set parent fgo in PreRenderBehavior script...
            if (copy.GetComponent<PreRenderBehavior>() != null)
                copy.GetComponent<PreRenderBehavior>().ParentFGO = fcopy;

            return fcopy;            
        }


        public static void DestroyGO(fGameObject go) {
            go.SetParent(null);
            UnityEngine.GameObject.Destroy(go);
        }

    }

}
