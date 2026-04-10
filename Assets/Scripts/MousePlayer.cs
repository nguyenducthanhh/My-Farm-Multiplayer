using UnityEngine;
using static PlayerFarmController;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class MousePlayer : MonoBehaviour
{

    private Animator animator;
    private void Awake()
    {
        animator = GetComponent<Animator>();
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

    public void PlayWaterAnimation(FacingDirection dir)
    {
        switch (dir)
        {
            case FacingDirection.Top:
                animator.Play("Watering-Top");
                break;
            case FacingDirection.Down:
                animator.Play("Watering-Down");
                break;
            case FacingDirection.Left:
                animator.Play("Watering-Left");
                break;
            case FacingDirection.Right:
                animator.Play("Watering-Right");
                break;
        }
    }


}