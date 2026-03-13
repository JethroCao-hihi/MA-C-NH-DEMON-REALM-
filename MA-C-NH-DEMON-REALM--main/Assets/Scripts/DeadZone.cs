using UnityEngine;

public class DeadZone : MonoBehaviour
{
    [SerializeField] private bool onlyAffectPlayerTag = true;
    [SerializeField] private int damage = 9999;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (onlyAffectPlayerTag && !collision.CompareTag("Player"))
            return;

        //Gây damege
        var heath = collision.GetComponent<PlayerHealth>();
        if (heath != null)
        {
            heath.TakeDamage(damage);
        }

        var respawnScript = collision.GetComponent<PlayerRespawn>();
        if (respawnScript != null)
            respawnScript.Respawn();
    }
}