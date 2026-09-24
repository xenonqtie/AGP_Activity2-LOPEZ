using UnityEngine;

public class Jump : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [SerializeField] private float minJumpHeight = 3f;
    [SerializeField] private float maxJumpHeight = 5f;
    [SerializeField] private float doubleJumpHeight = 3f;
    [SerializeField] private float timeToApex = 0.35f;
    [SerializeField] private float timeToFall = 0.25f;
    [SerializeField] private float timeToDoubleJumpApex = 0.3f;

    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float maxSpeedOnAir = 4f;
    [SerializeField] private float timeToReachMax = 0.3f;
    [SerializeField] private float timeToStop = 0.1f;
    [SerializeField] private float turnSpeed = 0.5f;
    [SerializeField] private float turnSpeedScalar = 2f;

    private float maxJumpVelocity => (2f * maxJumpHeight) / timeToApex;
    private float riseG => (2f * maxJumpHeight) / (timeToApex * timeToApex);
    private float cutG => (2f * maxJumpHeight * maxJumpHeight) / (minJumpHeight * timeToApex * timeToApex);
    private float fallG => (2f * maxJumpHeight) / (timeToFall * timeToFall);

    private float doubleJumpVelocity => (2f * doubleJumpHeight) / timeToDoubleJumpApex;
    private float doubleRiseG => (2f * doubleJumpHeight) / (timeToDoubleJumpApex * timeToDoubleJumpApex);

    private Vector3 velocity = Vector3.zero;
    private float groundPos;
    private bool isGrounded => Mathf.Approximately(transform.position.y, groundPos);
    private bool isJumpCut;
    private bool canDoubleJump;
    private bool isDoubleJumping;

    private float acceleration => (isGrounded ? maxSpeed : maxSpeedOnAir) / timeToReachMax;
    private float deceleration => (isGrounded ? maxSpeed : maxSpeedOnAir) / timeToStop;
    private float turnRate => (maxSpeed * turnSpeedScalar) / turnSpeed;

    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int HorizontalSpeedHash = Animator.StringToHash("HorizontalSpeed");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
    private static readonly int DoubleJumpHash = Animator.StringToHash("DoubleJump");

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Start()
    {
        groundPos = transform.position.y;
    }

    void Update()
    {
        HorizontalMovement();
        HandleJump();

        transform.position += velocity * Time.deltaTime;

        if (transform.position.y < groundPos)
        {
            transform.position = new Vector3(transform.position.x, groundPos, transform.position.z);
            velocity.y = 0;
        }

        UpdateAnimations();
    }

    private void HandleJump()
    {
        if (isGrounded)
        {
            canDoubleJump = true;
            isDoubleJumping = false;
            isJumpCut = false;
        }

        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded)
            {
                velocity.y = maxJumpVelocity;
                isJumpCut = false;
            }
            else if (canDoubleJump)
            {
                velocity.y = doubleJumpVelocity;
                canDoubleJump = false;
                isDoubleJumping = true;
                isJumpCut = false;

                if (animator != null)
                {
                    animator.SetTrigger(DoubleJumpHash);
                }
            }
        }

        if (Input.GetButtonUp("Jump") && velocity.y > 0)
        {
            isJumpCut = true;
        }

        float currentGravity = CalculateGravity();
        velocity.y -= currentGravity * Time.deltaTime;
    }

    private float CalculateGravity()
    {
        if (velocity.y < 0) return fallG;
        if (isDoubleJumping) return doubleRiseG;
        if (isJumpCut) return cutG;

        return riseG;
    }

    private void HorizontalMovement()
    {
        float input = Input.GetAxisRaw("Horizontal");
        float targetSpeed = maxSpeed * input;
        float targetAccel = ChooseAccel(input);
        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, targetAccel * Time.deltaTime);
    }

    private float ChooseAccel(float input)
    {
        bool isTurning = !Mathf.Approximately(Mathf.Sign(input), Mathf.Sign(velocity.x));
        if (Mathf.Abs(input) > 0)
        {
            return !isTurning ? acceleration : turnRate;
        }
        return deceleration;
    }

    private void UpdateAnimations()
    {
        if (spriteRenderer != null && Mathf.Abs(velocity.x) > 0.01f)
        {
            spriteRenderer.flipX = velocity.x < 0;
        }

        if (animator != null)
        {
            animator.SetBool(IsGroundedHash, isGrounded);
            animator.SetFloat(HorizontalSpeedHash, Mathf.Abs(velocity.x));
            animator.SetFloat(VerticalVelocityHash, velocity.y);
        }
    }
}