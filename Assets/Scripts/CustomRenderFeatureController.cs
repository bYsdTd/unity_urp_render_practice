using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(MeshFilter))]
public class CustomRenderFeatureController : MonoBehaviour
{
    public CustomRenderPassFeature renderPassFeature;
    // Start is called before the first frame update
    private void Start()
    {
        renderPassFeature = CustomRenderPassFeature.Instance;
        if (renderPassFeature == null)
        {
            return;
        }

        renderPassFeature.targetMeshTransform = transform;
        renderPassFeature.targetMesh = GetComponent<MeshFilter>().mesh;
    }

    private void Update()
    {
        renderPassFeature = CustomRenderPassFeature.Instance;
        if (renderPassFeature == null)
        {
            return;
        }
        renderPassFeature.UpdateMeshTransform(transform);
    }
}
