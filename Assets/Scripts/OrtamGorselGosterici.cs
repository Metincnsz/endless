using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class OrtamGorselGosterici : MonoBehaviour
{
    private Renderer meshRenderer;
    private RawImage uiRawImage;
    private VideoPlayer videoPlayer;

    void Start()
    {
        meshRenderer = GetComponent<Renderer>();
        uiRawImage = GetComponent<RawImage>();
        videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null && (meshRenderer != null || uiRawImage != null))
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = true;
            videoPlayer.isLooping = true;
            videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
        }

        if (LevelManager.Instance != null && LevelManager.Instance.AktifOrtam != null)
        {
            Uygula(LevelManager.Instance.AktifOrtam);
        }
        else
        {
            Debug.LogWarning("OrtamGorselGosterici: LevelManager veya AktifOrtam bulunamadı!");
        }
    }

    void Update()
    {
        // OTOMASYON: Dinamik olarak doğan oyuncu kamerasını arka plan Canvas'ına bağlar
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.worldCamera == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = mainCam;
                canvas.planeDistance = 900f; // Arka planın arkada kalmasını sağlar
                Debug.Log($"OrtamGorselGosterici: Dynamic camera '{mainCam.name}' assigned to Canvas.");
            }
        }
    }

    private void Uygula(OrtamVerisi ortam)
    {
        if (ortam.ortamVideosu != null && videoPlayer != null)
        {
            videoPlayer.clip = ortam.ortamVideosu;

            if (uiRawImage != null)
            {
                RenderTexture rt = new RenderTexture(1920, 1080, 16);
                videoPlayer.targetTexture = rt;
                uiRawImage.texture = rt;
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            }
            else if (meshRenderer != null)
            {
                videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
                videoPlayer.targetMaterialRenderer = meshRenderer;
                videoPlayer.targetMaterialProperty = "_BaseMap";
            }

            videoPlayer.Play();
        }
        else if (ortam.ortamGorseli != null)
        {
            if (videoPlayer != null) videoPlayer.Stop();

            if (uiRawImage != null)
            {
                uiRawImage.texture = ortam.ortamGorseli;
            }
            else if (meshRenderer != null)
            {
                meshRenderer.material.mainTexture = ortam.ortamGorseli;
                meshRenderer.material.SetTexture("_BaseMap", ortam.ortamGorseli);
            }
        }
        else
        {
            Debug.LogWarning($"{ortam.name} için herhangi bir görsel veya video atanmamış!");
        }
    }
}