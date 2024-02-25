using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class SpatialVideoRenderPassFeature : ScriptableRendererFeature
{
    // 单例实例
    public static SpatialVideoRenderPassFeature Instance { get; private set; }

    public class SpatialVideoRenderPass : ScriptableRenderPass
    {
        private SpatialVideoRenderPassFeature _owner;
        
        private readonly int _blurVideoHorizonId = Shader.PropertyToID("_BlurVideoTextureHorizon");
        // 渲染模糊的结果RT
        private RenderTargetIdentifier _blurResultRT0;
        private readonly int _blurResultId0 = Shader.PropertyToID("_BlurVideoTexture0");
        private RenderTargetIdentifier _blurResultRT1;
        private readonly int _blurResultId1 = Shader.PropertyToID("_BlurVideoTexture1");
        private RenderTargetIdentifier _blurResultRT2;
        private readonly int _blurResultId2 = Shader.PropertyToID("_BlurVideoTexture2");
        
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
        
        // 新增字段用于外部传入参数
        public Mesh _targetMesh;
        public Matrix4x4 _targetMeshTransform;
        public int key;
        public Material screenMaterial;
        public int _Layout = 1;
        public RenderTexture _blurTexture;
        
        private RenderTargetIdentifier _currentTarget;
        private RenderTargetIdentifier _currentDepth;
        
        private Vector2 _backPlaneSize;
        private Vector3 _backPlanePosition;
        private Vector3 _backPlaneNormal;
        
        
        private Matrix4x4 _backPlaneWorldToLocal;
        
        // 修改构造函数以接收Mesh对象和其Transform
        public SpatialVideoRenderPass(SpatialVideoRenderPassFeature owner)
        {
            this._owner = owner;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }
        
        public void UpdateTransform(Transform trans, float _backPlaneDistance)
        {
            var newMatrix = trans.localToWorldMatrix;
            _targetMeshTransform = newMatrix;
            _backPlaneSize = new Vector2(newMatrix.m00, newMatrix.m11);
            _backPlanePosition = trans.position + trans.forward * _backPlaneDistance;
            _backPlaneNormal = trans.forward;
            
            // 使用原始的旋转和缩放，因为我们只改变位置
            Quaternion _backPlaneRotation = trans.rotation;
            Vector3 _backPlaneScale = trans.localScale;

            // 构造新的世界空间到局部空间的变换矩阵
            _backPlaneWorldToLocal = Matrix4x4.TRS(_backPlanePosition, _backPlaneRotation, _backPlaneScale).inverse;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // 复制相机的渲染纹理描述符，但将MSAA样本设置为1以禁用抗锯齿
            RenderTextureDescriptor sourceRTDescriptor = cameraTextureDescriptor;
            sourceRTDescriptor.msaaSamples = 1;

            if (screenMaterial.mainTexture != null)
            {
                sourceRTDescriptor.width = screenMaterial.mainTexture.width;
                sourceRTDescriptor.height = screenMaterial.mainTexture.height;
            }                                                                                                                                                       
            else
            {
                sourceRTDescriptor.width = 1;
                sourceRTDescriptor.height = 1;
            }

            // TODO： 这里处理2D，3DLR，3DTB
            // 目前考虑是3D，LR的情况
            if (_Layout == 1)
            {
                sourceRTDescriptor.width /= 2;
            }
            else if (_Layout == 2)
            {
                sourceRTDescriptor.height /= 2;
            }
            
            _sourceTextureSize = new Vector2(sourceRTDescriptor.width, sourceRTDescriptor.height);
            
            
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
            _currentDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get($"RenderSpatialVideoEffect-{key}");

            // blur pass
            BlurPass(cmd);
            
            // 渲染到屏幕
            // 设置深度测试和写入状态
            cmd.SetRenderTarget(_currentTarget, _currentDepth);
            
            if (_targetMesh != null && screenMaterial != null)
            {
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                props.SetVector(PlaneSize,_backPlaneSize);
                props.SetVector(PlanePosition, _backPlanePosition);
                props.SetMatrix(PlaneWorldToLocalMatrix, _backPlaneWorldToLocal);
                props.SetVector(PlaneNormal, _backPlaneNormal);
                cmd.SetGlobalTexture("_BlurVideoTexture", _blurResultId);
                cmd.DrawMesh(_targetMesh, _targetMeshTransform, screenMaterial, 0, -1, props);
            }
            
            cmd.ReleaseTemporaryRT(_blurResultId0);
            cmd.ReleaseTemporaryRT(_blurResultId1);
            cmd.ReleaseTemporaryRT(_blurResultId2);
            cmd.ReleaseTemporaryRT(_blurResultId);
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void BlurPass(CommandBuffer cmd)
        {
            // 1 pass
            Draw(0, _blurResultId0, _sourceTextureSize, 
                _sourceTextureSize / 2, cmd, true);
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
        
        private void Draw(int sourceId, int targetId, Vector2 sourceSize, Vector2 targetSize, CommandBuffer buffer, bool isOrigin = false)
        {
            // horizon
            buffer.GetTemporaryRT(_blurVideoHorizonId, (int)targetSize.x, (int)targetSize.y,
            0, FilterMode.Bilinear, GraphicsFormat.R8G8B8A8_UNorm);
            
            buffer.SetRenderTarget(_blurVideoHorizonId);
            _owner.blurHorizon.SetVector(SourceTextureSize, new Vector4(sourceSize.x, sourceSize.y, 0f, 0f));
            if (isOrigin)
            {
                buffer.SetGlobalTexture("_SourceTexture", screenMaterial.mainTexture);
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                props.SetInt(Layout, _Layout);
                // _owner.blurHorizonOrigin.SetInt("_Layout", _Layout);
                buffer.DrawMesh(GetMesh(), Matrix4x4.identity, _owner.blurHorizon, 0, -1, props);
            }
            else
            {
                buffer.SetGlobalTexture("_SourceTexture", sourceId);
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                props.SetInt(Layout, 0);
                buffer.DrawMesh(GetMesh(), Matrix4x4.identity, _owner.blurHorizon, 0, -1, props);
            }
            
            // vertical
            buffer.SetRenderTarget(targetId);
            buffer.SetGlobalTexture("_SourceTexture", _blurVideoHorizonId);
            _owner.blurVertical.SetVector(SourceTextureSize, new Vector4(targetSize.x, targetSize.y, 0f, 0f));
            buffer.DrawMesh(GetMesh(), Matrix4x4.identity, _owner.blurVertical);
            
            buffer.ReleaseTemporaryRT(_blurVideoHorizonId);
        }
        
        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // 清理资源（如果需要）
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {

        }
    }
    
    public Material blurHorizon;
    public Material blurVertical;
    
    private Dictionary<int, SpatialVideoRenderPass> _renderPasses = new Dictionary<int, SpatialVideoRenderPass>();
    private static readonly int SourceTextureSize = Shader.PropertyToID("_SourceTextureSize");
    private static readonly int Layout = Shader.PropertyToID("_Layout");
    private static readonly int PlaneSize = Shader.PropertyToID("_PlaneSize");
    private static readonly int PlanePosition = Shader.PropertyToID("_PlanePosition");
    private static readonly int PlaneWorldToLocalMatrix = Shader.PropertyToID("_PlaneWorldToLocalMatrix");
    private static readonly int PlaneNormal = Shader.PropertyToID("_PlaneNormal");

    /// <inheritdoc/>
    public override void Create()
    {
        Instance = this;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        foreach (var renderPass in _renderPasses)
        {
            renderer.EnqueuePass(renderPass.Value);    
        }
    }

    public SpatialVideoRenderPass AddSpatialVideoRenderPass(int hashCode)
    {
        if (_renderPasses.TryGetValue(hashCode, out var pass))
        {
            return pass;
        }

        SpatialVideoRenderPass renderPass = new SpatialVideoRenderPass(this);
        renderPass.key = hashCode;
        _renderPasses.Add(hashCode, renderPass);
        return renderPass;
    }

    public void RemoveSpatialVideoRenderPass(int hashCode)
    {
        if (_renderPasses.ContainsKey(hashCode))
        {
            _renderPasses.Remove(hashCode);
        }
    }
}
