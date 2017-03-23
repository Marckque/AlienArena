using UnityEngine;

[System.Serializable]
public class ControllableParameters
{
    [Header("Velocity")]
    public float maxVelocity = 1f;

    [Header("Acceleration and Deceleration")]
    public bool useAcceleration;
    [Range(0f, 10f)]
    public float accelerationAmount = 1f;
    public AnimationCurve accelerationCurve;

    public bool useDeceleration;
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

        // Uses acceleration and deceleration
        if (ControllablePAR.useAcceleration && ControllablePAR.useDeceleration)
        {
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
        // Uses acceleration only
        else if (ControllablePAR.useAcceleration)
        {
            if (m_CurrentInput != Vector3.zero)
            {
                Acceleration();
                UpdateVelocityMultiplier();
            }
            else
            {
                m_AccelerationTime = 0f;
                m_VelocityMultiplier = 0f;
            }
        }
        // Uses deceleration only
        else if (ControllablePAR.useDeceleration)
        {
            if (GetRigidbody().velocity != Vector3.zero && m_CurrentInput == Vector3.zero)
            {
                Deceleration();
                UpdateVelocityMultiplier();
            }
            else
            {
                m_DecelerationTime = 0f;
                m_VelocityMultiplier = m_LastDirection.x;
            }
        }
        // Neither use acceleration nor deceleration
        else
        {
            if (m_CurrentInput != Vector3.zero)
            {
                m_LastDirection = m_CurrentInput;
                m_VelocityMultiplier = 1f;
            }
            else
            {
                m_VelocityMultiplier = 0f;
            }
        }
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
            m_AccelerationTime += Time.deltaTime * ControllablePAR.accelerationAmount;
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
            m_DecelerationTime += Time.deltaTime * ControllablePAR.decelerationAmount;
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