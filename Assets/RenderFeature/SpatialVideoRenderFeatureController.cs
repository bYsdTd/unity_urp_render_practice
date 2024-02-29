
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

    private void Awake()
    {
        // PXR_MixedReality.EnableVideoSeeThrough(true);
    }

    // 应用恢复后，再次开启透视
    void OnApplicationPause(bool pause)
    {
        // if (!pause)
        // {
        //     PXR_MixedReality.EnableVideoSeeThrough(true);
        // }
    }
    
    private void Update()
    {
        if (_renderPass == null)
        {
            return;
        }
        _renderPass.UpdateTransform(transform, _backPlaneDistance);
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
        var meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.enabled = false;
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
