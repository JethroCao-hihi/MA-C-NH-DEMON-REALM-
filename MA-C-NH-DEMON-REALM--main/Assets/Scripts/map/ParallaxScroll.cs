using UnityEngine;

public class ParallaxScroll : MonoBehaviour
{
    [Header("Độ trượt của nền (0.01 đến 0.1)")]
    public float parallaxSpeed = 0.05f; 
    
    private Transform cam;
    private Material mat;

    void Start()
    {
        // Tự động tìm Camera chính
        cam = Camera.main.transform;
        mat = GetComponent<Renderer>().material;
    }

    // Dùng LateUpdate để Background di chuyển SAU KHI Camera đã di chuyển (chống giật lag)
    void LateUpdate()
    {
        // 1. Background luôn chạy theo tọa độ X của Camera (Không bao giờ bị lòi viền)
        transform.position = new Vector3(cam.position.x, transform.position.y, transform.position.z);

        // 2. Cuộn hình ảnh bên trong để tạo hiệu ứng 3D Parallax
        mat.mainTextureOffset = new Vector2(cam.position.x * parallaxSpeed, 0);
    }
}