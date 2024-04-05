using UnityEngine;
using UnityEngine.AI;

public class Player : MonoBehaviour
{
    [SerializeField] private Rigidbody rigidBody = default;
    [SerializeField] private Camera playerCamera = default;
    [SerializeField] private CapsuleCollider capsuleCollider = default;

    // Camera Movement
    [SerializeField] private float mouseSensitivity = default;

    // Ground Movement
    [SerializeField] private float maxMovementSpeed = default;
    [SerializeField] private float groundAcceleration = default;
    //[SerializeField] private float floorRaycastDistance = default;
    [SerializeField] private float verticalDrag = default;
    [SerializeField] private float horizontalDrag = default;
    [SerializeField] private float maxDegreesJumpPossibility = default;
    [SerializeField] private float jumpVelocity = default;

    //Air Movement
    [SerializeField] private float airAcceleration = default;
    [SerializeField] private float gravity = default;
    [SerializeField] private float maxVerticalDegrees = default;
    [SerializeField] private float minVerticalDegrees = default;

    private float cameraZoom;
    private float targetCameraZoom;

    private bool isInAirPreviousFrame;
    private bool isInAir;
    private bool hasFeetRayCastHit;
    private RaycastHit feetRayCastHit;

    private const KeyCode KEYCODE_FORWARD = KeyCode.W;
    private const KeyCode KEYCODE_BACKWARD = KeyCode.S;
    private const KeyCode KEYCODE_LEFT = KeyCode.A;
    private const KeyCode KEYCODE_RIGHT = KeyCode.D;
    private const KeyCode KEYCODE_JUMP = KeyCode.Space;

    private void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    private void Update()
    {
        Update_CameraMovement();
    }

    private void Update_CameraMovement()
    {
        float yDifference = Input.GetAxis("Mouse Y") * mouseSensitivity;
        float xDifference = Input.GetAxis("Mouse X") * mouseSensitivity;

        transform.Rotate(0, xDifference, 0);
        Vector3 cameraAngles = playerCamera.transform.localRotation.eulerAngles;
        cameraAngles = GetRealEulerAngle(cameraAngles);
        cameraAngles.x -= yDifference;
        cameraAngles.x = Mathf.Clamp(cameraAngles.x, minVerticalDegrees, maxVerticalDegrees);
        playerCamera.transform.localRotation = Quaternion.Euler(cameraAngles);
    }

    private Vector3 GetRealEulerAngle(Vector3 angle)
    {
        if (angle.x > 180)
            angle.x -= 360;

        if (angle.y != 180)
            return angle;

        if (angle.z != 180)
            return angle;

        if (angle.x > 0)
            angle.x = 180 - angle.x;
        else
            angle.x = -180 - angle.x;

        angle.y = 0;
        angle.z = 0;
        return angle;
    }

    private void FixedUpdate()
    {
        if (isInAirPreviousFrame)
            FixedUpdate_CheckGrounded();
        else
            FixedUpdate_StayGround();
        
        FixedUpdate_Drag();
        if (isInAir)
        {
            FixedUpdate_AirMovement();
            FixedUpdate_Gravity();
        }
        else
            FixedUpdate_GroundMovement();
        
        FixedUpdate_Jump();
        
        isInAirPreviousFrame = isInAir;
    }

    private void FixedUpdate_StayGround()
    {
        float castDistance = rigidBody.velocity.magnitude * Time.fixedDeltaTime + capsuleCollider.contactOffset + 1;
        if (!CapsuleCastInDirection(Vector3.down, castDistance, out feetRayCastHit))
        {
            hasFeetRayCastHit = false;
            isInAir = true;
            return;
        }
        hasFeetRayCastHit = true;

        Physics.Raycast(feetRayCastHit.point + Vector3.up * capsuleCollider.contactOffset, Vector3.down, out RaycastHit colliderHit, capsuleCollider.contactOffset * 2);
        Vector3 normal = colliderHit.normal;

        float verticalMagnitudeSquared = Mathf.Sqrt(normal.x * normal.x + normal.z * normal.z);
        float slope = Mathf.Atan2(verticalMagnitudeSquared, normal.y) * Mathf.Rad2Deg;

        if (slope > maxDegreesJumpPossibility)
        {
            isInAir = true;
            return;
        }

        isInAir = false;
        transform.position += Vector3.down * (feetRayCastHit.distance - capsuleCollider.contactOffset * 2);
    }

    private Vector3 GetTriangleNormal(MeshCollider collider, int triangleIndex)
    {
        int vertexIndex1 = collider.sharedMesh.triangles[triangleIndex * 3];
        int vertexIndex2 = collider.sharedMesh.triangles[triangleIndex * 3 + 1];
        int vertexIndex3 = collider.sharedMesh.triangles[triangleIndex * 3 + 2];

        Vector3 vertex1 = collider.sharedMesh.vertices[vertexIndex1];
        Vector3 vertex2 = collider.sharedMesh.vertices[vertexIndex2];
        Vector3 vertex3 = collider.sharedMesh.vertices[vertexIndex3];

        Vector3 side1 = vertex2 - vertex1;
        Vector3 side2 = vertex3 - vertex1;
        return Vector3.Cross(side1, side2);
    }

    private void FixedUpdate_CheckGrounded()
    {
        float castDistance = rigidBody.velocity.magnitude * Time.fixedDeltaTime + capsuleCollider.contactOffset + 1;
        if (!CapsuleCastInDirection(Vector3.down, castDistance, out feetRayCastHit))
        {
            hasFeetRayCastHit = false;
            isInAir = true;
            return;
        }
        hasFeetRayCastHit = true;

        float verticalMagnitudeSquared = Mathf.Sqrt(feetRayCastHit.normal.x * feetRayCastHit.normal.x + feetRayCastHit.normal.z * feetRayCastHit.normal.z);
        float slope = Mathf.Atan2(verticalMagnitudeSquared, feetRayCastHit.normal.y) * Mathf.Rad2Deg;

        if (slope > maxDegreesJumpPossibility)
        {
            isInAir = true;
            return;
        }

        isInAir = false;
        return;
    }

    private void FixedUpdate_GroundMovement()
    {
        Vector3 lookAtNormal = Quaternion.Inverse(transform.localRotation) * feetRayCastHit.normal;

        Vector3 forward = lookAtNormal;
        forward.x = 0;
        forward = Quaternion.Euler(90, 0, 0) * forward;
        forward = transform.localRotation * forward;

        Vector3 right = lookAtNormal;
        right.z = 0;
        right = Quaternion.Euler(0, 0, -90) * right;
        right = transform.localRotation * right;

        Vector3 desiredVelocity = new Vector3();
        if (Input.GetKey(KEYCODE_FORWARD))
            desiredVelocity += forward;

        if (Input.GetKey(KEYCODE_BACKWARD))
            desiredVelocity -= forward;

        if (Input.GetKey(KEYCODE_RIGHT))
            desiredVelocity += right;

        if (Input.GetKey(KEYCODE_LEFT))
            desiredVelocity -= right;

        desiredVelocity.Normalize();
        desiredVelocity *= maxMovementSpeed;

        Vector3 currentVelocity = rigidBody.velocity;
        Vector3 velocityDifference = desiredVelocity - currentVelocity;

        float moveAmount = groundAcceleration * Time.fixedDeltaTime;
        if (moveAmount > velocityDifference.magnitude)
            currentVelocity = desiredVelocity;
        else
            currentVelocity += velocityDifference.normalized * moveAmount;
        
        rigidBody.velocity = currentVelocity;
    }

    private Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        return Quaternion.Euler(angles) * (point - pivot) + pivot;
    }

    private void FixedUpdate_AirMovement()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        Vector3 desiredDirection = new Vector3();

        if (Input.GetKey(KEYCODE_FORWARD))
            desiredDirection += forward;

        if (Input.GetKey(KEYCODE_BACKWARD))
            desiredDirection -= forward;

        if (Input.GetKey(KEYCODE_RIGHT))
            desiredDirection += right;

        if (Input.GetKey(KEYCODE_LEFT))
            desiredDirection -= right;

        desiredDirection.Normalize();
        Vector3 desiredVelocity = desiredDirection * maxMovementSpeed;

        if (desiredVelocity.sqrMagnitude > 0)
        {
            Vector3 centerCapsuleTopSide = transform.position;
            centerCapsuleTopSide.y += 0.5f;

            Vector3 centerCapsuleBottomSide = transform.position;
            centerCapsuleBottomSide.y -= 0.5f;

            float castDistance = maxMovementSpeed * Time.fixedDeltaTime;
            int layerMask = 1 << LayerMask.NameToLayer("Floor");
            RaycastHit rayCastHit;
            if (Physics.CapsuleCast(centerCapsuleTopSide, centerCapsuleBottomSide, 0.5f, desiredDirection, out rayCastHit, castDistance, layerMask))
            {
                float verticalMagnitudeSquared = Mathf.Sqrt(rayCastHit.normal.x * rayCastHit.normal.x + rayCastHit.normal.z * rayCastHit.normal.z);
                float slope = Mathf.Atan2(verticalMagnitudeSquared, rayCastHit.normal.y) * Mathf.Rad2Deg;

                if (slope >= maxDegreesJumpPossibility)
                {
                    Vector3 unhinderedVelocity = desiredDirection * rayCastHit.distance / Time.fixedDeltaTime;
                    Vector3 hinderedVelocity = desiredDirection * (castDistance - rayCastHit.distance) / Time.fixedDeltaTime;
                    Vector3 horizontalNormal = rayCastHit.normal;
                    horizontalNormal.y = 0;
                    horizontalNormal.Normalize();
                    desiredVelocity = Vector3.ProjectOnPlane(hinderedVelocity, horizontalNormal) + unhinderedVelocity;
                }
            }
        }

        Vector3 currentVelocity = rigidBody.velocity;
        currentVelocity.y = 0;
        Vector3 velocityDifference = desiredVelocity - currentVelocity;

        float moveAmount = airAcceleration * Time.fixedDeltaTime;
        if (moveAmount > velocityDifference.magnitude)
            currentVelocity = desiredVelocity;
        else
            currentVelocity += velocityDifference.normalized * moveAmount;

        currentVelocity.y = rigidBody.velocity.y;
        rigidBody.velocity = currentVelocity;
    }

    private void FixedUpdate_Jump()
    {
        if (isInAir)
            return;

        if (!Input.GetKey(KEYCODE_JUMP))
            return;

        Vector3 velocity = rigidBody.velocity;
        velocity.y = jumpVelocity;
        rigidBody.velocity = velocity;
        isInAir = true;
    }

    private void FixedUpdate_Gravity()
    {
        Vector3 velocity = rigidBody.velocity;
        if (hasFeetRayCastHit && velocity.y < 0)
        {
            Vector3 gravityDirection = Vector3.RotateTowards(feetRayCastHit.normal, Vector3.down, 90 * Mathf.Deg2Rad, 1);
            velocity -= gravityDirection.normalized * gravity * Time.fixedDeltaTime;
        }
        else
        {
            velocity.y += gravity * Time.fixedDeltaTime;
        }
        rigidBody.velocity = velocity;
    }
    
    private void FixedUpdate_Drag()
    {
        Vector3 velocity = rigidBody.velocity;
        velocity.x *= 1 - Time.fixedDeltaTime * verticalDrag;
        velocity.z *= 1 - Time.fixedDeltaTime * verticalDrag;
        velocity.y *= 1 - Time.fixedDeltaTime * horizontalDrag;
        rigidBody.velocity = velocity;
    }

    private bool CapsuleCastInDirection(Vector3 direction, float distance, out RaycastHit rayCastHit)
    {
        Vector3 centerCapsuleTopSide = transform.position;
        centerCapsuleTopSide.y += 0.5f;
        centerCapsuleTopSide -= direction * capsuleCollider.contactOffset * 2;

        Vector3 centerCapsuleBottomSide = transform.position;
        centerCapsuleBottomSide.y -= 0.5f;
        centerCapsuleBottomSide -= direction * capsuleCollider.contactOffset * 2;

        int layerMask = 1 << LayerMask.NameToLayer("Floor");
        bool result =  Physics.CapsuleCast(centerCapsuleTopSide, centerCapsuleBottomSide, 0.5f, direction, out rayCastHit, distance + capsuleCollider.contactOffset * 2, layerMask);
        if (rayCastHit.distance <= 0)
            return false;

        rayCastHit.distance -= capsuleCollider.contactOffset * 2;
        return result;
    }
}