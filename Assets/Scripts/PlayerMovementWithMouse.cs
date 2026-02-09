using UnityEngine;
using static PlayerFarmController;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovementWithMouse : MonoBehaviour
{

    [SerializeField] private float speed = 3f;

    private PlayerFarmController farmController;
    private Rigidbody2D rb;
    private Animator animator;

    private Vector2 input;
    public bool isMove = false;
    private Vector2 direction;
    public bool CanMove => !farmController.isHoeing;
    private void Awake()
    {
        farmController = GetComponent<PlayerFarmController>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && CanMove)
        {
            if (!CanMove) return;
            input = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            isMove = true;
            
        }
        direction = (input - rb.position).normalized;

        animator.SetFloat("Horizontal", direction.x);
        animator.SetFloat("Vertical", direction.y);
        animator.SetFloat("Speed", isMove? 1:0);

    }

    private void FixedUpdate()
    {
        Move();
    }

    public void PlayHoeAnimation(FacingDirection dir)
    {
        switch (dir)
        {
            case FacingDirection.Top:
                animator.Play("Hoeing-Top");
                break;
            case FacingDirection.Down:
                animator.Play("Hoeing-Down");
                break;
            case FacingDirection.Left:
                animator.Play("Hoeing-Left");
                break;
            case FacingDirection.Right:
                animator.Play("Hoeing-Right");
                break;
        }
    }

    private void Move()
    {
        if (isMove)
        {
            rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);

            if (Vector2.Distance(rb.position, input) <= 0.1f)
            {
                isMove = false;
            }
        }
    }
}
