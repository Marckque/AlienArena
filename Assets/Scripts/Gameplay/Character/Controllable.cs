using UnityEngine;

[System.Serializable]
public class ControllableParameters
{
    [Header("Movement")]
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

    [Header("Jump")]
    public float jumpHeight = 1f;
}

public class Controllable : Entity
{
    private const float MAX_VELOCITY_MULTIPLIER = 0.01f; // So that we can work with more comfortable variables
    private const float SLOPE_RAY_OFFSET = 0.4f;
    private const float SLOPE_RAY_LENGTH = 0.6f;

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
    private Ray ray;
    private Ray rayUp;
    private Ray rayDown;
    #endregion Variables

    protected override void Awake()
    {
        base.Awake();

        ControllablePAR.maxVelocity *= MAX_VELOCITY_MULTIPLIER;
    }

    protected void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GetRigidbody().velocity = new Vector3(0f, ControllablePAR.jumpHeight, 0f);
        }

        RotateTowardsMovementDirection();
        UpdateMovement();

        ray = new Ray(transform.position + Vector3.up * 0.5f, transform.right);
        rayUp = new Ray(transform.position + ExtensionMethods.CustomVectorUp(0.80f), transform.right);
        rayDown = new Ray(transform.position + ExtensionMethods.CustomVectorUp(0.20f), transform.right);
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

        GetSlope();
    }

    private void GetSlope()
    {
        // Set startPosition right at the back (according to its current moving direction) of the character
        Vector3 startPosition = transform.position;
        startPosition.x -= m_LastDirection.x * SLOPE_RAY_OFFSET;

        Ray ray = new Ray(startPosition, -transform.up);

        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * SLOPE_RAY_LENGTH, Color.red);

        if (Physics.Raycast(ray, out hit, SLOPE_RAY_LENGTH))
        {
            if (hit.collider)
            {
                m_LastDirection = m_LastDirection.x * hit.transform.right;
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
        /*
        float yVelocity = GetRigidbody().velocity.y;

        GetRigidbody().velocity = m_DesiredVelocity;
        GetRigidbody().velocity = Vector3.ClampMagnitude(GetRigidbody().velocity, ControllablePAR.maxVelocity);

        GetRigidbody().velocity = new Vector3(GetRigidbody().velocity.x, yVelocity, 0f);
        */

        
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * 0.6f, Color.cyan);
        Debug.DrawRay(rayUp.origin, rayUp.direction * 0.6f, Color.cyan);
        Debug.DrawRay(rayDown.origin, ray.direction * 0.6f, Color.cyan);

        if (Physics.Raycast(rayUp, out hit, 0.6f) || Physics.Raycast(ray, out hit, 0.6f) || Physics.Raycast(rayDown, out hit, 0.6f))
        {
            if (m_LastDirection.x > 0f && transform.position.x < hit.point.x)
            {
                Vector3 newPos = hit.point;
                newPos.x -= 0.5f;
                newPos.y = transform.position.y;
                newPos.z = 0f;
                transform.position = newPos;
            }
            else if (m_LastDirection.x < 0f && transform.position.x > hit.point.x)
            {
                Vector3 newPos = hit.point;
                newPos.x += 0.5f;
                newPos.y = transform.position.y;
                newPos.z = 0f;
                transform.position = newPos;
            }
        }
        else
        {
            GetRigidbody().MovePosition(transform.position + m_DesiredVelocity);
        }

        m_DesiredVelocity = Vector3.zero;
    }
    #endregion Movement

    private void RotateTowardsMovementDirection()
    {
        transform.eulerAngles = m_LastDirection.x < 0 ? new Vector3(0f, 180f, 0f) : Vector3.zero;
    }

    private bool IsOnFloor()
    {
        Ray ray = new Ray(transform.position, -transform.up);
        return Physics.Raycast(ray, 0.1f);
    }
}