using UnityEngine;

[System.Serializable]
public class ControllableParameters
{
    [Header("Velocity")]
    public float maxVelocity = 1f;

    [Header("Acceleration and Deceleration"), Tooltip("If this is true, all other acceleration variables are useless.")]
    public bool accelerationIsOff;
    [Range(0f, 10f)]
    public float accelerationAmount = 1f;
    public AnimationCurve accelerationCurve;
    [Space(10), Tooltip("If this is true, all other deceleration variables are useless.")]
    public bool decelerationIsOff;
    [Range(0f, 10f)]
    public float decelerationAmount = 1f;
    public AnimationCurve decelerationCurve;
}

public class Controllable : Entity
{
    [SerializeField]
    private ControllableParameters m_ControllableParameters = new ControllableParameters();
    public ControllableParameters ControllablePAR { get { return m_ControllableParameters; } }

    [Header("Movement")]
    private AnimationCurve m_VelocityCurve;
    private bool m_InitialiseAcceleration = true;
    private bool m_InitialiseDeceleration;
    private float m_AccelerationTime;
    private float m_DecelerationTime;
    private float m_VelocityTime;
    private float m_VelocityMultiplier;
    private Vector3 m_CurrentInput;
    private Vector3 m_LastDirection;
    private Vector3 m_DesiredVelocity;

    protected void Update()
    {
        Movement();
    }

    protected void FixedUpdate()
    {
        UpdateVelocity(m_LastDirection * m_VelocityMultiplier * ControllablePAR.maxVelocity);
        ApplyForce();
    }

    private void Movement()
    {
        GetMovementInput();

        if (m_CurrentInput != Vector3.zero)
        {
            Acceleration();
        }
        else
        {
            Deceleration();
        }

        UpdateVelocityMultiplier();
    }

    private void Acceleration()
    {
        if (m_InitialiseAcceleration)
        {
            m_AccelerationTime = 1f - m_DecelerationTime;
            m_InitialiseAcceleration = false;
            m_InitialiseDeceleration = true;
        }

        m_DecelerationTime = 0f;
        m_VelocityCurve = ControllablePAR.accelerationCurve;

        if (m_AccelerationTime < 1f)
        {
            if (ControllablePAR.accelerationIsOff)
            {
                m_AccelerationTime = 1f;
            }
            else
            {
                m_AccelerationTime += Time.deltaTime * ControllablePAR.accelerationAmount;
            }
        }
        else
        {
            m_AccelerationTime = 1f;
        }
    }

    private void Deceleration()
    {
        if (m_InitialiseDeceleration)
        {
            m_DecelerationTime = 1f - m_AccelerationTime;
            m_InitialiseAcceleration = true;
            m_InitialiseDeceleration = false;
        }

        m_AccelerationTime = 0f;
        m_VelocityCurve = ControllablePAR.decelerationCurve;

        if (m_DecelerationTime < 1f)
        {
            if (ControllablePAR.decelerationIsOff)
            {
                m_DecelerationTime = 1f;
            }
            else
            {
                m_DecelerationTime += Time.deltaTime * ControllablePAR.decelerationAmount;
            }
        }
        else
        {
            m_DecelerationTime = 1f;
        }
    }

    private void UpdateVelocityMultiplier()
    {
        m_VelocityTime = m_AccelerationTime > 0 ? m_AccelerationTime : m_DecelerationTime;
        m_VelocityMultiplier = m_VelocityCurve.Evaluate(m_VelocityTime);
    }

    private void GetMovementInput()
    {
        m_CurrentInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, 0f).normalized;

        if (m_CurrentInput != Vector3.zero)
        {
            m_LastDirection = m_CurrentInput;
        }
    }

    private bool IsOnFloor()
    {
        Ray ray = new Ray(transform.position, -transform.up);
        return Physics.Raycast(ray, 0.1f);
    }

    private void UpdateVelocity(Vector3 force)
    {
        m_DesiredVelocity += force; 
    }

    private void ApplyForce()
    {
        GetRigidbody().velocity = m_DesiredVelocity;
        GetRigidbody().velocity = Vector3.ClampMagnitude(GetRigidbody().velocity, ControllablePAR.maxVelocity);

        m_DesiredVelocity = Vector3.zero;
    }
}