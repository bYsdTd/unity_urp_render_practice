using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomRenderPassFeature : ScriptableRendererFeature
{
    // 单例实例
    public static CustomRenderPassFeature Instance { get; private set; }
    class CustomRenderPass : ScriptableRenderPass
    {
        public Material material;
        private RenderTargetIdentifier currentTarget;

        // 新增字段用于Mesh对象和其Transform
        private Mesh targetMesh;
        private Matrix4x4 targetMeshTransform;
        
        // 修改构造函数以接收Mesh对象和其Transform
        public CustomRenderPass(Material material, Mesh mesh, Matrix4x4 transform)
        {
            this.material = material;
            this.targetMesh = mesh;
            this.targetMeshTransform = transform;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }
        
        public void UpdateTransform(Matrix4x4 newMatrix)
        {
            targetMeshTransform = newMatrix;
        }
        
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            currentTarget = renderingData.cameraData.renderer.cameraColorTarget;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Render Mesh With Custom Effect");

            // 使用传入的Mesh对象和Transform信息绘制Mesh
            if (targetMesh != null && material != null)
            {
                cmd.DrawMesh(targetMesh, targetMeshTransform, material);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // 清理资源（如果需要）
        }
    }

    public Material quadMaterial; // 使用的材质
    public Mesh targetMesh; // 场景中目标Mesh对象
    public Transform targetMeshTransform; // 目标Mesh的Transform
    private CustomRenderPass m_ScriptablePass;
    
    /// <inheritdoc/>
    public override void Create()
    {
        Instance = this;
        if (targetMeshTransform == null)
        {
            return;
        }
        // 创建CustomRenderPass时传入Mesh对象和Transform
        m_ScriptablePass = new CustomRenderPass(quadMaterial, targetMesh, targetMeshTransform.localToWorldMatrix);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (quadMaterial == null || targetMesh == null)
            return;

        m_ScriptablePass.material = quadMaterial;
        renderer.EnqueuePass(m_ScriptablePass);
    }
    
    // 提供一个方法来更新Transform
    public void UpdateMeshTransform(Transform transform)
    {
        if (m_ScriptablePass != null)
        {
            m_ScriptablePass.UpdateTransform(transform.localToWorldMatrix);
        }
    }
}


