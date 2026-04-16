using System;
using UnityEngine;

public class CannonBallBall : MonoBehaviour
{
    [Tooltip("The time (in seconds) that the player will be stuck in ragdoll mode when hit by a cannonball.")]
    [SerializeField]
    private float _playerRagdollDuration = 3.0f;
    [SerializeField]
    private float _moveSpeed = 15f;
    [SerializeField]
    private float _forceMultiplier = 1000.0f;
    [SerializeField]
    private float _yForce = 600.0f;
    [SerializeField]
    private Rigidbody _rigidbody;

    private Vector3 _moveDir;

    public void Init(Vector3 moveDir)
    {
        _moveDir = moveDir;
    }

    private void FixedUpdate()
    {
        _rigidbody.MovePosition(_rigidbody.position + transform.TransformDirection(_moveDir * _moveSpeed) * Time.fixedDeltaTime);
    }

    private void OnCollisionEnter(Collision c)
    {
        var player = c.collider.GetComponent<PlayerController>();
        
        if (player == null)
        {
            _rigidbody.constraints = RigidbodyConstraints.None;
            _rigidbody.useGravity = true;
            // TODO: Apply random force as well?
        }
        else
        {
            var pushDirection = new Vector3(_rigidbody.linearVelocity.x * _forceMultiplier, _yForce, _rigidbody.linearVelocity.z * _forceMultiplier);
            player.StartRagdoll(_playerRagdollDuration, pushDirection);
        }
    }
}
