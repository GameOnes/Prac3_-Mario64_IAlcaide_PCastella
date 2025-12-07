using System;
using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour, IRestartGameElement
{
    public enum TPunchType
    {
        RIGHT_HAND = 0,
        LEFT_HAND,
        KICK
    }

    public Camera m_Camera;

    CharacterController m_CharacterController;
    Animator m_Animator;
    Vector3 m_StartPosition;
    Quaternion m_StartRotation;
    public float m_WalkSpeed;
    float m_VerticalSpeed = 0.0f;
    public Transform m_LookAt;
    [Range(0.0f, 1.0f)] public float m_RotationLerpPct = 0.8f;
    public float m_DampTime = 0.2f;
    CheckPoint m_CurrentCheckPoint;

    [Header("Jump")]
    [SerializeField] KeyCode m_JumpKeyCode = KeyCode.Space;
    int m_CurrentJumpId = 0;
    public float m_MaxTimeToComboJump = 1.5f;
    float m_LastJumpTime = 0;
    public float m_JumpSpeed = 12.0f;
    private float m_ActualJumpSpeed;
    public float m_MaxAngleToKillGoomba = 30.0f;
    public float m_KillJumpSpeed = 12.0f;
    private int m_MaxJumps = 3;

    [Header("Punch")]
    public float m_MaxTimeToComboPunch = 0.8f;
    int m_CurrentPunchId;
    float m_LastPunchTime;
    public GameObject m_RightHandPunchCollider;
    public GameObject m_LeftHandPunchCollider;
    public GameObject m_KickHandPunchCollider;

    [Header("Sound")]
    public AudioSource m_LeftFootStepAudioSource;
    public AudioSource m_RightFootStepAudioSource;

    CoinsController m_CoinsController = new CoinsController();
    LifeController m_LifeController = new LifeController();

    [Header("Platform")]

    public float m_BridgeHitForce = 10.0f;

    [Header("UI")]
    public GameUI m_GameUI;
    public int m_Coins = 0;
    public int m_Life = 8;

    [Header("Hit")]
    public RestartGameUI m_RestartGameUI;
    public float m_InmortalityTime = 1.0f;
    float m_LastHitTime = 0.0f;

    [Header("Input")]
    public int m_PunchMouseButtonDown = 0;

    [Header("Long Jump")]
    public KeyCode m_CrouchKey = KeyCode.LeftControl;
    public float m_LongJumpHorizontalSpeed = 12f;
    public float m_LongJumpVerticalSpeed = 8f;
    public float m_CrouchingSpeed = 2.0f;
    private bool m_IsCrouching = false;

    private void Awake()
    {
        m_CharacterController = GetComponent<CharacterController>();
        m_Animator = GetComponent<Animator>();
        m_CoinsController = DependencyInjector.GetDependency<CoinsController>();
    }

    private void Start()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        m_LastPunchTime = -m_MaxTimeToComboPunch;
        m_ActualJumpSpeed = m_JumpSpeed;
        m_RightHandPunchCollider.SetActive(false);
        m_LeftHandPunchCollider.SetActive(false);
        m_KickHandPunchCollider.SetActive(false);
        m_StartPosition = transform.position;
        m_StartRotation = transform.rotation;
        GameManager.GetGameManager().AddRestartGameElement(this);
    }

    void Update()
    {
        Vector3 l_Right = m_Camera.transform.right;
        Vector3 l_Forward = m_Camera.transform.forward;
        Vector3 l_Movement = Vector3.zero;

        l_Right.y = 0;
        l_Right.Normalize();
        l_Forward.y = 0;
        l_Forward.Normalize();


        if (Input.GetKey(KeyCode.D))
            l_Movement += l_Right;
        else if (Input.GetKey(KeyCode.A))
            l_Movement -= l_Right;

        if (Input.GetKey(KeyCode.W))
            l_Movement += l_Forward;

        else if (Input.GetKey(KeyCode.S))
            l_Movement -= l_Forward;



        l_Movement.Normalize();
        float l_Speed = m_WalkSpeed;
        float l_SpeedAnimatorValue = 1.0f;
        if (!m_IsCrouching)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                l_Speed *= 2;
                l_SpeedAnimatorValue = 1.5f;
            }

        }

        if (l_Movement.sqrMagnitude == 0.0f)
        {
            m_Animator.SetFloat("Speed", 0.0f, m_DampTime, Time.deltaTime);
        }
        else
        {
            m_Animator.SetFloat("Speed", l_SpeedAnimatorValue, m_DampTime, Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(l_Movement), m_RotationLerpPct);
        }


        l_Movement *= l_Speed * Time.deltaTime;
        m_VerticalSpeed += Physics.gravity.y * 1.5f * Time.deltaTime;
        l_Movement.y = m_VerticalSpeed * Time.deltaTime;
        CollisionFlags l_CollisionFlags = m_CharacterController.Move(new Vector3(l_Movement.x, l_Movement.y - 0.01f, l_Movement.z));

        if (((l_CollisionFlags & CollisionFlags.CollidedBelow) != 0 && m_VerticalSpeed < 0f) || ((l_CollisionFlags & CollisionFlags.CollidedAbove) != 0 && m_VerticalSpeed > 0f))
        {
            m_VerticalSpeed = 0f;
        }

        bool l_IsGrounded = (l_CollisionFlags & CollisionFlags.CollidedBelow) != 0;
        m_Animator.SetBool("IsGrounded", l_IsGrounded);
        UpdatePunch();
        UpdateJump();

        if (Input.GetKeyDown(m_CrouchKey))
        {
            m_Animator.SetBool("IsCrouching", true);
            m_WalkSpeed *= 0.5f;
            m_IsCrouching = true;
        }
        else if (Input.GetKeyUp(m_CrouchKey))
        {
            m_Animator.SetBool("IsCrouching", false);
            m_WalkSpeed /= 0.5f;
            m_IsCrouching = false;
        }

    }

    void LateUpdate()
    {
        UpdateElevator();
    }


    void UpdateJump()
    {

        if (Input.GetKeyDown(m_JumpKeyCode))
        {
            if (CanJump())
            {
                if (m_IsCrouching)
                {
                    LongJump();
                    return;
                }
                else
                {
                    Jump();
                }

            }
        }
    }

    bool CanJump()
    {
        bool l_IsGrounded = m_Animator.GetBool("IsGrounded");
        return l_IsGrounded;
    }

    public void Jump()
    {
        float l_ActualJumpCombo = Time.time - m_LastJumpTime;
        if (l_ActualJumpCombo > m_MaxTimeToComboJump)
        {
            m_CurrentJumpId = 0;
        }

        if (m_CurrentJumpId % m_MaxJumps == 0)
        {
            m_ActualJumpSpeed = m_JumpSpeed;
        }
        else if (m_CurrentJumpId % m_MaxJumps == 1)
        {
            m_ActualJumpSpeed *= 1.2f;
        }
        else if (m_CurrentJumpId % m_MaxJumps == 2)
        {
            m_ActualJumpSpeed *= 1.5f;
        }
        m_LastJumpTime = Time.time;

        m_VerticalSpeed = m_ActualJumpSpeed;
        m_Animator.SetTrigger("Jump");
        m_Animator.SetInteger("JumpId", m_CurrentJumpId);
        m_Animator.SetBool("IsGrounded", false);
        m_CurrentJumpId = (m_CurrentJumpId + 1) % m_MaxJumps;
    }
    public void LongJump()
    {
        Debug.Log("Long Jump");
        m_VerticalSpeed = m_LongJumpVerticalSpeed;
        Vector3 l_forward = transform.forward * m_LongJumpHorizontalSpeed;
        m_CharacterController.Move(l_forward * Time.deltaTime);
        m_LastJumpTime = Time.time + 0.5f;

        m_Animator.SetTrigger("LongJump");
        m_Animator.SetBool("IsGrounded", false);


    }

    public void JumpOverEnemy()
    {
        m_VerticalSpeed = m_KillJumpSpeed;
    }
    bool CanKillWithFeet(ControllerColliderHit hit)
    {
        float l_Dot = Vector3.Dot(hit.normal, Vector3.up);
        return m_VerticalSpeed < 0.0f && l_Dot > MathF.Cos(m_MaxAngleToKillGoomba * Mathf.Deg2Rad);
    }

    // ENDS JUMP LOGIC

    // PUNCH LOGIC

    void UpdatePunch()
    {
        if (CanPunch() && Input.GetMouseButtonDown(m_PunchMouseButtonDown))

            Punch();

    }

    bool CanPunch()
    {
        return !m_Animator.IsInTransition(0) && m_Animator.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash("Movement");
    }


    void Punch()
    {
        float l_DiffLastPunchTime = Time.time - m_LastPunchTime;
        if (l_DiffLastPunchTime < m_MaxTimeToComboPunch)
        {
            m_CurrentPunchId = (m_CurrentPunchId + 1) % 3;
        }
        else
        {
            m_CurrentPunchId = 0;
        }
        m_LastPunchTime = Time.time;
        m_Animator.SetTrigger("Punch");
        m_Animator.SetInteger("PunchId", m_CurrentPunchId);
    }

    public void SetActivePunch(TPunchType PunchType, bool Active)
    {
        if (PunchType == TPunchType.RIGHT_HAND)
        {
            m_RightHandPunchCollider.SetActive(Active);
        }
        else if (PunchType == TPunchType.LEFT_HAND)
        {
            m_LeftHandPunchCollider.SetActive(Active);
        }
        else if (PunchType == TPunchType.KICK)
        {
            m_KickHandPunchCollider.SetActive(Active);
        }
    }

    //ENDS PUNCH LOGIC


    //ELEVATOR LOGIC

    public float m_MaxAngleToAttachToElevator = 30.0f;
    Collider m_ElevatorCollider;

    bool CanAttachToElevator(Collider elevatorCollider)
    {
        Debug.Log("Attach?");
        return Vector3.Dot(elevatorCollider.transform.up, Vector3.up) > Mathf.Cos(m_MaxAngleToAttachToElevator * Mathf.Deg2Rad);
    }
    void AttachToElevator(Collider elevatorCollider)
    {
        Debug.Log("Attached");
        transform.SetParent(elevatorCollider.transform.parent);
        m_ElevatorCollider = elevatorCollider;
    }

    void DetachFromElevator()
    {
        Debug.Log("Detached");
        transform.SetParent(null);
        UpdateUpElevator();
        m_ElevatorCollider = null;
    }
    void UpdateUpElevator()
    {
        Vector3 l_Direction = transform.forward;
        l_Direction.y = 0.0f;
        l_Direction.Normalize();
        transform.rotation = Quaternion.LookRotation(l_Direction, Vector3.up);
    }

    void UpdateElevator()
    {
        if (m_ElevatorCollider != null)
            UpdateUpElevator();
    }


    // ENDS ELEVATOR LOGIC

    // HUD LOGIC

    public void AddCoin()
    {
        m_CoinsController.AddCoins(1);
    }

    public void Hit()
    {
        if (CantGetDamage())
        { return; }

        m_LastHitTime = Time.time;
        m_LifeController.AddLife(-1);
        m_Animator.SetInteger("Life", m_LifeController.GetValue());
        m_Animator.SetTrigger("Hit");
        Debug.Log(m_LifeController.GetValue());
        if (m_LifeController.GetValue() < 1)
        {
            StartCoroutine(DieAnimationCoroutine());
            return;
        }

    }

    IEnumerator DieAnimationCoroutine()
    {
        yield return null;
        AnimatorStateInfo l_State = m_Animator.GetCurrentAnimatorStateInfo(0);

        while (!l_State.IsName("death_front"))
        {
            yield return null;
            l_State = m_Animator.GetCurrentAnimatorStateInfo(0);
        }
        while (l_State.normalizedTime < 1.1f)
        {
            yield return null;
            l_State = m_Animator.GetCurrentAnimatorStateInfo(0);
        }
        Die();
    }
    bool CantGetDamage()
    {
        return Time.time < m_LastHitTime + m_InmortalityTime;
    }
    void Die()
    {
        m_GameUI.gameObject.SetActive(false);
        m_RestartGameUI.RestartUI();
        gameObject.SetActive(false);
    }
    //ENDS HUD LOGIC

    //OTHER LOGIC

    /*public void Step(AnimationEvent _AnimationEvent)
    {
        AudioSource l_currentAudioSource = null;

        if (_AnimationEvent.stringParameter == "Left")
        {
            l_currentAudioSource = m_LeftFootStepAudioSource;

        }
        else if (_AnimationEvent.stringParameter == "Right")
        {
            l_currentAudioSource = m_RightFootStepAudioSource;

        }
        AudioClip l_AudioClip = (AudioClip)_AnimationEvent.objectReferenceParameter;
        l_currentAudioSource.clip = l_AudioClip;
        l_currentAudioSource.Play();
    }
    */
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Goomba"))
        {
            GoombaEnemy l_GoombaEnemy = hit.collider.GetComponent<GoombaEnemy>();
            if (CanKillWithFeet(hit))
            {
                Debug.Log("HitOnTop");
                l_GoombaEnemy.SetDieState();
                JumpOverEnemy();
            }
            else
            {
                Hit();
            }
        }
        else if (hit.collider.CompareTag("Bridge"))
        {
            hit.rigidbody.AddForceAtPosition(hit.normal * m_BridgeHitForce, hit.point);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeathZone"))
        {
            GameManager.GetGameManager().RestartGame();
        }

        if (other.CompareTag("Elevator"))
        {
            if (CanAttachToElevator(other))
                AttachToElevator(other);
        }
        else if (other.CompareTag("CheckPoint"))
        {
            m_CurrentCheckPoint = other.GetComponent<CheckPoint>();
            m_StartPosition = m_CurrentCheckPoint.m_RestartPoint.position;
        }
        else if (other.CompareTag("Coin"))
        {
            m_CoinsController.AddCoins(1);
            other.gameObject.SetActive(false);
        }
        else if (other.CompareTag("Star"))
        {
            if (CanHeal(m_LifeController))
            {
                m_LifeController.AddLife(1);
                other.gameObject.SetActive(false);
            }
        }
    }
    bool CanHeal(LifeController life)
    {
        return life.GetValue() < m_Life;
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Elevator"))
            DetachFromElevator();
    }


    public void RestartGame()
    {
        if (m_CurrentCheckPoint != null)
        {
            m_StartPosition = m_CurrentCheckPoint.m_RestartPoint.position;
            m_StartRotation = m_CurrentCheckPoint.m_RestartPoint.rotation;
        }
        m_LifeController.SetValue(8);
        m_CoinsController.SetValue(0);
        m_CharacterController.enabled = false;
        transform.position = m_StartPosition;
        transform.rotation = m_StartRotation;
        m_GameUI.gameObject.SetActive(true);
        gameObject.SetActive(true);
        m_CharacterController.enabled = true;
    }
}
