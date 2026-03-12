using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private bool onlyAffectPlayerTag = true;
    [SerializeField] private float recheckCooldown = 0.25f;

    private float lastSetTime = -999f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (onlyAffectPlayerTag && !collision.CompareTag("Player"))
            return;

        if (Time.time < lastSetTime + recheckCooldown)
            return;

        PlayerRespawn respawnScript = collision.GetComponent<PlayerRespawn>();
        if (respawnScript == null) return;

        respawnScript.UpdateCheckpoint(transform.position);
        lastSetTime = Time.time;

        // (Sau này bạn có thể thêm code bật Animation cột cờ sáng lên ở đây)
    }
}