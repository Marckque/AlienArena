using UnityEngine;
using System.Collections;

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

    [Header("Physics material")]
    public PhysicMaterial[] physicMaterials;
}

public class Controllable : Entity
{
    private const float MAX_VELOCITY_MULTIPLIER = 0.01f; // So that we can work with more comfortable variables
    private const float SLOPE_RAY_OFFSET = 0.4f;
    private const float RAY_LENGTH = 0.5f;

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

    [Header("Other")]
    private bool m_IsOnFloor;
    private bool m_IsJumping;
    #endregion Variables

    protected override void Awake()
    {
        base.Awake();

        //pControllable.physicMaterials = new PhysicMaterial[2];
        //pControllable.maxVelocity *= MAX_VELOCITY_MULTIPLIER;
    }

    protected virtual void Update()
    {
        //m_IsOnFloor = IsOnFloor();

        if (Input.GetKeyDown(KeyCode.Space) && !m_IsJumping)
        {
            StartCoroutine(Jump());
            GetRigidbody().velocity = new Vector3(0f, pControllable.jumpHeight, 0f);
        }

        RotateTowardsMovementDirection();
        UpdateMovement();
    }

    private IEnumerator Jump()
    {
        m_IsJumping = true;
        yield return new WaitForSeconds(0.25f);
        m_IsJumping = false;
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

        Vector3 startPosition = transform.position;
        Ray ray = new Ray(startPosition, Vector3.down);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction, Color.red, 0.1f);

        if (Physics.Raycast(ray, out hit, RAY_LENGTH))
        {
            if (hit.collider)
            {
                m_LastDirection = Mathf.Sign(m_LastInput.x) * hit.transform.right;
            }

            if (Vector3.Dot(hit.transform.right, m_LastDirection) < 0f)
            {
                transform.position = new Vector3(transform.position.x, hit.point.y + 0.01f, 0f);
            }
        }

        /*
        Vector3 startPosition = transform.position + ExtensionMethods.Up(0.1f);
        Vector3 startPositionF = startPosition;

        startPosition.x -= Mathf.Sign(m_CurrentInput.x) * SLOPE_RAY_OFFSET;
        startPositionF.x += Mathf.Sign(m_CurrentInput.x) * SLOPE_RAY_OFFSET;

        Ray ray = new Ray(startPosition, -transform.up);
        Ray rayF = new Ray(startPositionF, transform.right);

        Debug.DrawRay(rayF.origin, rayF.direction * RAY_LENGTH, Color.red, 0.1f);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, RAY_LENGTH) && Physics.Raycast(rayF, out hit, RAY_LENGTH))
        {
            GetRigidbody().useGravity = true;
            return;
        }

        if (Physics.Raycast(ray, out hit, RAY_LENGTH) || Physics.Raycast(rayF, out hit, RAY_LENGTH))
        {
            //GetRigidbody().useGravity = false;

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
        */
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
        /*
        Ray rayUp = new Ray(transform.position + ExtensionMethods.Up(0.9f), transform.right);
        Debug.DrawRay(rayUp.origin, rayUp.direction * rayUp.direction.magnitude, Color.green, 0.1f);
        Ray rayDown = new Ray(transform.position + ExtensionMethods.Up(0.1f), transform.right);
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, transform.right);
        RaycastHit hit;
        if (Physics.Raycast(rayUp, out hit, RAY_LENGTH) || Physics.Raycast(rayDown, out hit, RAY_LENGTH) || Physics.Raycast(ray, out hit, RAY_LENGTH))
        if (Physics.Raycast(rayUp, out hit, RAY_LENGTH))
        {
            // Going right
            if (hit.collider)
            {
                if (m_LastInput.x > 0f && transform.position.x < hit.point.x || m_LastInput.x < 0f && transform.position.x > hit.point.x)
                {
                    pEntity.entityCollider.material = pControllable.physicMaterials[1];
                    Vector3 newPos = hit.point;
                    newPos.x -= 0.5f;
                    newPos.y = transform.position.y;
                    newPos.z = 0f;
                    transform.position = newPos;
                }
                else
                {
                    pEntity.entityCollider.material = pControllable.physicMaterials[0];
                    Vector3 newPos = hit.point;
                    newPos.x += 0.5f;
                    newPos.y = transform.position.y;
                    newPos.z = 0f;
                    transform.position = newPos;
                }
            }
            // Going left
            else 
            {
                pEntity.entityCollider.material = pControllable.physicMaterials[0];
                Vector3 newPos = hit.point;
                newPos.x += 0.5f;
                newPos.y = transform.position.y;
                newPos.z = 0f;
                transform.position = newPos;
            }
        }
        */

        float yVelocity = GetRigidbody().velocity.y;

        GetRigidbody().velocity = m_DesiredVelocity;
        GetRigidbody().velocity = Vector3.ClampMagnitude(GetRigidbody().velocity, pControllable.maxVelocity);

        GetRigidbody().velocity = new Vector3(GetRigidbody().velocity.x, yVelocity, 0f);

        // Makes sure we don't move when we should not
        /*
        if (Mathf.Approximately(GetRigidbody().velocity.y, 0f) && Mathf.Approximately(0f, m_VelocityMultiplier) && m_CurrentInput == Vector3.zero)
        {
            GetRigidbody().velocity = Vector3.zero;
            pEntity.entityCollider.material = pControllable.physicMaterials[1];
        }
        else
        {
            pEntity.entityCollider.material = pControllable.physicMaterials[0];
        }
        */
        /*
        if (m_IsOnFloor && m_CurrentInput == Vector3.zero)
        {
            pEntity.entityCollider.material = pControllable.physicMaterials[1];
        }
        else
        {
            pEntity.entityCollider.material = pControllable.physicMaterials[0];
        }
        */
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
        RaycastHit hit;

        if (!m_IsJumping && Physics.Raycast(ray, out hit, RAY_LENGTH))
        {
            transform.position = new Vector3(transform.position.x, hit.point.y);
            return true;
        }
        else
        {
            return false;
        }
    }
}