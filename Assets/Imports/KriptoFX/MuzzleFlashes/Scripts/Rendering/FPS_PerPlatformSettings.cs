using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class FPS_PerPlatformSettings : MonoBehaviour
{
    private bool defaultOpaueColorUsing;
    private bool defaultDepthUsing;


    void OnEnable()
    {
        var cam = Camera.main;

        if (cam == null) return;
        var addCamData = cam.GetComponent<UniversalAdditionalCameraData>();
        if (addCamData != null)
        {
            defaultOpaueColorUsing = addCamData.requiresColorTexture;
            defaultDepthUsing = addCamData.requiresDepthTexture;
            addCamData.requiresColorTexture = true;
            addCamData.requiresDepthTexture = true;
        }
    }

    void OnDisable()
    {
        var cam = Camera.main;

        if (cam == null) return;
        var addCamData = cam.GetComponent<UniversalAdditionalCameraData>();
        if (addCamData != null)
        {
            addCamData.requiresColorTexture = defaultOpaueColorUsing;
            addCamData.requiresDepthTexture = defaultDepthUsing;
        }
    }
}
