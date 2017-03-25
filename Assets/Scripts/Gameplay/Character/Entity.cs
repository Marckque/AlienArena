using UnityEngine;

[System.Serializable]
public class EntityParameters
{
    [Header("Physics and graphics")]
    public Rigidbody entityRigidbody;
    public MeshRenderer entityModel;
}

public class Entity : MonoBehaviour
{
    [SerializeField]
    private EntityParameters m_EntityParameters = new EntityParameters();
    public EntityParameters pEntity { get { return m_EntityParameters; } }

    protected virtual void Awake()
    {
        SetRigidbody();
    }

    private void SetRigidbody()
    {
        pEntity.entityRigidbody.drag = 0f;
    }

    public Rigidbody GetRigidbody()
    {
        return pEntity.entityRigidbody;
    }
}