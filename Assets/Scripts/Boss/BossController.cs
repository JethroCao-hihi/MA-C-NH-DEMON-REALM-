using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator anim;

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 3f;
    private float nextAttackTime;

    [Header("Shoot Attack")]
    [SerializeField] private GameObject ballBulletPrefab;
    [SerializeField] private Transform[] firePoints;

    [Header("Slam Wave Attack")]
    [SerializeField] private GameObject wavePrefab;
    [SerializeField] private Transform leftWavePoint;
    [SerializeField] private Transform rightWavePoint;

    private static readonly int AttackShootHash = Animator.StringToHash("Attack_Shoot");
    private static readonly int AttackSlamHash = Animator.StringToHash("Attack_Slam");

    private void Awake()
    {
        if (anim == null)
            anim = GetComponent<Animator>();
    }

    private void Start()
    {
        nextAttackTime = Time.time + attackCooldown;
    }

    private void Update()
    {
        if (Time.time < nextAttackTime)
            return;

        ChooseRandomAttack();
        nextAttackTime = Time.time + attackCooldown;
    }

    private void ChooseRandomAttack()
    {
        if (anim == null) return;

        int randomAttack = Random.Range(0, 2);

        anim.ResetTrigger(AttackShootHash);
        anim.ResetTrigger(AttackSlamHash);

        if (randomAttack == 0)
            anim.SetTrigger(AttackShootHash);
        else
            anim.SetTrigger(AttackSlamHash);
    }

    private static void TrySetDirectionIfHasBossBullet(GameObject go, Vector2 dir)
    {
        if (go == null) return;

        BossBullet bullet = go.GetComponent<BossBullet>();
        if (bullet != null)
            bullet.SetDirection(dir);
    }

    private Vector2 GetDirectionFromPoint(Transform point)
    {
        // Convention: if the point is to the left of the boss, shoot left; otherwise shoot right.
        // Uses world positions to avoid local flip/rotation issues.
        if (point == null) return Vector2.right;

        float dx = point.position.x - transform.position.x;
        return dx < 0f ? Vector2.left : Vector2.right;
    }

    private Vector2 GetInvertedDirectionFromPoint(Transform point)
    {
        return -GetDirectionFromPoint(point);
    }

    // --- Animation Events ---

    public void SpawnBullet()
    {
        if (ballBulletPrefab == null || firePoints == null || firePoints.Length == 0)
            return;

        foreach (Transform point in firePoints)
        {
            if (point == null) continue;

            GameObject bulletGo = Instantiate(ballBulletPrefab, point.position, point.rotation);
            TrySetDirectionIfHasBossBullet(bulletGo, GetInvertedDirectionFromPoint(point));
        }
    }

    public void SpawnWave()
    {
        if (wavePrefab == null)
            return;

        if (leftWavePoint != null)
        {
            GameObject left = Instantiate(wavePrefab, leftWavePoint.position, leftWavePoint.rotation);
            TrySetDirectionIfHasBossBullet(left, GetInvertedDirectionFromPoint(leftWavePoint));
        }

        if (rightWavePoint != null)
        {
            GameObject right = Instantiate(wavePrefab, rightWavePoint.position, rightWavePoint.rotation);
            TrySetDirectionIfHasBossBullet(right, GetInvertedDirectionFromPoint(rightWavePoint));
        }
    }
}
