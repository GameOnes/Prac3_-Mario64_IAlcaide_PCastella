using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;

public class GoombaEnemy : MonoBehaviour, IRestartGameElement
{
    [Header("Settings")]
    public float m_WalkSpeed = 1f;
    public float m_ChaseSpeed = 2f;
    public float m_JumpForce = 4f;
    public float m_GroundRayDistance = 1f;
    public float m_ForwardRayDistance = 0.6f;
    public float m_ChaseDistance = 8.0f;
    public float m_AngleToRotate = 60.0f;
    public float m_MaxPatrolDistance = 8.0f;

    CharacterController m_CharacterController;
    Vector3 m_StartPosition;
    Quaternion m_StartRotation;
    Transform m_Player;
    Animator m_Animator;

    [Header("Die")]
    Vector3 m_KnockbackDirection;
    bool m_IsDead = false;
    bool m_ConfirmDead = false;
    public float m_KnockbackForce = 8.0f;

    private TState m_State;
    private TState m_PreviousState;

    Vector3 m_Speed = Vector3.zero;
    bool m_InsidePatrol = true;
    [Header("Loot")]
    public GameObject m_CoinPrefab;
    public GameObject m_StarPrefab;
    private Vector3 m_DeathPosition;
    public int m_CoinsToDrop = 1;
    public float m_StarDropChance = 0.1f;
    public float m_MaxTimeToSpawnLoot = 0.5f;

    [Header("AI Timers")]
    public float m_ActionDuration = 0.5f;
    public float m_MinDecisionTime = 1.0f;
    public float m_MaxDecisionTime = 2.0f;

    bool m_IsChasing = false;
    bool m_IsAlert = false;
    bool m_ReturningToZone = false;
    bool m_WaitingReturn = false;
    bool m_IsRotating = false;
    bool m_CanDecide = true;
    bool m_Dying = false;

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
        UpdatePatrolZone();
        UpdateRaycasts();
        UpdatePlayerDistance();
        switch (m_State)
        {
            case TState.FRONT: UpdateFrontState(); break;
            case TState.LEFT: UpdateLeftState(); break;
            case TState.RIGHT: UpdateRightState(); break;
            case TState.LEFTJUMP: UpdateLeftJumpState(); break;
            case TState.RIGHTJUMP: UpdateRightJumpState(); break;
            case TState.ALERT: UpdateAlertState(); break;
            case TState.CHASE: UpdateChaseState(); break;
            case TState.BACKTOZONE:  UpdateBackToZoneState(); break;
            case TState.DIE: UpdateDieState(); break;
        }
    }

    private void LateUpdate()
    {
        Quaternion l_LockZ = transform.rotation;
        l_LockZ.z = 0;
        transform.rotation = l_LockZ;

    }


    //UPDATES
    void UpdatePatrolZone()
    {
        Vector3 st = m_StartPosition;
        st.y = 0;
        Vector3 tm = transform.position;
        tm.y = 0;
        float l_Distance = Vector3.Distance(tm, st);  
        m_InsidePatrol = l_Distance < m_MaxPatrolDistance;
        if (!m_InsidePatrol && !m_ReturningToZone && m_State != TState.DIE)
        {
            SetBackToZoneState();
        }   
    }


    void UpdateRaycasts()
    {
        if (m_State == TState.CHASE || m_State == TState.ALERT)
            return;
        if (!Physics.Raycast(transform.position + transform.forward * 0.2f, Vector3.down, m_GroundRayDistance))
        {
            transform.Rotate(0, 180, 0);
        }
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, m_ForwardRayDistance))
        {
            transform.Rotate(0, 180, 0);
        }
    }

    void UpdatePlayerDistance()
    {
        if (m_ReturningToZone) return;
        if (m_State == TState.DIE || m_State == TState.BACKTOZONE) return;

        float l_Distance = Vector3.Distance(transform.position, m_Player.position);

        if (l_Distance < m_ChaseDistance)
        {
            if (m_State != TState.ALERT && m_State != TState.CHASE)
                SetAlertState();
            return;
        }

        bool isPatrolState = m_State == TState.FRONT || m_State == TState.LEFT || m_State == TState.RIGHT || m_State == TState.LEFTJUMP || m_State == TState.RIGHTJUMP;

        if (!isPatrolState || m_ReturningToZone || !m_InsidePatrol)
        if (m_State != TState.FRONT)
            SetFrontState();
        if (m_State == TState.FRONT)
            DoRandomAction();


    }

    // PATROL STATES

    // Front
    void SetFrontState()
    {
        if (m_IsDead) return;
        m_PreviousState = m_State;
        m_State = TState.FRONT;
        m_CanDecide = true;
        m_IsRotating = false;
        m_IsAlert = false;
        m_IsChasing = false;
    }

    void UpdateFrontState()
    {
        if (m_IsDead) return;
        MoveForward(m_WalkSpeed);
    }

    //Left
    void SetLeftState()
    {
        if (m_IsDead) return;
        m_PreviousState = m_State;
        m_State = TState.LEFT;
        if (m_IsDead) return;
        StartCoroutine(SmoothRotation(m_AngleToRotate, m_ActionDuration));
        if (m_IsDead) return;
        StartCoroutine(ReturnToFrontCoroutine());
    }

    
    void UpdateLeftState()
    {
        if (m_IsDead) return;
        MoveForward(m_WalkSpeed);
    }

    //Right
    void SetRightState()
    {
        if (m_IsDead) return;
        m_PreviousState = m_State;
        m_State = TState.RIGHT;
        if (m_IsDead) return;
        StartCoroutine(SmoothRotation(-m_AngleToRotate, m_ActionDuration));
        if (m_IsDead) return;
        StartCoroutine(ReturnToFrontCoroutine());
    }

    void UpdateRightState()
    {
        if (m_IsDead) return;
        MoveForward(m_WalkSpeed);
    }

    //Left Jump
    void SetLeftJumpState()
    {
        if (m_IsDead) return;
        m_Speed.y = m_JumpForce;
        m_PreviousState = m_State;
        m_State = TState.LEFTJUMP;
        if (m_IsDead) return;
        StartCoroutine(SmoothRotation(m_AngleToRotate, m_ActionDuration));
        if (m_IsDead) return;
        StartCoroutine(ReturnToFrontCoroutine());
    }

    void UpdateLeftJumpState()
    {
        if (m_IsDead) return;
        MoveForward(m_WalkSpeed);
    }

    //Right Jump
    void SetRightJumpState()
    {
        if (m_IsDead) return;
        m_Speed.y = m_JumpForce;
        m_PreviousState = m_State;
        m_State = TState.RIGHTJUMP;
        if (m_IsDead) return;
        StartCoroutine(SmoothRotation(-m_AngleToRotate, m_ActionDuration));
        if (m_IsDead) return;
        StartCoroutine(ReturnToFrontCoroutine());
    }

    void UpdateRightJumpState()
    {
        MoveForward(m_WalkSpeed);
    }

    //CHASING STATES
    
    //Alert 
    void SetAlertState()
    {
        if (m_IsDead) return;
        if (m_IsAlert || m_IsChasing || !m_InsidePatrol || m_ReturningToZone) return;
            
        Vector3 dir = m_Player.transform.position - transform.position;
        dir.y = 0;
        Quaternion l_LookAt = Quaternion.LookRotation(dir);
        float l_Angle = l_LookAt.eulerAngles.y;
        if (m_IsDead) return;
        StartCoroutine(SmoothRotation(l_Angle, m_ActionDuration * 0.75f));
        if (m_IsDead) return;
        m_PreviousState = m_State;
        m_State = TState.ALERT;
    }

    void UpdateAlertState()
    {
        if (m_IsDead) return;
        if (!m_IsAlert)
            StartCoroutine(AlertCoroutine());
    }

    IEnumerator AlertCoroutine()
    {
        if (m_IsDead) yield break;
        if (m_ReturningToZone || m_IsChasing || m_IsAlert || !m_InsidePatrol) yield break;
        m_IsAlert = true;
        if (m_IsAlert)
            m_Animator.SetTrigger("Alert");
        AnimatorStateInfo state = m_Animator.GetCurrentAnimatorStateInfo(0);
        while (!state.IsName("Alert"))
        {
            yield return null;
            if (m_IsDead) yield break;
            state = m_Animator.GetCurrentAnimatorStateInfo(0);
        }
        if (m_IsDead) yield break;
        while (state.normalizedTime < 1.11f)
        {
            yield return null;
            if (m_IsDead) yield break;
            state = m_Animator.GetCurrentAnimatorStateInfo(0);
        }
        m_IsAlert = false;
        if (m_IsDead) yield break;
        if (m_ReturningToZone || m_IsChasing || m_IsAlert || !m_InsidePatrol) yield break;
        float distance = Vector3.Distance(transform.position, m_Player.position);
        if (distance < m_ChaseDistance && m_State == TState.ALERT)
            SetChaseState();
    }
    //Chase
    void SetChaseState()
    {
        if (m_IsDead) return;
        m_PreviousState = m_State;
        m_State = TState.CHASE;
    }

    void UpdateChaseState()
    {
        if (m_IsDead) return;
        float distanceToPlayer = Vector3.Distance(transform.position, m_Player.position);
        if (m_IsChasing)
            m_Animator.SetBool("Chasing", true);
        if (distanceToPlayer > m_ChaseDistance)
        {
            m_IsChasing = false;
            m_IsAlert = false;
            if (!m_IsChasing)
                m_Animator.SetBool("Chasing", false);
            SetFrontState();
            return;
        }

        m_IsChasing = true;
        transform.LookAt(new Vector3(m_Player.position.x, transform.position.y, m_Player.position.z));
        MoveForward(m_ChaseSpeed);
    }

    //Back To Zone
    void SetBackToZoneState()
    {
        if (m_IsDead) return;
        if (m_ReturningToZone) return;
        m_ReturningToZone = true;
        m_PreviousState = m_State;
        m_State = TState.BACKTOZONE;
    }

    void UpdateBackToZoneState() 
    {
        if (m_IsDead) return;
        m_IsChasing = false;
        m_Animator.SetBool("Chasing", false);
        Vector3 zoneCenter = m_StartPosition;
        Vector3 goombaPos = transform.position;
        goombaPos.y = 0;
        zoneCenter.y = 0;
        float l_ReturnDistance = m_MaxPatrolDistance * 0.75f;
        float l_DistanceToCenter = Vector3.Distance(goombaPos, zoneCenter);
        if (l_DistanceToCenter <= l_ReturnDistance)
        {
            m_InsidePatrol = true;
            m_ReturningToZone = false;
            SetFrontState();
            return;
        }
        Vector3 dir = zoneCenter;
        dir.y = transform.position.y;
        transform.rotation = Quaternion.LookRotation(dir - transform.position);
        MoveForward(m_WalkSpeed);
    }

    //OTHER STATES

    //Die
    public void SetDieState()
    {
        
        Vector3 l_OppositeDirection = (transform.position - m_Player.position).normalized;
        l_OppositeDirection.y = 0;
        m_KnockbackDirection = l_OppositeDirection * m_KnockbackForce; 
        m_State = TState.DIE;
        transform.rotation = Quaternion.LookRotation(m_Player.position - transform.position);
    }

    void UpdateDieState()
    {
        m_IsChasing = false;
        m_IsAlert = false;
        m_ReturningToZone = false;
        m_WaitingReturn = false;
        m_InsidePatrol = true;
        m_CanDecide = false;
        m_IsDead = true;
        if (m_IsDead && !m_ConfirmDead)
            m_Animator.SetTrigger("Dead");
            
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
        if (m_IsDead && !m_ConfirmDead)
            StartCoroutine(DieCoroutine());

        m_ConfirmDead = true;
        
    }

    IEnumerator DieCoroutine()
    {
        if (m_Dying) yield break;
        AnimatorStateInfo l_State = m_Animator.GetCurrentAnimatorStateInfo(0);

        while(!l_State.IsName("DeathGoomba"))
        {
            yield return null;
            l_State = m_Animator.GetCurrentAnimatorStateInfo(0);
        }
        while (l_State.normalizedTime < 1.09f)
        {
            yield return null;
            l_State = m_Animator.GetCurrentAnimatorStateInfo(0);
        }
        m_DeathPosition = transform.position;
        Kill();
    }



    //OTHER FUNCTIONS


    IEnumerator SmoothRotation(float angle, float duration)
    {
        if (m_IsDead) yield break;
        if (m_ReturningToZone ||m_IsChasing || m_IsAlert) yield break;
        if (m_IsRotating) yield break;
        m_IsRotating = true;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, -angle, 0);
        float elapsed = 0;
        while (elapsed < duration)
        {
            if (m_IsDead) yield break;
            if (m_ReturningToZone || m_IsChasing || m_IsAlert) yield break;
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            yield return null;
        }
        if ((!m_ReturningToZone && !m_IsChasing && !m_IsAlert) || !m_IsDead)
            transform.rotation = endRot;
        m_IsRotating = false;
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

    void DoRandomAction()
    {
        if (m_IsDead) return;
        if (!m_CanDecide || m_State != TState.FRONT) return;
            StartCoroutine(DecisionCoroutine());
    }

    IEnumerator DecisionCoroutine()
    {
        if (m_IsDead) yield break;
        if (m_ReturningToZone || m_IsChasing || m_IsAlert) yield break;
        if (!m_CanDecide || m_State != TState.FRONT) yield break;
        m_CanDecide = false;
        float l_WaitTime = Random.Range(m_MinDecisionTime + m_ActionDuration, m_MaxDecisionTime + m_ActionDuration);
        yield return new WaitForSeconds(l_WaitTime);
        if (m_IsDead) yield break;
        if (m_ReturningToZone || m_IsChasing || m_IsAlert && !m_IsDead)
        {
             m_CanDecide = true;
             yield break;
        }
        float l_Value = Random.value;
        if (l_Value < 0.375f) SetLeftState();
        else if (l_Value < 0.75f) SetRightState();
        else if (l_Value < 0.875f) SetLeftJumpState();
        else SetRightJumpState();
        m_CanDecide = true;
    }

    IEnumerator ReturnToFrontCoroutine()
    {
        if (m_IsDead) yield break;
        if (m_ReturningToZone || m_IsChasing || m_IsAlert || m_WaitingReturn) yield break;
        m_WaitingReturn = true;
        yield return new WaitForSeconds(m_ActionDuration);
        if (m_IsDead) yield break;
        if (m_State != TState.ALERT && m_State != TState.CHASE && m_State != TState.BACKTOZONE)
            SetFrontState();
        m_WaitingReturn = false;
    }

    

    void DropLoot(GameObject coinPrefab, GameObject starPrefab, int coinsAmount, Vector3 position)
    {
        for (int i = 0; i < coinsAmount; i++)
        {
            float randomValue = Random.value;

            Instantiate(coinPrefab, position, Quaternion.identity).SetActive(true);

            if (randomValue <= m_StarDropChance)
                Instantiate(starPrefab, position, Quaternion.identity).SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Punch"))
        {
            SetDieState();
        }
    }



    public void RestartGame()
    {
        m_IsDead = false;
        m_ConfirmDead = false;
        m_CharacterController.enabled = false;
        transform.position = m_StartPosition;
        transform.rotation = m_StartRotation;
        m_CharacterController.enabled = true;
        gameObject.SetActive(true);
        SetFrontState();
    }

    public void Kill()
    {

        DropLoot(m_CoinPrefab, m_StarPrefab, m_CoinsToDrop, m_DeathPosition);
        gameObject.SetActive(false);
    }


}
