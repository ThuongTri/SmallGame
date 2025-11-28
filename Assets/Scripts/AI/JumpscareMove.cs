using UnityEngine;

public class JumpscareMove : MonoBehaviour
{
    [Header("Settings")]
    public float runSpeed = 6f; // Tốc độ chạy
    public float stopDistance = 0.5f; // Gần quá thì dừng (để tránh xuyên qua người)
    
    [Header("Animation")]
    public string runTriggerName = "Run"; // Tên Trigger hoặc Bool trong Animator để chạy

    private Transform player;
    private Animator anim;

    void Start()
    {
        // 1. Tự tìm Player
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // 2. Lấy Animator và bật chạy
        anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            // Nếu animator của bạn dùng Trigger để chạy
            anim.SetTrigger(runTriggerName); 
            
            // Hoặc nếu dùng Float Speed như con quái chính thì dùng dòng dưới:
            // anim.SetFloat("Speed", 1f); 
        }
    }

    void Update()
    {
        if (player == null) return;

        // 3. Luôn nhìn về phía Player (chỉ xoay trục Y)
        Vector3 direction = player.position - transform.position;
        direction.y = 0; // Giữ cho quái không bị nghiêng lên trời/xuống đất
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        // 4. Lao tới Player
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > stopDistance)
        {
            // Di chuyển thẳng về phía trước
            transform.Translate(Vector3.forward * runSpeed * Time.deltaTime);
        }
    }
}