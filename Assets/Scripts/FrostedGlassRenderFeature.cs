using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FrostedGlassRenderFeature : ScriptableRendererFeature
{
    class FrostedGlassRenderPass : ScriptableRenderPass
    {
        // 定义材质和其他渲染相关参数
        private static readonly string k_RenderTag = "Render Custom Layer Objects";
        private Material frostedGlassMaterial;
        private int overrideMaterialPassIndex = 0;
        private string profilerTag = "FrostedGlassRenderPass";
        private RenderTargetIdentifier source;
        private RTHandle temporaryRT;
        private FilteringSettings filteringSettings;

        public FrostedGlassRenderPass(Material material)
        {
            frostedGlassMaterial = material;
            // 设置这个渲染通道在渲染流程中的执行时机
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            temporaryRT = RTHandles.Alloc(camera.pixelWidth, camera.pixelHeight, name: "TemporaryRT");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // 创建DrawingSettings
            var drawingSettings = new DrawingSettings(new ShaderTagId("UniversalForward"), new SortingSettings(renderingData.cameraData.camera))
            {
                overrideMaterial = this.frostedGlassMaterial,
                overrideMaterialPassIndex = this.overrideMaterialPassIndex
            };
            
            // 从RenderingData获取Camera的CullingResults
            var cullingResults = renderingData.cullResults;

            // 执行实际的绘制调用
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
            
            // CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            //
            // // 使用材质执行自定义后处理效果
            // cmd.Blit(renderingData.cameraData.renderer.cameraColorTarget, temporaryRT, frostedGlassMaterial);
            // cmd.Blit(temporaryRT, renderingData.cameraData.renderer.cameraColorTarget);
            //
            // context.ExecuteCommandBuffer(cmd);
            // cmd.Clear();
            //
            // CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (temporaryRT != null)
            {
                temporaryRT.Release();
                temporaryRT = null;
            }
        }
    }

    // 在这里指定你的ShaderGraph生成的材质
    public Material frostedGlassMaterial;
    FrostedGlassRenderPass _mScriptablePass;

    public override void Create()
    {
        _mScriptablePass = new FrostedGlassRenderPass(frostedGlassMaterial);

        // 设置渲染通道的执行时机
        // 例如，在后处理之前执行
        _mScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_mScriptablePass);
    }
}
