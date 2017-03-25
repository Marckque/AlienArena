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

    [Header("Layers")]
    public LayerMask slope;
}

public class Controllable : Entity
{
    private const float MAX_VELOCITY_MULTIPLIER = 0.01f; // So that we can work with more comfortable variables
    private const float SLOPE_RAY_OFFSET = 0.4f;
    private const float RAY_LENGTH = 0.6f;

    #region Variables
    [SerializeField]
    private ControllableParameters m_ControllableParameters = new ControllableParameters();
    public ControllableParameters pControllable { get { return m_ControllableParameters; } }

    [Header("Movement")]
    private AnimationCurve m_VelocityCurve;
    private bool m_InitialiseAcceleration = true;
    private bool m_InitialiseDeceleration;
    private float m_AccelerationTime;
    private float m_DecelerationTime;
    private float m_VelocityTime;
    private float m_VelocityMultiplier;
    private Vector3 m_CurrentInput;
    private Vector3 m_LastInput;
    private Vector3 m_LastDirection;
    private Vector3 m_DesiredVelocity;
    #endregion Variables

    protected override void Awake()
    {
        base.Awake();

        pControllable.maxVelocity *= MAX_VELOCITY_MULTIPLIER;
    }

    protected virtual void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GetRigidbody().velocity = new Vector3(0f, pControllable.jumpHeight, 0f);
        }

        RotateTowardsMovementDirection();
        UpdateMovement();
    }

    protected void FixedUpdate()
    {
        SetDesiredVelocity(m_LastDirection * m_VelocityMultiplier * pControllable.maxVelocity);
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
            m_LastInput = m_CurrentInput;
            m_LastDirection = m_LastInput;
        }

        GetSlope();
    }

    private void GetSlope()
    {
        // Set startPosition right at the back (according to its current moving direction) of the character
        Vector3 startPosition = transform.position + ExtensionMethods.Up(0.1f);
        Vector3 startPositionF = startPosition;

        startPosition.x -= Mathf.Sign(m_CurrentInput.x) * SLOPE_RAY_OFFSET;
        startPositionF.x += Mathf.Sign(m_CurrentInput.x) * SLOPE_RAY_OFFSET;

        Ray ray = new Ray(startPosition, -transform.up);
        Ray rayF = new Ray(startPositionF, -transform.up);
        Debug.DrawRay(rayF.origin, rayF.direction * RAY_LENGTH, Color.red, 0.1f);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, RAY_LENGTH) && Physics.Raycast(rayF, out hit, RAY_LENGTH))
        {
            GetRigidbody().useGravity = true;
            return;
        }

        if (Physics.Raycast(ray, out hit, RAY_LENGTH) || Physics.Raycast(rayF, out hit, RAY_LENGTH))
        {
            GetRigidbody().useGravity = false;

            if (hit.collider)
            {
                m_LastDirection = Mathf.Sign(m_LastInput.x) * hit.transform.right;
                m_LastDirection.z = 0f;
            }
        }

        if (!Physics.Raycast(ray, out hit, RAY_LENGTH) || !Physics.Raycast(rayF, out hit, RAY_LENGTH))
        {
            GetRigidbody().useGravity = true;
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
        m_VelocityCurve = pControllable.accelerationCurve;

        if (m_AccelerationTime < 1f)
        {
            if (pControllable.accelerationIsOff)
            {
                m_AccelerationTime = 1f;
            }
            else
            {
                m_AccelerationTime += Time.deltaTime * pControllable.accelerationAmount;
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
        m_VelocityCurve = pControllable.decelerationCurve;

        if (m_DecelerationTime < 1f)
        {
            if (pControllable.decelerationIsOff)
            {
                m_DecelerationTime = 1f;
            }
            else
            {
                m_DecelerationTime += Time.deltaTime * pControllable.decelerationAmount;
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
        Ray rayUp = new Ray(transform.position + ExtensionMethods.Up(0.9f), transform.right);
        Ray rayDown = new Ray(transform.position + ExtensionMethods.Up(0.1f), transform.right);
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, transform.right);
        RaycastHit hit;

        if (Physics.Raycast(rayUp, out hit, RAY_LENGTH) || Physics.Raycast(rayDown, out hit, RAY_LENGTH) || Physics.Raycast(ray, out hit, RAY_LENGTH))
        {
            // Going right
            if (m_LastInput.x > 0f && transform.position.x < hit.point.x)
            {
                Vector3 newPos = hit.point;
                newPos.x -= 0.5f;
                newPos.y = transform.position.y;
                newPos.z = 0f;
                transform.position = newPos;
            }
            // Going left
            else if (m_LastInput.x < 0f && transform.position.x > hit.point.x)
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