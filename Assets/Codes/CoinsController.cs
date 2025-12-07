using UnityEngine;

public class CoinsController
{
    int m_Coins = 0;
    public delegate void OnCoinsChanged(CoinsController _CoinsController);
    public event OnCoinsChanged m_OnCoinsChanged;

    public CoinsController()
    {
        DependencyInjector.AddDependency<CoinsController>(this);
    }
    public void AddCoins(int coins)
    {
        m_Coins += coins;
        m_OnCoinsChanged.Invoke(this);
    }
    public int GetValue()
    {
        return m_Coins;
    }

    public void SetValue(int coins)
    {
        m_Coins = coins;
    }
}
  
