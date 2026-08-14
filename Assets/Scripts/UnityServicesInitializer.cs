using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class UnityServicesInitializer : MonoBehaviour
{
    public static bool IsInitialized { get; private set; }

    private async void Start()
    {
        await InitializeServicesAsync();
    }

    public static async Task InitializeServicesAsync()
    {
        if (IsInitialized) return;

        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[UGS] Anonim Giriş Başarılı! Oyuncu ID: {AuthenticationService.Instance.PlayerId}");
            }

            IsInitialized = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UGS] Başlatma Hatası: {ex.Message}");
        }
    }
}