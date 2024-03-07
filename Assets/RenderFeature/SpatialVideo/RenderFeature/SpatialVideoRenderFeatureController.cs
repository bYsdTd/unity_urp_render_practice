
using System;
using Unity.XR.PXR;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SpatialVideoRenderFeatureController : MonoBehaviour
{
    public Material _screenMaterial;
    public float _backPlaneDistance = 1;
    public int _Layout = 0;
    
    private SpatialVideoRenderPassFeature _renderPassFeature;
    private SpatialVideoRenderPassFeature.SpatialVideoRenderPass _renderPass;
    private static readonly int Layout = Shader.PropertyToID("_Layout");
    private static readonly int PlaneSize = Shader.PropertyToID("_PlaneSize");
    private static readonly int PlanePosition = Shader.PropertyToID("_PlanePosition");
    private static readonly int PlaneWorldToLocalMatrix = Shader.PropertyToID("_PlaneWorldToLocalMatrix");
    private static readonly int PlaneNormal = Shader.PropertyToID("_PlaneNormal");
    private MeshRenderer _meshRenderer;
    
    private void Awake()
    {
        PXR_MixedReality.EnableVideoSeeThrough(true);
    }

    // 应用恢复后，再次开启透视
    void OnApplicationPause(bool pause)
    {
        if (!pause)
        {
            PXR_MixedReality.EnableVideoSeeThrough(true);
        }
    }

    private void UpdateTransform()
    {
        var newMatrix = transform.localToWorldMatrix;
        var _targetMeshTransform = newMatrix;
        var _backPlaneSize = new Vector2(newMatrix.m00, newMatrix.m11);
        var _backPlanePosition = transform.position + transform.forward * _backPlaneDistance;
        var _backPlaneNormal = transform.forward;
            
        // 使用原始的旋转和缩放，因为我们只改变位置
        Quaternion _backPlaneRotation = transform.rotation;
            
        // Debug.Log($"Camera.main.fieldOfView:  {Camera.main.fieldOfView} distance {_backPlaneDistance}");
        // 根据相似三角形，后平面的大小与前平面成比例
        // float halffov = Camera.main.fieldOfView * 0.5f;
        // float d1 = trans.localScale.y * 0.5f / Mathf.Tan(Mathf.Deg2Rad * halffov);
        float d1 = (Camera.main.transform.position - transform.position).magnitude;
        float d2 = d1 + _backPlaneDistance;
            
        Vector3 _backPlaneScale = transform.localScale * d2 / d1;
            
        // 构造新的世界空间到局部空间的变换矩阵
        var _backPlaneWorldToLocal = Matrix4x4.TRS(_backPlanePosition, _backPlaneRotation, _backPlaneScale).inverse;
        
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        props.SetVector(PlaneSize,_backPlaneSize);
        props.SetVector(PlanePosition, _backPlanePosition);
        props.SetMatrix(PlaneWorldToLocalMatrix, _backPlaneWorldToLocal);
        props.SetVector(PlaneNormal, _backPlaneNormal);
        
        _meshRenderer.SetPropertyBlock(props);
    }
    
    private void Update()
    {
        if (_renderPass == null || _meshRenderer == null)
        {
            return;
        }
        _renderPass.UpdateTransform(transform, _backPlaneDistance);
        
        UpdateTransform();
    }

    private void OnEnable()
    {
        _renderPassFeature = SpatialVideoRenderPassFeature.Instance;
        if (_renderPassFeature == null)
        {
            return;
        }
        int key = GetHashCode();
        _renderPass = _renderPassFeature.AddSpatialVideoRenderPass(key);
        if (_renderPass == null)
        {
            return;
        }
        _renderPass.screenMaterial = _screenMaterial;
        _renderPass.UpdateTransform(transform, _backPlaneDistance);
        _renderPass._Layout = _Layout;
        var meshFilter = GetComponent<MeshFilter>();
        _renderPass._targetMesh = meshFilter.mesh;
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    private void OnDisable()
    {
        _renderPassFeature = SpatialVideoRenderPassFeature.Instance;
        if (_renderPassFeature == null)
        {
            return;
        }
        int key = GetHashCode();
        _renderPassFeature.RemoveSpatialVideoRenderPass(key);
    }
}
