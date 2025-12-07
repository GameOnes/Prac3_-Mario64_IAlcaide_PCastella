using UnityEngine;

public class PunchBehaviour : StateMachineBehaviour
{
    public PlayerController.TPunchType m_PunchType;
    [Range(0.0f,1.0f)]public float m_StartPrct;
    [Range(0.0f, 1.0f)] public float m_EndPrct;
    PlayerController m_PlayerController;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        m_PlayerController = animator.GetComponent<PlayerController>();
        m_PlayerController.SetActivePunch(m_PunchType, false);
    }
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        bool l_Active = stateInfo.normalizedTime>=m_StartPrct && stateInfo.normalizedTime <= m_EndPrct;
        m_PlayerController.SetActivePunch(m_PunchType, l_Active);
        
    }
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        m_PlayerController.SetActivePunch(m_PunchType, false);
    }

}
