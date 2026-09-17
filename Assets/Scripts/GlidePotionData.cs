using UnityEngine;

[CreateAssetMenu(fileName = "Data_Glide", menuName = "Oyun/İksirler/Süzülme")]
public class GlidePotionData : PotionData
{
    [Header("Süzülme (Glide) Ayarları 🪂")]
    public float glideHeight = 3.8f;
    public float ascentSpeed = 6f;
    public float hoverFrequency = 3f;
    public float hoverAmplitude = 0.35f;
    public float descentSpeed = 4f;

    public override bool IsGliding => true;

    public override void OnStart(PlayerPotionController controller)
    {
        controller.GroundBaseY = controller.transform.position.y;
        controller.IsGlideLanding = false;
    }

    public override void OnFixedUpdate(PlayerPotionController controller)
    {
        Rigidbody rb = controller.Rb;
        if (rb == null) return;

        float targetY = controller.GroundBaseY + glideHeight + (Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude);
        float newY = Mathf.MoveTowards(rb.position.y, targetY, ascentSpeed * Time.fixedDeltaTime);

        rb.position = new Vector3(rb.position.x, newY, rb.position.z);
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    public override void OnEnd(PlayerPotionController controller)
    {
        controller.IsGlideLanding = true;
    }
}