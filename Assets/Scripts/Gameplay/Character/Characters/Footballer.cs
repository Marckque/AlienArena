using UnityEngine;

public class Footballer : Controllable
{
    private Ball m_Ball;

    protected override void Update()
    {
        base.Update();

        if (m_Ball)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                Shoot();
            }
        }
    }

    private void Shoot()
    {
        m_Ball.SetDirection(transform.forward);
        m_Ball.Launch();
    }

    protected void OnTriggerEnter(Collider other)
    {
        Ball ball = other.GetComponent<Ball>();

        if (ball)
        {
            m_Ball = ball;
            m_Ball.InitialiseOnCatch(transform);
        }
    }
}