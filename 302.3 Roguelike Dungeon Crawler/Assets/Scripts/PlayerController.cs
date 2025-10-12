using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    //public int health = 10;
    //public int maxHealth = 10;
    //public int attackPower = 2;
    //public int defense = 1;
    public float moveSpeed;
    public float dashSpeed;
    public float dashDuration;
    private bool canDash = true;
    public float dashCooldown;
    private float moveX = 0f;
    private float moveY = 0f;

    private Vector2 moveDirection;
    private bool isDashing = false;

    private void Update()
    {
        if (!isDashing)
        {
            HandleMovementInput();
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartCoroutine(Dash());
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isDashing)
        {
            Move();
        }
    }

    private void HandleMovementInput()
    {
        moveX = 0f;
        moveY = 0f;

        if (Input.GetKey(KeyCode.W)) moveY = 1f;
        if (Input.GetKey(KeyCode.S)) moveY = -1f;
        if (Input.GetKey(KeyCode.A)) moveX = -1f;
        if (Input.GetKey(KeyCode.D)) moveX = 1f;

        moveDirection = new Vector2(moveX, moveY).normalized;

        if (moveDirection == Vector2.zero)
        {

            animator.Play("Idle");
        }
        else
        {
            // Set animation direction based on movement
            if (moveY > 0)
            {
                animator.Play("Run_Up");
            }
            else if (moveY < 0)
            {
                animator.Play("Run_Down");
            }
            else if (moveX > 0)
            {
                animator.Play("Run_Right");
            }
            else if (moveX < 0)
            {
                animator.Play("Run_Left");
            }
        }
    }

    private void Move()
    {
        transform.Translate(moveDirection * moveSpeed * Time.fixedDeltaTime);
    }

    private System.Collections.IEnumerator Dash()
    {
        if (!canDash)
        {
            //Debug.Log("Dash is on cooldown!");
            yield break;
        }

        canDash = false;
        isDashing = true;
        Vector2 dashDirection = moveDirection;
        float dashEndTime = Time.time + dashDuration;

        while (Time.time < dashEndTime)
        {
            transform.Translate(dashDirection * dashSpeed * Time.deltaTime);
            yield return null;
        }

        isDashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}
