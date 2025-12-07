using System;
using UnityEngine;

public class CameraController: MonoBehaviour
{
    public PlayerController m_Player;
    float m_Yaw = 0.0f;
    float m_Pitch = 0.0f;
    public float m_YawSpeed = 360.0f;
    public float m_PitchSpeed = 180.0f;
    public float m_MinPitch = -60.0f;
    public float m_MaxPitch = 80.0f;
    public float m_MinDistance = 3.0f;
    public float m_MaxDistance = 12.0f;
    public LayerMask m_LayerMask;
    public float m_OffsetDistance = 0.1f;
    public float m_IdleTimer = 5.0f;
    float m_CurrentIdleTimer;
    

    private void Start()
    {
        m_Yaw = transform.eulerAngles.y;
        m_CurrentIdleTimer = m_IdleTimer;
    }
    private void LateUpdate()
    {
        Vector3 l_LookAt = m_Player.m_LookAt.transform.position;
        float l_HorizontalAxis = Input.GetAxis("Mouse X");
        float l_VerticalAxis = Input.GetAxis("Mouse Y");

        bool l_InputDetected = Mathf.Abs(l_HorizontalAxis) > 0.01f || Mathf.Abs(l_VerticalAxis) > 0.01f;

        if (!l_InputDetected && m_Player.IsIdle())
        {
            m_CurrentIdleTimer -= Time.deltaTime;

            if (m_CurrentIdleTimer <= 0f)
            {
                m_Yaw = m_Player.transform.eulerAngles.y;
            }
        }
        else
        {
            m_CurrentIdleTimer = m_IdleTimer;
        }
        m_Yaw += l_HorizontalAxis * m_YawSpeed * Time.deltaTime;
        m_Pitch -= l_VerticalAxis * m_PitchSpeed * Time.deltaTime;
        m_Pitch = Mathf.Clamp(m_Pitch, m_MinPitch, m_MaxPitch); // lo hacemos porque si no el angulo se puede pasar de 90 y se voltea 
        // no lo hacemos con el jaw por que el pitch es el unico que recorre todo el angulo de visión.

        float l_YawRadians = m_Yaw * Mathf.Deg2Rad;
        float l_PitchRadians = m_Pitch * Mathf.Deg2Rad;
        Vector3 l_Direction = new Vector3(
            Mathf.Cos(l_PitchRadians) * Mathf.Sin(l_YawRadians),
            Mathf.Sin(l_PitchRadians),
            Mathf.Cos(l_PitchRadians) * Mathf.Cos(l_YawRadians)
        );

        float l_Distance = Vector3.Distance(l_LookAt, transform.position);
        l_Distance = Mathf.Clamp(l_Distance, m_MinDistance, m_MaxDistance);

        Ray l_Ray = new Ray(l_LookAt, -l_Direction);
        Vector3 l_DesiredPosition = l_LookAt - l_Direction * l_Distance;

        if (Physics.Raycast(l_Ray, out RaycastHit l_RaycastHit, l_Distance, m_LayerMask.value))
            l_DesiredPosition = l_RaycastHit.point + l_Direction * m_OffsetDistance;

        transform.position = l_DesiredPosition;
        transform.LookAt(l_LookAt);
    }


}
