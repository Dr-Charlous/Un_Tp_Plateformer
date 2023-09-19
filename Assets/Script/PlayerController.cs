using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    [SerializeField] Vector2 _inputs;
    [SerializeField] bool _inputJump;
    [SerializeField] Rigidbody2D _rb;

    [Header("Movements")]
    [SerializeField] float _walkSpeed;
    [SerializeField] float _acceleration;

    [Header("Groundcheck")]
    [SerializeField] float _groundOffset;
    [SerializeField] float _groundRadius;
    [SerializeField] LayerMask _GroundLayer;

    [Header("Jump")]
    [SerializeField] float _timerMinBetweenJump;
    [SerializeField] float _jumpForce;
    [SerializeField] float _velocityFallMin;
    [SerializeField] [Tooltip("Gravity when the player goes up and press jump")] float _gravityUpJump;
    [SerializeField] [Tooltip("Gravity otherwise")] float _gravity;
    [SerializeField] float _jumpInputTimer = 0.1f;


    [SerializeField] Collider2D[] _collidersGround;
    [SerializeField] bool _isGrounded;
    [SerializeField] float _timerNoJump;
    [SerializeField] float _timerSinceJumpPressed;
    [SerializeField] float _TimeSinceGrounded;
    [SerializeField] float _coyoteTime;
    [SerializeField] float _slopeDetectOffset;
    [SerializeField] bool _isOnSlope;
    [SerializeField] Collider2D _collider;
    [SerializeField] PhysicsMaterial2D _physicsFriction;
    [SerializeField] PhysicsMaterial2D _physicsNoFriction;
    Vector3 _offsetCollisionBox;
    Vector3 _offsetToReplace;
    Vector2 _collisionBox;

    RaycastHit2D[] _hitResults = new RaycastHit2D[2];
    float[] directions = new float[] { 1, -1 };

    private void Update()
    {
        HandleInputs();
    }

    private void FixedUpdate()
    {
        HandleMovements();
        HandleGrounded();
        HandleJump();
        HandleSlope();
        HandleCorners();
    }

    void HandleInputs()
    {
        _inputs.x = Input.GetAxisRaw("Horizontal");
        _inputs.y = Input.GetAxisRaw("Vertical");

        _inputJump = Input.GetKey(KeyCode.UpArrow);

        if (_inputJump)
        {
            _timerSinceJumpPressed = 0;
        }

    }

    void HandleMovements()
    {
        var velocity = _rb.velocity;
        Vector2 wantedVelocity = new Vector2(_inputs.x * _walkSpeed, velocity.y);
        _rb.velocity = Vector2.MoveTowards(velocity, wantedVelocity, _acceleration * Time.deltaTime);
    }

    //Vector2 _point;

    void HandleGrounded()
    {
        _TimeSinceGrounded += Time.deltaTime;

        Vector2 point = transform.position + Vector3.up * _groundOffset;
        bool currentGrounded = Physics2D.OverlapCircleNonAlloc(point, _groundRadius, _collidersGround, _GroundLayer) > 0;

        if (currentGrounded == false && _isGrounded)
        {
            _TimeSinceGrounded = 0;
        }

        _isGrounded = currentGrounded;
    }


    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireSphere(_point, _groundRadius);
    //    Gizmos.color = Color.white;
    //}


    void HandleJump()
    {
        _timerNoJump -= Time.deltaTime;
        _timerSinceJumpPressed += Time.deltaTime;

        //Limite vitesse chute
        if (_rb.velocity.y < _velocityFallMin)
        {
            _rb.velocity = new Vector2(_rb.velocity.x, _velocityFallMin);
        }

        if (_isGrounded == false)
        {
            if (_rb.velocity.y < 0)
            {
                _rb.gravityScale = _gravity;
            }
            else
            {
                _rb.gravityScale = _inputJump ? _gravityUpJump : _gravity;
            }
        }
        else
        {
            _rb.gravityScale = _gravity;
        }

        if (_inputJump && (_rb.velocity.y <= 0 || _isOnSlope) && (_isGrounded || _TimeSinceGrounded < _coyoteTime) && _timerNoJump <= 0 && _timerSinceJumpPressed < _jumpInputTimer)
        {
            _rb.velocity = new Vector2(_rb.velocity.x, _jumpForce);
            _timerNoJump = _timerMinBetweenJump;
        }
    }

    void HandleSlope()
    {
        Vector3 origin = transform.position + Vector3.up * _groundOffset;
        bool slopeRight = Physics2D.RaycastNonAlloc(origin, Vector2.right, _hitResults, _slopeDetectOffset, _GroundLayer) > 0;
        bool slopeLeft = Physics2D.RaycastNonAlloc(origin, -Vector2.right, _hitResults, _slopeDetectOffset, _GroundLayer) > 0;

        _isOnSlope = (slopeRight || slopeLeft) && (slopeRight == false || slopeLeft == false);

        if (Mathf.Abs(_inputs.x) < 0.1f && (slopeLeft || slopeRight))
        {
            _collider.sharedMaterial = _physicsFriction;
        }
        else
        {
            _collider.sharedMaterial = _physicsNoFriction;
        }
    }

    void HandleCorners()
    {
        for (int i = 0; i < directions.Length; i++)
        {
            float dir = directions[i];

            if (Mathf.Abs(_inputs.x) > 0.1f && Mathf.Abs(Mathf.Sign(dir) - Mathf.Sign(_inputs.x)) < 0.001f && _isGrounded == false && _isOnSlope == false)
            {
                Vector3 position = transform.position + new Vector3(_offsetCollisionBox.x + dir * _offsetToReplace.x, _offsetCollisionBox.y, 0);
                int result = Physics2D.BoxCastNonAlloc(position, _collisionBox, 0, Vector2.zero, _hitResults, 0, _GroundLayer);

                if (result > 0)
                {
                    position = transform.position + new Vector3(_offsetCollisionBox.x + dir * _offsetToReplace.x, _offsetCollisionBox.y + _offsetToReplace.y, 0);
                    result = Physics2D.BoxCastNonAlloc(position, _collisionBox, 0, Vector2.zero, _hitResults, 0, _GroundLayer);

                    if (result == 0)
                    {
                        Debug.Log("replace");
                        transform.position += new Vector3(dir * _offsetToReplace.x, _offsetToReplace.y);

                        if (_rb.velocity.y < 0)
                        {
                            _rb.velocity = new Vector2(_rb.velocity.x, 0);
                        }
                    }
                }
            }
        }
    }
}
