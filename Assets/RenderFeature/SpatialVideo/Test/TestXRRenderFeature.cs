using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TestXRRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader shader;
    [SerializeField] private GameObject renderObject;
    [SerializeField] private Material testMaterial;
    [SerializeField] private Texture testTexture;
    
    private Material meshMaterial;
    
    class CustomRenderPass : ScriptableRenderPass
    {
        public MeshFilter _meshFilter;
        public Material _meshMaterial;
        public Material _testMaterial;

        public Texture _testTex;
        
        private RenderTargetIdentifier _currentTarget;
        private RenderTargetIdentifier _currentDepth;
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            _currentTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            _currentDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_testMaterial == null)
            {
                return;
            }
            
            var cb = CommandBufferPool.Get("TestMultiView");
            
            // blur pass
            // 输出临时RT
            int tempId = Shader.PropertyToID("_OutputRT");
            cb.GetTemporaryRT(tempId, _testTex.width /2, _testTex.height,
                0, FilterMode.Bilinear, GraphicsFormat.R8G8B8A8_UNorm);
            cb.SetRenderTarget(tempId);
            // 输入贴图
            cb.SetGlobalTexture("_TestTex", _testTex);
            cb.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _testMaterial);
            
            // // 渲染到屏幕
            // // 设置深度测试和写入状态
            // cb.SetRenderTarget(_currentTarget, _currentDepth);
            // cb.SetRenderTarget(_currentTarget);
            
            // cb.SetGlobalTexture("_TestTex", _testTex);
            // cb.DrawMesh(_meshFilter.sharedMesh, _meshFilter.transform.localToWorldMatrix, _meshMaterial);
            
            // cb.ReleaseTemporaryRT(tempId);
            context.ExecuteCommandBuffer(cb);
            cb.Clear();
            CommandBufferPool.Release(cb);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        m_ScriptablePass._meshFilter = renderObject.GetComponent<MeshFilter>();
        
        meshMaterial = CoreUtils.CreateEngineMaterial(shader);

        m_ScriptablePass._testMaterial = testMaterial;
        m_ScriptablePass._meshMaterial = meshMaterial;
        m_ScriptablePass._testTex = testTexture;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        CoreUtils.Destroy(meshMaterial);
    }
}


