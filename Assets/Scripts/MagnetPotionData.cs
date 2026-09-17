using UnityEngine;

[CreateAssetMenu(fileName = "Data_Magnet", menuName = "Oyun/İksirler/Mıknatıs")]
public class MagnetPotionData : PotionData
{
    [Header("Mıknatıs Çekim Ayarları 🧲")]
    public float forwardReach = 32f;
    public float trackWidth = 60f;
    public float reachHeight = 8f;
    public float pullSpeed = 35f;
    public bool adaptiveSpeed = true;
    public LayerMask goldLayerMask;

    public override void OnUpdate(PlayerPotionController controller)
    {
        Transform t = controller.transform;
        float behindTolerance = 2f;
        float totalDepth = forwardReach + behindTolerance;
        float forwardCenterOffset = (forwardReach - behindTolerance) * 0.5f;

        Vector3 boxCenter = t.position + (t.forward * forwardCenterOffset) + (Vector3.up * 1.5f);
        Vector3 boxHalfExtents = new Vector3(trackWidth * 0.5f, reachHeight * 0.5f, totalDepth * 0.5f);

        int mask = (goldLayerMask.value == 0) ? Physics.AllLayers : goldLayerMask.value;
        Collider[] hits = Physics.OverlapBox(boxCenter, boxHalfExtents, t.rotation, mask);

        Vector3 targetPos = t.position + Vector3.up * 0.8f;
        float playerSpeed = (controller.Karakter != null) ? controller.Karakter.ileriKosmaHizi : 8f;

        foreach (var hit in hits)
        {
            GoldCollectible gold = hit.GetComponent<GoldCollectible>();
            if (gold == null) gold = hit.GetComponentInParent<GoldCollectible>();

            if (gold != null)
            {
                float currentPullSpeed = pullSpeed;
                if (adaptiveSpeed)
                {
                    float distance = Vector3.Distance(gold.transform.position, targetPos);
                    currentPullSpeed = Mathf.Max(pullSpeed, (playerSpeed * 2.2f) + (distance * 3.5f));
                }

                gold.transform.position = Vector3.MoveTowards(
                    gold.transform.position,
                    targetPos,
                    currentPullSpeed * Time.deltaTime
                );
            }
        }
    }
}