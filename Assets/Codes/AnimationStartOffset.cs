using UnityEngine;

[RequireComponent (typeof(Animation))]
public class AnimationStartOffset : MonoBehaviour
{
    public bool m_RandomStart;
    [Range(0.0f, 1.0f)] public float m_StartOffset;

    private void Start()
    {
        Animation l_Animation = GetComponent<Animation>();
        l_Animation[l_Animation.clip.name].normalizedTime = m_RandomStart ? Random.value : m_StartOffset;
        l_Animation.Sample();
    }
}
