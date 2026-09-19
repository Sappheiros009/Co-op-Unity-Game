using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace SlimeCoop.Prototype
{
    /// <summary>Explicit URP offscreen diagnostic, not a native window screenshot or performance measurement.</summary>
    public static class PrototypeFrameCapture
    {
        public static IEnumerator Save(string file, Action<string> complete)
        {
            var camera=Camera.main;
            if(camera==null){complete("Capture camera missing");yield break;}
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>();
            var modes=new RenderMode[canvases.Length];var cameras=new Camera[canvases.Length];var distances=new float[canvases.Length];
            RenderTexture target=null;Texture2D pixels=null;
            try
            {
                for(var i=0;i<canvases.Length;i++)
                {
                    modes[i]=canvases[i].renderMode;cameras[i]=canvases[i].worldCamera;distances[i]=canvases[i].planeDistance;
                    if(modes[i]!=RenderMode.ScreenSpaceOverlay)continue;
                    canvases[i].renderMode=RenderMode.ScreenSpaceCamera;canvases[i].worldCamera=camera;canvases[i].planeDistance=camera.nearClipPlane+.2f;
                }
                Canvas.ForceUpdateCanvases();
                foreach(var label in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>())label.ForceMeshUpdate();
                yield return null;
                target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);target.Create();
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                var previous=RenderTexture.active;
                try
                {
                    RenderTexture.active=target;pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
                    pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
                }
                finally{RenderTexture.active=previous;}
                File.WriteAllBytes(file,pixels.EncodeToPNG());
                var data=pixels.GetPixels32();var min=765;var max=0;
                for(var i=0;i<data.Length;i+=31){var value=data[i].r+data[i].g+data[i].b;min=Math.Min(min,value);max=Math.Max(max,value);}
                complete(max-min<10?"Uniform/blank capture":"");
            }
            finally
            {
                if(pixels!=null)UnityEngine.Object.Destroy(pixels);
                if(target!=null){target.Release();UnityEngine.Object.Destroy(target);}
                for(var i=0;i<canvases.Length;i++)if(canvases[i]!=null)
                {canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];}
            }
        }
    }
}
