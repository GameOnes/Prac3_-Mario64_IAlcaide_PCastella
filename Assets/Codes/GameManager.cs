using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    static GameManager m_GameManager;
    List<IRestartGameElement> m_RestartGameElements = new List<IRestartGameElement>();
    void Awake()
    {
        if (m_GameManager != null)
        {
            GameObject.Destroy(gameObject);
            return;
        }
        m_GameManager = this;
        DontDestroyOnLoad(gameObject);
    }

    public static GameManager GetGameManager()
    {
               return m_GameManager;
    }

    public void AddRestartGameElement(IRestartGameElement restartGameElement)
    {
        Debug.Log("Added: " +  restartGameElement);
        m_RestartGameElements.Add(restartGameElement);
    }
    
    public void RestartGame()
    {
        foreach (IRestartGameElement l_Element in m_RestartGameElements) 
        {
            Debug.Log("Restarted: " + l_Element);
            l_Element.RestartGame();
        }
    }
}

