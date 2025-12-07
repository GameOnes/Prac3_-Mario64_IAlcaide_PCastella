using UnityEngine;

public class LifeController
{
    int m_Life = 8;
    public delegate void OnLifeChanged(LifeController _LifeController);
    public event OnLifeChanged m_OnLifeChanged;


    public LifeController()
    {
        DependencyInjector.AddDependency<LifeController>(this);
    }
    public void AddLife(int l_Life)
    {
        m_Life += l_Life; 
        m_OnLifeChanged.Invoke(this);
    }
    public int GetValue()
    {
        return m_Life;
    }
    
    public void SetValue(int l_Value)
    {
        m_Life = l_Value;
    }


}
  
