using UnityEngine;

[CreateAssetMenu(fileName = "Data_Bounce", menuName = "Oyun/İksirler/Sıçrama")]
public class BouncePotionData : PotionData
{
    [Header("Sıçrama Kuvveti 🦘")]
    [Tooltip("Inspector'dan istediğiniz değeri girin (Örn: 25 - 45)")]
    public float bounceForce = 30f;

    public override void OnStart(PlayerPotionController controller)
    {
        if (controller.Karakter != null)
        {
            controller.Karakter.Sicra(bounceForce);
        }
        else if (controller.Rb != null)
        {
            controller.Rb.linearVelocity = new Vector3(controller.Rb.linearVelocity.x, 0f, controller.Rb.linearVelocity.z);
            controller.Rb.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);
        }
    }
}