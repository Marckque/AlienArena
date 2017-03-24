using UnityEngine;

[System.Serializable]
public class ControllableParameters
{
    [Header("Velocity")]
    public float maxVelocity = 1f;

    [Header("Acceleration"), Tooltip("If this is true, all acceleration values are useless.")]
    public bool accelerationIsOff;
    [Range(0f, 10f)]
    public float accelerationAmount = 1f;
    public AnimationCurve accelerationCurve;

    [Header("Deceleration"), Tooltip("If this is true, all deceleration values are useless.")]
    public bool decelerationIsOff;
    [Range(0f, 10f)]
    public float decelerationAmount = 1f;
    public AnimationCurve decelerationCurve;
}

public class Controllable : Entity
{
    #region Variables
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
    #endregion Variables

    protected void Update()
    {
        RotateTowardsMovementDirection();
        UpdateMovement();
    }

    protected void FixedUpdate()
    {
        SetDesiredVelocity(m_LastDirection * m_VelocityMultiplier * ControllablePAR.maxVelocity);
        ApplyForce();
    }

    #region Movement
    private void UpdateMovement()
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

    private void GetMovementInput()
    {
        m_CurrentInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, 0f).normalized;

        if (m_CurrentInput != Vector3.zero)
        {
            m_LastDirection = m_CurrentInput;
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

    private void SetDesiredVelocity(Vector3 force)
    {
        m_DesiredVelocity += force;
    }

    private void ApplyForce()
    {
        float yVelocity = GetRigidbody().velocity.y;

        GetRigidbody().velocity = m_DesiredVelocity;
        GetRigidbody().velocity = Vector3.ClampMagnitude(GetRigidbody().velocity, ControllablePAR.maxVelocity);

        GetRigidbody().velocity = new Vector3(GetRigidbody().velocity.x, yVelocity, 0f);

        m_DesiredVelocity = Vector3.zero;
    }
    #endregion Movement

    private void RotateTowardsMovementDirection()
    {
        transform.eulerAngles = m_LastDirection.x == -1 ? new Vector3(0f, 180f, 0f) : Vector3.zero;
    }

    private bool IsOnFloor()
    {
        Ray ray = new Ray(transform.position, -transform.up);
        return Physics.Raycast(ray, 0.1f);
    }
}