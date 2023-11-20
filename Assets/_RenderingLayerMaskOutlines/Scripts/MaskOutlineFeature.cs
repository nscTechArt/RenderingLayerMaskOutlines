using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[Serializable]
public class MaskOutlineFeatureSettings
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    public LayerMask layerMask;
    [Range(0, 32)] public int renderingLayerMask;
    public Material maskPassMaterial, outlinePassMaterial;
}

public class MaskOutlineFeature : ScriptableRendererFeature
{
    public MaskOutlineFeatureSettings settings;

    private MaskPass maskPass;
    private OutlinePass outlinePass;
    
    public override void Create()
    {
        maskPass = new MaskPass(settings);
        outlinePass = new OutlinePass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(maskPass);
        renderer.EnqueuePass(outlinePass);
    }


}