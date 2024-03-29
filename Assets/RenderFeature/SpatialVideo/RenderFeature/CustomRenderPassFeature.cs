using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class CustomRenderPassFeature : ScriptableRendererFeature
{
    // 单例实例
    public static CustomRenderPassFeature Instance { get; private set; }
    class CustomRenderPass : ScriptableRenderPass
    {
        private CustomRenderPassFeature _owner;
        // private RenderTargetIdentifier tempRT; // 临时RT标识符
        // private int blurResultId = Shader.PropertyToID("_BlurVideoTexture"); // 临时RT的ID
        // 假设这是在你的RenderFeature或其他合适的地方进行初始化
        // private RenderTexture persistentRT = null;
        
        // 渲染模糊的结果RT
        private RenderTargetIdentifier _blurResultRT0;
        private readonly int _blurResultId0 = Shader.PropertyToID("_BlurVideoTexture0");
        private RenderTargetIdentifier _blurResultRT1;
        private readonly int _blurResultId1 = Shader.PropertyToID("_BlurVideoTexture1");
        private RenderTargetIdentifier _blurResultRT2;
        private readonly int _blurResultId2 = Shader.PropertyToID("_BlurVideoTexture2");
        
        // 开始拉伸像素以后结果的存储, 同时也是最后存储模糊的结果
        private RenderTargetIdentifier _extendResultRT;
        private readonly int _extendResultId = Shader.PropertyToID("_ExtendVideoTexture");
        
        // 开始拉伸像素以后结果的存储, 同时也是最后存储模糊的结果
        private RenderTargetIdentifier _blurResultRT;
        private readonly int _blurResultId = Shader.PropertyToID("_BlurVideoTexture");

        private Vector2 _sourceTextureSize;
        
        private Mesh _mesh = null;
        private Mesh GetMesh()
        {
            if (null == _mesh)
            {
                Vector3[] vertices = new Vector3[] {
                    new Vector3(-1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f, 1.0f, 0.0f),
                    new Vector3(-1.0f, 1.0f, 0.0f),
                };
                Vector2[] uvs = new Vector2[] {
                    new Vector2(0.0f, 1.0f),
                    new Vector2(1.0f, 1.0f),
                    new Vector2(1.0f, 0.0f),
                    new Vector2(0.0f, 0.0f),
                };
            
                ushort[] triangles = new ushort[] {
                    0, 2, 1, 0, 3, 2,
                };

                _mesh = new Mesh();
                _mesh.SetVertices(vertices);
                _mesh.SetUVs(0, uvs);
                _mesh.SetTriangles(triangles, 0);
            }
            
            return _mesh;
        }
        
        public Material edgeStretchMaterial;
        public Material screenMaterial;
        private RenderTargetIdentifier _currentTarget;

        // 新增字段用于Mesh对象和其Transform
        private Mesh _targetMesh;
        private Matrix4x4 _targetMeshTransform;
        
        // 修改构造函数以接收Mesh对象和其Transform
        public CustomRenderPass(Material edgeStretchMaterial, Material screenMaterial, Mesh mesh, Matrix4x4 transform, CustomRenderPassFeature owner)
        {
            this._owner = owner;
            this.edgeStretchMaterial = edgeStretchMaterial;
            this.screenMaterial = screenMaterial;
            this._targetMesh = mesh;
            this._targetMeshTransform = transform;
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        }
        
        public void UpdateTransform(Matrix4x4 newMatrix)
        {
            _targetMeshTransform = newMatrix;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // 复制相机的渲染纹理描述符，但将MSAA样本设置为1以禁用抗锯齿
            RenderTextureDescriptor sourceRTDescriptor = cameraTextureDescriptor;
            sourceRTDescriptor.msaaSamples = 1;

            if (edgeStretchMaterial.mainTexture != null)
            {
                sourceRTDescriptor.width = edgeStretchMaterial.mainTexture.width * 2;
                sourceRTDescriptor.height = edgeStretchMaterial.mainTexture.height * 2;
            }
            else
            {
                sourceRTDescriptor.width = 999;
                sourceRTDescriptor.height = 666;
            }

            _sourceTextureSize = new Vector2(sourceRTDescriptor.width, sourceRTDescriptor.height);
            
            // extend result RT
            cmd.GetTemporaryRT(_extendResultId, sourceRTDescriptor);
            _extendResultRT = new RenderTargetIdentifier(_extendResultId);
            
            // blur final pass RT
            cmd.GetTemporaryRT(_blurResultId, sourceRTDescriptor);
            _blurResultRT = new RenderTargetIdentifier(_blurResultId);
            
            // blur pass 1 RT
            sourceRTDescriptor.width /= 2;
            sourceRTDescriptor.height /= 2;
            cmd.GetTemporaryRT(_blurResultId0, sourceRTDescriptor);
            _blurResultRT0 = new RenderTargetIdentifier(_blurResultId0);
            
            // blur pass 2 RT
            sourceRTDescriptor.width /= 2;
            sourceRTDescriptor.height /= 2;
            cmd.GetTemporaryRT(_blurResultId1, sourceRTDescriptor);
            _blurResultRT1 = new RenderTargetIdentifier(_blurResultId1);
            
            // blur pass 3 RT
            sourceRTDescriptor.width /= 2;
            sourceRTDescriptor.height /= 2;
            cmd.GetTemporaryRT(_blurResultId2, sourceRTDescriptor);
            _blurResultRT2 = new RenderTargetIdentifier(_blurResultId2);
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            _currentTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("RenderSpatialVideoEffect");
            // 设置临时RT为目标，并清除
            cmd.SetRenderTarget(_extendResultRT);
            cmd.ClearRenderTarget(true, true, Color.clear);
            
            // 使用传入的Mesh对象和Transform信息绘制Mesh
            if ( edgeStretchMaterial != null)
            {
                cmd.DrawMesh(GetMesh(), Matrix4x4.identity, edgeStretchMaterial);
            }

            // blur pass
            BlurPass(cmd);
            
            // 渲染到屏幕
            cmd.SetRenderTarget(_currentTarget);
            if (_targetMesh != null && screenMaterial != null)
            {
                // screenMaterial.SetTexture("_BlurVideoTexture", persistentRT);
                cmd.DrawMesh(_targetMesh, _targetMeshTransform, screenMaterial);
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void BlurPass(CommandBuffer cmd)
        {
            // 1 pass
            Draw(_extendResultId, _blurResultId0, _sourceTextureSize, 
                _sourceTextureSize / 2, cmd);
            // 2 pass 
            _sourceTextureSize /= 2;
            Draw(_blurResultId0, _blurResultId1, _sourceTextureSize, 
                _sourceTextureSize / 2, cmd);
            
            // 3 pass 
            _sourceTextureSize /= 2;
            Draw(_blurResultId1, _blurResultId2, _sourceTextureSize, 
                _sourceTextureSize / 2, cmd);
            
            // 4 pass 
            _sourceTextureSize /= 2;
            Draw(_blurResultId2, _blurResultId, _sourceTextureSize, 
                _sourceTextureSize / 2, cmd);
        }
        
        private void Draw(int sourceId, int targetId, Vector2 sourceSize, Vector2 targetSize, CommandBuffer buffer)
        {
            // horizon
            int tempId = Shader.PropertyToID("_BlurVideoTextureHorizon");
                buffer.GetTemporaryRT(tempId, (int)targetSize.x, (int)targetSize.y,
                0, FilterMode.Bilinear, GraphicsFormat.R8G8B8A8_UNorm);
            
            buffer.SetRenderTarget(tempId);
            buffer.SetGlobalTexture("_SourceTexture", sourceId);
            _owner.blurHorizon.SetVector("_SourceTextureSize", new Vector4(sourceSize.x, sourceSize.y, 0f, 0f));
            buffer.DrawMesh(GetMesh(), Matrix4x4.identity, _owner.blurHorizon);
            
            // vertical
            buffer.SetRenderTarget(targetId);
            buffer.SetGlobalTexture("_SourceTexture", tempId);
            _owner.blurVertical.SetVector("_SourceTextureSize", new Vector4(targetSize.x, targetSize.y, 0f, 0f));
            buffer.DrawMesh(GetMesh(), Matrix4x4.identity, _owner.blurVertical);
            
            buffer.ReleaseTemporaryRT(tempId);
        }
        
        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // 清理资源（如果需要）
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_blurResultId0);
            cmd.ReleaseTemporaryRT(_blurResultId1);
            cmd.ReleaseTemporaryRT(_blurResultId2);
            cmd.ReleaseTemporaryRT(_blurResultId);
            cmd.ReleaseTemporaryRT(_extendResultId);
        }
    }

    public Material edgeStretchMaterial; // 使用的材质
    public Material screenMaterial;
    [NonSerialized]
    public Mesh targetMesh; // 场景中目标Mesh对象
    [NonSerialized]
    public Transform targetMeshTransform; // 目标Mesh的Transform
    public Material blurHorizon;
    public Material blurVertical;

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
        m_ScriptablePass = new CustomRenderPass(edgeStretchMaterial,  screenMaterial, targetMesh, targetMeshTransform.localToWorldMatrix, this);
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


