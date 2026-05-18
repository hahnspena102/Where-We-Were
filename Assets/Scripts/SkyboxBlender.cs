using UnityEngine;

public class SkyboxBlender : MonoBehaviour
{
  [Header("Crossfade Material")]
  [SerializeField] private Material crossFadeSkyboxMaterial;

  [Header("Startup Cubemaps")]
  [SerializeField] private Cubemap startCubemapA;
  [SerializeField] private Cubemap startCubemapB;
  [SerializeField] private bool fadeFromAToBOnStart = true;

  [Header("Blend Settings")]
  [SerializeField] private float blendDuration = 1.5f;
  [SerializeField] private bool assignMaterialOnStart = true;

  private static readonly int BlendId = Shader.PropertyToID("_Blend");
  private static readonly int CubemapAId = Shader.PropertyToID("_CubemapA");
  private static readonly int CubemapBId = Shader.PropertyToID("_CubemapB");

  private float blendT;
  private bool isBlending;
  private Cubemap pendingTarget;

  private void Start()
  {
    if (crossFadeSkyboxMaterial == null)
    {
      Debug.LogWarning("SkyboxBlender is missing a crossfade skybox material.", this);
      return;
    }

    if (assignMaterialOnStart)
    {
      RenderSettings.skybox = crossFadeSkyboxMaterial;
    }

    crossFadeSkyboxMaterial.SetFloat(BlendId, 0f);

    if (fadeFromAToBOnStart)
    {
      if (startCubemapA == null || startCubemapB == null)
      {
        Debug.LogWarning("SkyboxBlender startup fade is enabled, but Start Cubemap A or B is missing.", this);
        return;
      }

      SetImmediate(startCubemapA);
      CrossFadeTo(startCubemapB);
    }

  }

  private void Update()
  {
    if (!isBlending || crossFadeSkyboxMaterial == null)
    {
      return;
    }

    float duration = Mathf.Max(0.0001f, blendDuration);
    blendT += Time.deltaTime / duration;
    float blend = Mathf.Clamp01(blendT);
    crossFadeSkyboxMaterial.SetFloat(BlendId, blend);

    if (blend >= 1f)
    {
      CompleteBlend();
    }
  }

  public void CrossFadeTo(Cubemap targetCubemap)
  {
    if (targetCubemap == null)
    {
      Debug.LogWarning("CrossFadeTo called with a null cubemap.", this);
      return;
    }

    if (crossFadeSkyboxMaterial == null)
    {
      Debug.LogWarning("SkyboxBlender cannot blend without a crossfade skybox material.", this);
      return;
    }

    if (assignMaterialOnStart && RenderSettings.skybox != crossFadeSkyboxMaterial)
    {
      RenderSettings.skybox = crossFadeSkyboxMaterial;
    }

    Texture currentA = crossFadeSkyboxMaterial.GetTexture(CubemapAId);
    if (currentA == null)
    {
      crossFadeSkyboxMaterial.SetTexture(CubemapAId, targetCubemap);
      crossFadeSkyboxMaterial.SetTexture(CubemapBId, targetCubemap);
      crossFadeSkyboxMaterial.SetFloat(BlendId, 0f);
      isBlending = false;
      return;
    }

    pendingTarget = targetCubemap;
    crossFadeSkyboxMaterial.SetTexture(CubemapBId, pendingTarget);
    crossFadeSkyboxMaterial.SetFloat(BlendId, 0f);
    blendT = 0f;
    isBlending = true;
  }

  public void SetImmediate(Cubemap cubemap)
  {
    if (cubemap == null || crossFadeSkyboxMaterial == null)
    {
      return;
    }

    if (assignMaterialOnStart)
    {
      RenderSettings.skybox = crossFadeSkyboxMaterial;
    }

    crossFadeSkyboxMaterial.SetTexture(CubemapAId, cubemap);
    crossFadeSkyboxMaterial.SetTexture(CubemapBId, cubemap);
    crossFadeSkyboxMaterial.SetFloat(BlendId, 0f);
    isBlending = false;
  }

  private void CompleteBlend()
  {
    isBlending = false;
    blendT = 0f;

    if (pendingTarget != null)
    {
      crossFadeSkyboxMaterial.SetTexture(CubemapAId, pendingTarget);
    }

    crossFadeSkyboxMaterial.SetTexture(CubemapBId, crossFadeSkyboxMaterial.GetTexture(CubemapAId));
    crossFadeSkyboxMaterial.SetFloat(BlendId, 0f);
  }
}