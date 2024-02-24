
using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class SpatialVideoRenderFeatureController : MonoBehaviour
{
    public Material _screenMaterial;
    public float _backPlaneDistance = 1;
    public int _Layout = 0;
    
    private SpatialVideoRenderPassFeature _renderPassFeature;
    private SpatialVideoRenderPassFeature.SpatialVideoRenderPass _renderPass;
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
        _renderPass._targetMesh = GetComponent<MeshFilter>().mesh;
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
