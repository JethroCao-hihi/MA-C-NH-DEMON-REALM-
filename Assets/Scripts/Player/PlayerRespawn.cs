using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    #region === VARIABLES ===

    [Header("Respawn Settings")]
    public Vector2 currentCheckpoint; // Tọa độ lưu game hiện tại

    [Header("Respawn Options")]
    [SerializeField] private int pitDamage = 20;
    [Tooltip("Hồi sinh sẽ hồi % máu (1-100). Để 100 nếu muốn đầy máu.")]
    [SerializeField, Range(1, 100)] private int respawnHealthPercent = 100;

    // Cached
    private Rigidbody2D rb;
    private PlayerHealth health; // Để gọi hàm trừ máu nếu cần
    private PlayerMovement movement;

    #endregion

    #region === UNITY CALLBACKS ===

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<PlayerHealth>();
        movement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        // Lấy vị trí lúc mới vào game làm Checkpoint mặc định ban đầu
        currentCheckpoint = transform.position;
    }

    #endregion

    #region === RESPAWN LOGIC ===

    // Hàm này dùng để cập nhật Checkpoint mới (Script Checkpoint sẽ gọi hàm này)
    public void UpdateCheckpoint(Vector2 newPos)
    {
        currentCheckpoint = newPos;
        Debug.Log("[PlayerRespawn] Đã lưu Checkpoint mới tại: " + currentCheckpoint);
    }

    // Hàm này dùng để kéo Player về Checkpoint (Script DeadZone sẽ gọi hàm này)
    public void Respawn()
    {
        // 1. Reset lại lực rơi (tránh việc vừa hồi sinh đã bị trôi đi)
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // 2. Dịch chuyển Player về vị trí Checkpoint
        transform.position = currentCheckpoint;
        Debug.Log("[PlayerRespawn] Player đã hồi sinh tại Checkpoint!");

        // 3. Reset trạng thái movement (nếu có)
        if (movement != null)
            movement.Respawn();

        // 4. (Tuỳ chọn) xử lý máu
        if (health != null)
        {
            if (health.IsDead)
                health.Respawn(respawnHealthPercent);
            else if (pitDamage > 0)
                health.TakeDamage(pitDamage);
        }
    }

    #endregion
}