using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class CustomRenderPassFeature : ScriptableRendererFeature
{
    // 单例实例
    public static CustomRenderPassFeature Instance { get; private set; }
    class CustomRenderPass : ScriptableRenderPass
    {
        
        private RenderTargetIdentifier tempRT; // 临时RT标识符
        private int tempRTID = Shader.PropertyToID("_TempRT"); // 临时RT的ID
        // 假设这是在你的RenderFeature或其他合适的地方进行初始化
        RenderTexture persistentRT = new RenderTexture(1920, 1080, 24);
        
        private Mesh mesh = null;

        private Mesh GetMesh()
        {
            if (null == mesh)
            {
                Vector3[] _vertices = new Vector3[] {
                    new Vector3(-1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f, 1.0f, 0.0f),
                    new Vector3(-1.0f, 1.0f, 0.0f),
                };
                Vector2[] _uvs = new Vector2[] {
                    new Vector2(0.0f, 0.0f),
                    new Vector2(1.0f, 0.0f),
                    new Vector2(1.0f, 1.0f),
                    new Vector2(0.0f, 1.0f),
                };
            
                ushort[] triangles = new ushort[] {
                    0, 2, 1, 0, 3, 2,
                };

                mesh = new Mesh();
                mesh.SetVertices(_vertices);
                mesh.SetUVs(0, _uvs);
                mesh.SetTriangles(triangles, 0);
            }
            
            return mesh;
        }
        
        public Material edgeStretchMaterial;
        public Material screenMaterial;
        private RenderTargetIdentifier currentTarget;

        // 新增字段用于Mesh对象和其Transform
        private Mesh targetMesh;
        private Matrix4x4 targetMeshTransform;
        
        // 修改构造函数以接收Mesh对象和其Transform
        public CustomRenderPass(Material edgeStretchMaterial, Material screenMaterial, Mesh mesh, Matrix4x4 transform)
        {
            this.edgeStretchMaterial = edgeStretchMaterial;
            this.screenMaterial = screenMaterial;
            this.targetMesh = mesh;
            this.targetMeshTransform = transform;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }
        
        public void UpdateTransform(Matrix4x4 newMatrix)
        {
            targetMeshTransform = newMatrix;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(tempRTID, cameraTextureDescriptor);
            tempRT = new RenderTargetIdentifier(tempRTID);
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
            // 设置临时RT为目标，并清除
            cmd.SetRenderTarget(tempRT);
            cmd.ClearRenderTarget(true, true, Color.clear);
            
            // 使用传入的Mesh对象和Transform信息绘制Mesh
            if ( edgeStretchMaterial != null)
            {
                cmd.DrawMesh(GetMesh(), Matrix4x4.identity, edgeStretchMaterial);
            }
            
            // 渲染到屏幕
            cmd.Blit(tempRT, persistentRT);
            cmd.SetRenderTarget(currentTarget);
            if (targetMesh != null && screenMaterial != null)
            {
                screenMaterial.SetTexture("_MainTex", persistentRT);
                cmd.DrawMesh(targetMesh, targetMeshTransform, screenMaterial);
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // 清理资源（如果需要）
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tempRTID);
        }
    }

    [FormerlySerializedAs("quadMaterial")] public Material edgeStretchMaterial; // 使用的材质
    public Material screenMaterial;
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
        m_ScriptablePass = new CustomRenderPass(edgeStretchMaterial,  screenMaterial, targetMesh, targetMeshTransform.localToWorldMatrix);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (edgeStretchMaterial == null || targetMesh == null || screenMaterial == null)
            return;

        m_ScriptablePass.edgeStretchMaterial = edgeStretchMaterial;
        m_ScriptablePass.screenMaterial = screenMaterial;
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


