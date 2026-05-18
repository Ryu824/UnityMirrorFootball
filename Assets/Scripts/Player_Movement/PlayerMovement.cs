using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移动参数")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float turnSpeed = 20f;
    public float accelerationTime = 0.2f;
    public float decelerationTime = 0.15f;
    private float sprintStartTime = 0.3f;

    [Header("体力设置")]
    public float maxStamina = 100f;
    public float sprintCostRate = 20f;
    public float walkRegenRate = 10f;
    public float idleRegenRate = 25f;
    public float currentStamina;

    [Header("跳跃参数")]
    public float initialJumpForce = 7f;
    public float continuousJumpForce = 0.5f;
    public float maxJumpHoldTime = 0.3f;
    public float groundCheckRadius = 0.2f;
    public Vector3 groundCheckOffset = new Vector3(0, 0.1f, 0);
    public LayerMask groundLayer;

    // 组件引用
    private Animator m_Animator;
    private Rigidbody m_Rigidbody;

    // 内部状态变量
    private Vector3 currentVelocity = Vector3.zero;
    private float currentSpeed = 0f;
    private bool Grounded = true;
    private bool isJumping = false;
    private float jumpHoldTimer = 0f;

    // --- 新增：AI 输入缓存 ---
    private Vector3 aiMoveInput = Vector3.zero;
    private bool aiSprintInput = false;

    // 修复错误 CS0122：改为 public，供其他脚本访问
    public bool isAIControlled = false;


    public void OnFootstep()
    {
        // 目前留空即可，仅用来消除报错
        // 如果你想以后加脚步声，可以在这里写：
        // AudioManager.Instance.PlaySound("Footstep_Grass");
    }
    // 新增：AI 调用此接口移动
    public void SetMoveInput(Vector3 moveDir, bool sprint)
    {
        aiMoveInput = moveDir;
        aiSprintInput = sprint;
    }

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Animator = GetComponent<Animator>();
        currentStamina = maxStamina;

        if (m_Rigidbody != null)
        {
            m_Rigidbody.freezeRotation = true;
        }
    }

    void Update()
    {
        // 只有非AI控制时才读取键盘输入
        if (!isAIControlled)
        {
            HandlePlayerInput();
        }

        UpdateGroundCheck();
        HandleJumpLogic();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleRotation();
        UpdateAnimator();
    }

    // --- 原输入处理逻辑修改 ---
    void HandlePlayerInput()
    {
        // 冲刺输入
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            aiSprintInput = true;
        }

        if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
        {
            aiSprintInput = false;
        }

        // 移动输入
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        aiMoveInput = new Vector3(h, 0f, v);
    }

    // --- 补全缺失的方法：地面检测 ---
    void UpdateGroundCheck()
    {
        Vector3 checkPos = transform.position + groundCheckOffset;
        Grounded = Physics.CheckSphere(checkPos, groundCheckRadius, groundLayer);

        if (Grounded && isJumping)
        {
            isJumping = false;
        }
    }

    // --- 跳跃逻辑 ---
    void HandleJumpLogic()
    {
        if (!isAIControlled)
        {
            if (Input.GetKeyDown(KeyCode.Space) && Grounded && !isJumping)
            {
                isJumping = true;
                jumpHoldTimer = 0f;
                Vector3 vel = m_Rigidbody.velocity;
                vel.y = 0f;
                m_Rigidbody.velocity = vel;
                m_Rigidbody.AddForce(Vector3.up * initialJumpForce, ForceMode.Impulse);
            }

            if (Input.GetKey(KeyCode.Space) && isJumping)
            {
                jumpHoldTimer += Time.deltaTime;
                if (jumpHoldTimer < maxJumpHoldTime)
                {
                    m_Rigidbody.AddForce(Vector3.up * continuousJumpForce, ForceMode.Force);
                }
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                isJumping = false;
            }
        }
    }

    // --- 移动与体力逻辑 (使用 aiMoveInput) ---
    void HandleMovement()
    {
        Vector3 inputVector = aiMoveInput;

        if (inputVector.magnitude > 1f)
            inputVector.Normalize();

        bool isMoving = inputVector.magnitude > 0.1f;
        bool canSprint = false;

        // 处理冲刺与体力
        if (isMoving && aiSprintInput)
        {
            float cost = sprintCostRate * Time.fixedDeltaTime;
            if (currentStamina > cost)
            {
                currentStamina -= cost;
                canSprint = true;
            }
            else
            {
                currentStamina = 0f;
                aiSprintInput = false;
            }
        }

        // 处理体力恢复
        if (!canSprint)
        {
            float regenRate = isMoving ? walkRegenRate : idleRegenRate;
            currentStamina = Mathf.Min(currentStamina + regenRate * Time.fixedDeltaTime, maxStamina);
        }

        // 计算速度
        float targetSpeed = CalculateTargetSpeed(inputVector, canSprint);
        SmoothAcceleration(targetSpeed, inputVector);

        // 应用移动
        Vector3 movement = currentVelocity * Time.fixedDeltaTime;
        m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
    }

    float CalculateTargetSpeed(Vector3 inputVector, bool canSprint)
    {
        if (inputVector.magnitude > 0.1f)
        {
            if (canSprint)
            {
                return sprintSpeed;
            }
            return walkSpeed;
        }
        return 0f;
    }

    void SmoothAcceleration(float targetSpeed, Vector3 inputDirection)
    {
        Vector3 targetVelocity = inputDirection * targetSpeed;

        if (inputDirection.magnitude > 0.1f)
        {
            float acceleration = targetSpeed / accelerationTime;
            currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            float deceleration = currentSpeed / decelerationTime;
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, deceleration * Time.fixedDeltaTime);
        }
        currentSpeed = currentVelocity.magnitude;
    }

    void HandleRotation()
    {
        if (currentVelocity.magnitude > 0.1f)
        {
            Vector3 desiredForward = Vector3.RotateTowards(transform.forward, currentVelocity.normalized, turnSpeed * Time.fixedDeltaTime, 0f);
            m_Rigidbody.MoveRotation(Quaternion.LookRotation(desiredForward));
        }
    }

    void UpdateAnimator()
    {
        if (m_Animator != null)
        {
            m_Animator.SetFloat("Speed", currentSpeed);
            try
            {
                m_Animator.SetBool("Grounded", Grounded);
            }
            catch
            {
                // 忽略
            }
        }
    }
}
