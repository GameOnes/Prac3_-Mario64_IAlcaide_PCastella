using UnityEngine;

public class RestartGameUI : MonoBehaviour
{

    KeyCode m_RestartKey = KeyCode.R;
    void Awake()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKey(m_RestartKey))
        {
            GameManager.GetGameManager().RestartGame();
            gameObject.SetActive(false);
        }
    }
    public void RestartUI()
    {
        gameObject.SetActive(true);
    }
}
