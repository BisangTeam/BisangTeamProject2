using System;
using UnityEngine;

public abstract class MineMapEvent : MonoBehaviour
{
    protected Action<float> OnMineMapSize;

    public virtual void MakeMineMap(float x)
    {
        
    }

    public void UpdateMineMap(float x)
    {
        OnMineMapSize?.Invoke(x);
    }
}
