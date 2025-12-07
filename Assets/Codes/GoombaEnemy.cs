using System.Collections;
using UnityEngine;

public class GoombaEnemy : MonoBehaviour, IRestartGameElement
{
    [Header("Settings")]
    public float m_WalkSpeed = 1f;
    public float m_ChaseSpeed = 2f;
    bool m_IsChasing = false;
    bool m_IsAlert = false;
    public float m_JumpForce = 4f;
    public float m_GroundRayDistance = 1f;
    public float m_ForwardRayDistance = 0.6f;
    public float m_ChaseDistance = 6f;
    public float m_AngleToRotate = 60.0f;
    public Transform m_PatrolZone;

    CharacterController m_CharacterController;
    Vector3 m_StartPosition;
    Quaternion m_StartRotation;
    Transform m_Player;
    Animator m_Animator;

    [Header("Die")]
    Vector3 m_KnockbackDirection;
    bool m_IsDead = false;
    public float m_KnockbackForce = 8.0f;

    private TState m_State;
    private TState m_PreviousState;

    Vector3 m_Speed = Vector3.zero;
    bool m_InsidePatrol = true;
    [Header("Loot")]
    public GameObject m_CoinPrefab;
    public GameObject m_StarPrefab;

    public int m_CoinsToDrop = 1;
    public float m_StarDropChance = 0.1f;
    public float m_MaxTimeToSpawnLoot = 0.5f;

    [Header("AI Timers")]
    public float m_MinDecisionTime = 1.0f;
    public float m_MaxDecisionTime = 2.0f;
    public float m_ActionDuration = 0.5f;

    bool canDecide = true;

    enum TState
    {
        FRONT = 0,
        LEFT,
        RIGHT,
        LEFTJUMP,
        RIGHTJUMP,
        ALERT,
        CHASE,
        BACKTOZONE,
        DIE
    }

    void Awake()
    {
        m_CharacterController = GetComponent<CharacterController>();
        m_Player = GameObject.FindGameObjectWithTag("Player").transform;
        m_Animator = GetComponent<Animator>();
    }

    void Start()
    {
        GameManager.GetGameManager().AddRestartGameElement(this);
        m_StartPosition = transform.position;
        m_StartRotation = transform.rotation;
        SetFrontState();
    }

    public void Update()
    {
        HandlePatrolZone();
        HandleRaycasts();
        HandlePlayerDistance();
        switch (m_State)
        {
            case TState.FRONT: UpdateFrontState(); break;
            case TState.LEFT: UpdateLeftState(); break;
            case TState.RIGHT: UpdateRightState(); break;
            case TState.LEFTJUMP: UpdateLeftJumpState(); break;
            case TState.RIGHTJUMP: UpdateRightJumpState(); break;
            case TState.ALERT: UpdateAlertState(); break;
            case TState.CHASE: UpdateChaseState(); break;
            case TState.BACKTOZONE: UpdateBackToZoneState(); break;
            case TState.DIE: UpdateDieState(); break;
        }
        Debug.Log(m_State);
        AnimatorController();
    }

    private void LateUpdate()
    {
        Quaternion l_LockZ = transform.rotation;
        l_LockZ.z = 0;
        transform.rotation = l_LockZ;

    }
    void SetFrontState()
    {
        m_State = TState.FRONT;
    }

    void UpdateFrontState()
    {
        MoveForward(m_WalkSpeed);
    }

    void SetLeftState()
    {
        transform.Rotate(0, -m_AngleToRotate, 0);
        m_State = TState.LEFT;
        StartCoroutine(ReturnToFrontCoroutine());
    }

    void UpdateLeftState()
    {
        MoveForward(m_WalkSpeed);
    }

    void SetRightState()
    {
        transform.Rotate(0, m_AngleToRotate, 0);
        m_State = TState.RIGHT;
        StartCoroutine(ReturnToFrontCoroutine());
    }

    void UpdateRightState()
    {
        MoveForward(m_WalkSpeed);
    }

    void SetLeftJumpState()
    {
        transform.Rotate(0, -m_AngleToRotate, 0);
        m_Speed.y = m_JumpForce;
        m_State = TState.LEFTJUMP;
        StartCoroutine(ReturnToFrontCoroutine());
    }

    void UpdateLeftJumpState()
    {
        MoveForward(m_WalkSpeed);
    }

    void SetRightJumpState()
    {
        transform.Rotate(0, m_AngleToRotate, 0);
        m_Speed.y = m_JumpForce;
        m_State = TState.RIGHTJUMP;
        StartCoroutine(ReturnToFrontCoroutine());
    }

    void UpdateRightJumpState()
    {
        MoveForward(m_WalkSpeed);
    }

    void SetAlertState()
    {
        m_State = TState.ALERT;
        m_IsAlert = true;
        m_IsChasing = false;
        transform.LookAt(new Vector3(m_Player.position.x, transform.position.y, m_Player.position.z));
    }

    void UpdateAlertState()
    {
        StartCoroutine(AlertCoroutine());

    }
    IEnumerator AlertCoroutine()
    {
        AnimatorStateInfo state = m_Animator.GetCurrentAnimatorStateInfo(0);
        while (!state.IsName("Alert"))
        {
            yield return null;
            state = m_Animator.GetCurrentAnimatorStateInfo(0);
        }

        while (state.normalizedTime < 1.11f)
        {
            yield return null;
            state = m_Animator.GetCurrentAnimatorStateInfo(0);
        }
        m_IsAlert = false;
        SetChaseState();
    }
    void SetChaseState()
    {
        m_State = TState.CHASE;
        m_IsAlert = false;
        m_IsChasing = true;
    }

    void UpdateChaseState()
    {
        transform.LookAt(new Vector3(m_Player.position.x, transform.position.y, m_Player.position.z));
        MoveForward(m_ChaseSpeed);
    }

    void SetBackToZoneState()
    {
        m_State = TState.BACKTOZONE;
        m_IsAlert = false;
        m_IsChasing = false;
    }

    void UpdateBackToZoneState()
    {
        float l_RadiusZone = m_PatrolZone.localScale.x * 0.75f;

        float l_DistanceToCenter = Vector3.Distance(transform.position, m_PatrolZone.position);

        if (l_DistanceToCenter <= l_RadiusZone)
        {
            m_InsidePatrol = true;
            SetFrontState();
            return;
        }

        Vector3 dir = new Vector3(m_PatrolZone.position.x, transform.position.y, m_PatrolZone.position.z);
        transform.LookAt(dir);
        MoveForward(m_WalkSpeed);
    }

    public void SetDieState()
    {
        Debug.Log("SetDie");
        m_State = TState.DIE;
        Vector3 l_OppositeDirection = (transform.position - m_Player.position).normalized;
        l_OppositeDirection.y = 0;
        m_KnockbackDirection = l_OppositeDirection * m_KnockbackForce;
        m_IsDead = true;
    }
    void DropLoot(GameObject coinPrefab, GameObject starPrefab, int coinsAmount)
    {
        for (int i = 0; i < coinsAmount; i++)
        {
            float randomValue = Random.value;
            Vector3 spawnPos = transform.position;
            GameObject coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);
            coin.SetActive(true);
            if (randomValue <= m_StarDropChance)
            {
                GameObject star = Instantiate(starPrefab, spawnPos, Quaternion.identity);
                star.SetActive(true);
            }
        }
    }
    void UpdateDieState()
    {
        Debug.Log("Dead");
        m_IsDead = true;

        if (m_KnockbackDirection.magnitude > 0.1f)
        {
            m_CharacterController.Move(m_KnockbackDirection * Time.deltaTime);
            m_KnockbackDirection = Vector3.Lerp(m_KnockbackDirection, Vector3.zero, 5f * Time.deltaTime);
        }

        if (!m_CharacterController.isGrounded)
        {
            m_Speed.y += Physics.gravity.y * Time.deltaTime;
            m_CharacterController.Move(m_Speed * Time.deltaTime);
        }
        StartCoroutine(DieCoroutine());
    }

    IEnumerator DieCoroutine()
    {
        yield return null;
        AnimatorStateInfo l_State = m_Animator.GetCurrentAnimatorStateInfo(0);

        while (!l_State.IsName("DeathGoomba"))
        {
            yield return null;
            l_State = m_Animator.GetCurrentAnimatorStateInfo(0);
        }
        while (l_State.normalizedTime < 1f)
        {
            yield return null;
            l_State = m_Animator.GetCurrentAnimatorStateInfo(0);
        }
        //DropLoot(m_CoinPrefab, m_StarPrefab, m_CoinsToDrop);
        Kill();
    }

    void MoveForward(float l_Speed)
    {
        Vector3 l_Move = transform.forward * l_Speed;
        ApplyGravity();
        m_CharacterController.Move((l_Move + m_Speed) * Time.deltaTime);
    }

    void ApplyGravity()
    {
        if (m_CharacterController.isGrounded && m_Speed.y < 0)
            m_Speed.y = -1f;
        else
            m_Speed.y += Physics.gravity.y * Time.deltaTime;
    }

    void HandleRaycasts()
    {
        if (!Physics.Raycast(transform.position + transform.forward * 0.2f, Vector3.down, m_GroundRayDistance))
        {
            transform.Rotate(0, 180, 0);
        }
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, m_ForwardRayDistance))
        {
            transform.Rotate(0, 180, 0);
        }
    }

    void HandlePlayerDistance()
    {
        float l_Distance = Vector3.Distance(transform.position, m_Player.position);
        if (m_State == TState.BACKTOZONE || m_State == TState.DIE) return;

        if (l_Distance < m_ChaseDistance && m_State != TState.ALERT && m_State != TState.CHASE)
        {
            SetAlertState();
            return;
        }
        else if (l_Distance > m_ChaseDistance)
        {
            m_IsAlert = false;
            m_IsChasing = false;
            SetFrontState();
            if (m_State == TState.FRONT)
            {
                DoRandomAction();
            }
        }
    }

    void DoRandomAction()
    {
        if (m_State == TState.BACKTOZONE) return;
        if (!canDecide) return;
        if (m_State != TState.FRONT) return;

        StartCoroutine(DecisionCoroutine());


    }

    IEnumerator DecisionCoroutine()
    {
        canDecide = false;

        float l_WaitTime = Random.Range(m_MinDecisionTime, m_MaxDecisionTime);
        yield return new WaitForSeconds(l_WaitTime);

        float l_Value = Random.value;

        if (l_Value < 0.375f) SetLeftState();
        else if (l_Value < 0.75f) SetRightState();
        else if (l_Value < 0.875f) SetLeftJumpState();
        else SetRightJumpState();

        canDecide = true;
    }

    IEnumerator ReturnToFrontCoroutine()
    {
        yield return new WaitForSeconds(m_ActionDuration);
        SetFrontState();
    }

    void HandlePatrolZone()
    {
        if (!m_InsidePatrol && m_State != TState.BACKTOZONE && m_State != TState.DIE && m_State != TState.CHASE)
            SetBackToZoneState();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == m_PatrolZone)
            m_InsidePatrol = true;

        if (other.CompareTag("Punch"))
        {
            SetDieState();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == m_PatrolZone)
            m_InsidePatrol = false;
    }

    public void RestartGame()
    {
        m_CharacterController.enabled = false;
        transform.position = m_StartPosition;
        transform.rotation = m_StartRotation;
        m_CharacterController.enabled = true;
        gameObject.SetActive(true);
        m_IsDead = false;
        SetFrontState();
    }

    public void Kill()
    {
        DropLoot(m_CoinPrefab, m_StarPrefab, m_CoinsToDrop);
        gameObject.SetActive(false);
    }

    void AnimatorController()
    {
        m_Animator.SetBool("Chasing", m_IsChasing);
        m_Animator.SetBool("Alert", m_IsAlert);
        m_Animator.SetBool("Dead", m_IsDead);
    }
}
