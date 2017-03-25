using UnityEngine;

public class Ball : Entity
{
    public MeshRenderer m_GraphicalBall;

    private float m_ShootForce = 600f;
    private Vector3 m_Direction;

    protected override void Awake()
    {
        base.Awake();

        DeactivateGraphicalBall();
    }

    public void InitialiseOnCatch(Transform footballerTransform)
    {
        DeactivatePhysicalBall();
        ActivateGraphicalBall();
    }

    public void SetDirection(Vector3 direction)
    {
        m_Direction = direction;
    }

    public void Launch()
    {
        transform.position = m_GraphicalBall.transform.position;
        DeactivateGraphicalBall();
        ActivatePhysicalBall();

        GetRigidbody().AddForce(m_Direction * m_ShootForce);
    }

    public void ActivatePhysicalBall()
    {
        gameObject.SetActive(true);
    }

    public void DeactivatePhysicalBall()
    {
        gameObject.SetActive(false);
    }

    public void ActivateGraphicalBall()
    {
        m_GraphicalBall.enabled = true;
    }

    public void DeactivateGraphicalBall()
    {
        m_GraphicalBall.enabled = false;
    }
}

    

    