﻿using UnityEngine;

[System.Serializable]
public class EntityParameters
{
    public Rigidbody entityRigidbody;    
}

public class Entity : MonoBehaviour
{
    [SerializeField]
    private EntityParameters m_EntityParameters = new EntityParameters();
    public EntityParameters EntityPAR { get { return m_EntityParameters; } }

    protected void Awake()
    {
        SetRigidbody();
    }

    private void SetRigidbody()
    {
        EntityPAR.entityRigidbody.drag = 0f;
    }

    public Rigidbody GetRigidbody()
    {
        return EntityPAR.entityRigidbody;
    }
}