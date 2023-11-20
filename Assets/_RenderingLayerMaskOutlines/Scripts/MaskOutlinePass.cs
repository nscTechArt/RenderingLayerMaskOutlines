using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MaskPass : ScriptableRenderPass
{

    private FilteringSettings filteringSettings;
    private readonly Material maskMaterial;

    private readonly List<ShaderTagId> shaderTagIds = new List<ShaderTagId>()
    {
        new ShaderTagId("SRPDefaultUnlit"),
        new ShaderTagId("UniversalForward"),
        new ShaderTagId("UniversalForwardOnly")
    };
    
    private RenderTargetHandle maskRT;

    public MaskPass(MaskOutlineFeatureSettings featureSettings)
    {
        renderPassEvent = featureSettings.renderPassEvent;
        uint renderingLayerMask = (uint) 1 << featureSettings.renderingLayerMask - 1;
        filteringSettings = new FilteringSettings(
            RenderQueueRange.all, featureSettings.layerMask, renderingLayerMask);
        maskMaterial = featureSettings.maskPassMaterial;
        
        maskRT.Init("_MaskRT");
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        RenderTextureDescriptor descriptor = cameraTextureDescriptor;
        descriptor.colorFormat = RenderTextureFormat.Default;
        descriptor.depthBufferBits = 0;
        cmd.GetTemporaryRT(maskRT.id, descriptor, FilterMode.Bilinear);
        
        ConfigureTarget(maskRT.Identifier());
        
        ConfigureClear(ClearFlag.Color, Color.black);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, new ProfilingSampler("Mask Render Pass")))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
            var drawingSettings = CreateDrawingSettings(shaderTagIds, ref renderingData, SortingCriteria.None);
            drawingSettings.overrideMaterial = maskMaterial;
            
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

            cmd.SetGlobalTexture("_MaskGlobalRT", maskRT.Identifier());
        }
        
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(maskRT.id);
    }
}

public class OutlinePass : ScriptableRenderPass
{
    private FilteringSettings filteringSettings;
    private readonly Material outlineMaterial;

    private RenderTargetIdentifier cameraColorRT, tempRT;
    private readonly int tempRTId = Shader.PropertyToID("_TempRT");
    
    public OutlinePass(MaskOutlineFeatureSettings featureSettings)
    {
        renderPassEvent = featureSettings.renderPassEvent;
        outlineMaterial = featureSettings.outlinePassMaterial;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        cmd.GetTemporaryRT(tempRTId, descriptor, FilterMode.Bilinear);
        tempRT = new RenderTargetIdentifier(tempRTId);

        cameraColorRT = renderingData.cameraData.renderer.cameraColorTarget;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();

        using (new ProfilingScope(cmd, new ProfilingSampler("Outline Pass")))
        {
            Blit(cmd, cameraColorRT, tempRT);
            Blit(cmd, tempRT, cameraColorRT, outlineMaterial);
        }
        
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(tempRTId);
    }
}