
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class SpatialVideoRenderFeatureController : MonoBehaviour
{
    public Material _screenMaterial;
    public float _backPlaneDistance = 1;
    public int _Layout = 0;
    
    private SpatialVideoRenderPassFeature renderPassFeature;
    // Start is called before the first frame update
    private void Start()
    {
        renderPassFeature = SpatialVideoRenderPassFeature.Instance;
        if (renderPassFeature == null)
        {
            return;
        }
        
        renderPassFeature.screenMaterial = _screenMaterial;
        renderPassFeature.targetMeshTransform = transform;
        renderPassFeature._layout = _Layout;
        renderPassFeature.targetMesh = GetComponent<MeshFilter>().mesh;
    }

    private void Update()
    {
        renderPassFeature = SpatialVideoRenderPassFeature.Instance;
        if (renderPassFeature == null)
        {
            return;
        }
        renderPassFeature.UpdateMeshTransform(transform, _backPlaneDistance);
    }
}
