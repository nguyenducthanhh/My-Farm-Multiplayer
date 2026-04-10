using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 3f;

    private Rigidbody2D rb;
    private Animator animator;

    private Vector2 input;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (UsernameWizard.IsEnteringUsername)
        {
            input = Vector2.zero; // Force input = 0
            UpdateAnimation(input);
            return; // Bỏ qua việc nhận input
        }
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        UpdateAnimation(input);

    }

    private void FixedUpdate()
    {
        if (UsernameWizard.IsEnteringUsername)
        {
            rb.velocity = Vector2.zero; // Force dừng lại
            return;
        }
        Move(input);
    }

    private void Move(Vector2 direction)
    {
        rb.velocity = direction.normalized * speed;
    }

    private void UpdateAnimation(Vector2 direction)
    {
        animator.SetFloat("Horizontal", direction.x);
        animator.SetFloat("Vertical", direction.y);
        animator.SetFloat("Speed", direction.sqrMagnitude);
    }


}
