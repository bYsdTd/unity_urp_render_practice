
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class SpatialVideoRenderFeatureController : MonoBehaviour
{
    public float _backPlaneDistance = 1;
    
    private SpatialVideoRenderPassFeature renderPassFeature;
    // Start is called before the first frame update
    private void Start()
    {
        renderPassFeature = SpatialVideoRenderPassFeature.Instance;
        if (renderPassFeature == null)
        {
            return;
        }

        renderPassFeature.targetMeshTransform = transform;
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
