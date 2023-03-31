using System.Collections.Generic;
using Assets.Scripts.Constants;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameObject _cameraContainer;

    [SerializeField] private float _horizontalMouseSensitivity, _verticalMouseSensitivity, _sprintSpeed, _walkSpeed, _jumpForce, _smoothTime;
    [SerializeField] private float _bombSpawnDistance = 1.5f;

    [SerializeField] private GameObject _playerModel;

    private float _verticalLookRotation;
    private bool _grounded;
    private Vector3 _smoothMoveVelocity;
    private Vector3 _moveAmount;

    private Rigidbody _rigidbody;
    private PhotonView _photonView;

    [SerializeField]
    private int _absoluteMaxBombs = 3;
    [SerializeField]
    private int _absoluteMaxFirepower = 1;
    public int MaxBombs = 3;
    private int _currentFirepower = 1;
    public List<Bomb> _bombs;
    private int _availableBombs;
    private bool _matchEnded;
    private bool _dead;
    private bool _remoteBombs;
    
    private PlayerControls _playerControls;
    private InputAction _camera;
    private InputAction _move;
    private InputAction _jump;
    private InputAction _bomb;
    private InputAction _openMenu;
    private InputAction _detonate;
    private InputAction _run;
    private bool _movingPlayer;
    private Vector2 _movePlayerInput;
    private bool _movingCamera;
    private Vector2 _moveCameraInput;
    private bool _isRunning;

    private void OnEnable()
    {
        if (!_photonView.IsMine) return;

        _camera = _playerControls.Default.Camera;
        _move = _playerControls.Default.Movement;
        _jump = _playerControls.Default.Jump;
        _bomb = _playerControls.Default.LayBomb;
        _openMenu = _playerControls.Default.OpenMenu;
        _detonate = _playerControls.Default.Detonate;
        _run = _playerControls.Default.Run;

        _camera.Enable();
        _move.Enable();
        _jump.Enable();
        _bomb.Enable();
        _openMenu.Enable();
        _detonate.Enable();
        _run.Enable();

        _camera.performed += OnMoveCamera;
        _camera.canceled += OnMoveCameraEnd;
        _move.performed += OnMovePlayer;
        _move.canceled += OnMovePlayerEnd;
        _jump.performed += OnJump;
        _bomb.performed += OnLayBomb;
        _openMenu.performed += OnQuitMatch;
        _detonate.performed += OnDetonate;
        _run.performed += OnRun;
        _run.canceled += OnRunEnd;
    }

    private void OnDisable()
    {
        if (!_photonView.IsMine) return;
        _camera.Disable();
        _move.Disable();
        _jump.Disable();
        _bomb.Disable();
        _openMenu.Disable();
        _detonate.Disable();
        _run.Disable();
    }

    public bool IsAlive()
    {
        return !_dead;
    }

    public string GetName()
    {
        return _photonView.Owner.NickName;
    }

    public void EndMatch()
    {
        _matchEnded = true;
    }

    private void Awake()
    {
        _playerControls = new PlayerControls();
        _rigidbody = GetComponent<Rigidbody>();
        _photonView = GetComponent<PhotonView>();
    }

    private void Start()
    {
        _bombs = new List<Bomb>();
        if (_photonView.IsMine)
            _availableBombs = MaxBombs;
        else
        {
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(_rigidbody);
        }
    }

    private void OnMoveCamera(InputAction.CallbackContext context)
    {
        if (_matchEnded) return;
        _moveCameraInput = context.ReadValue<Vector2>();
        _movingCamera = true;
    }

    private void OnMoveCameraEnd(InputAction.CallbackContext context)
    {
        _movingCamera = false;
    }

    private void OnMovePlayer(InputAction.CallbackContext context)
    {
        if (_matchEnded || _dead) return;

        _movePlayerInput = context.ReadValue<Vector2>();
        _movingPlayer = true;
    }

    private void OnMovePlayerEnd(InputAction.CallbackContext context)
    {
        StopPlayerMovement();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (_matchEnded || _dead || !_grounded) return;
        _rigidbody.AddForce(transform.up * _jumpForce);
    }

    private void OnLayBomb(InputAction.CallbackContext context)
    {
        if (_matchEnded || _dead || _availableBombs <= 0) return;
        // TODO: Start animation instead of immediately placing bomb.
        // TODO: use reference from player model's dir instead of this script's obj's forward!
        var spawnPos = transform.position + (_bombSpawnDistance * new Vector3(transform.forward.x, 0, transform.forward.z));
        var bombObject = _remoteBombs ?
            PhotonNetwork.Instantiate(GameConstants.SpawnablePrefabs.RemoteBomb, spawnPos, Quaternion.identity).GetComponent<Bomb>() :
            PhotonNetwork.Instantiate(GameConstants.SpawnablePrefabs.BasicBomb, spawnPos, Quaternion.identity).GetComponent<Bomb>();
        _bombs.Add(bombObject);
        bombObject.Initialize(this, _currentFirepower);
        _availableBombs--;
    }

    private void OnDetonate(InputAction.CallbackContext context)
    {
        if (_matchEnded || _dead) return;

        for (var i = 0; i < _bombs.Count; i++)
        {
            if (_bombs[i] is RemoteBomb)
            {
                _bombs[i].Explode();
                return;
            }
        }
    }

    private void OnRun(InputAction.CallbackContext context)
    {
        _isRunning = true;
    }

    private void OnRunEnd(InputAction.CallbackContext context)
    {
        _isRunning = false;
    }

    private static void OnQuitMatch(InputAction.CallbackContext context)
    {
        Debug.Log("Quitting Room!");
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LoadLevel((int)GameConstants.LevelIndexes.MainMenu);
    }

    private void Update()
    {
        if (_movingCamera)
            HandleMoveCamera();
        if (_movingPlayer)
            HandleMovePlayer();
    }

    private void HandleMoveCamera()
    {
        // TODO: Revisit whether we want the player to rotate with the camera.
        transform.Rotate(Vector3.up * _moveCameraInput.x * _horizontalMouseSensitivity);
        _verticalLookRotation += _moveCameraInput.y * _verticalMouseSensitivity;
        _verticalLookRotation = Mathf.Clamp(_verticalLookRotation, -90f, 90f);
        _cameraContainer.transform.localEulerAngles = Vector3.left * _verticalLookRotation;
    }

    private void HandleMovePlayer()
    {
        var moveDir = new Vector3(_movePlayerInput.x, 0, _movePlayerInput.y);
        var moveSpeed = _isRunning ? _sprintSpeed : _walkSpeed;
        _moveAmount = Vector3.SmoothDamp(_moveAmount, moveDir * moveSpeed, ref _smoothMoveVelocity, _smoothTime);
    }

    private void StopPlayerMovement()
    {
        _movingPlayer = false;
        _moveAmount = Vector3.zero;
        _smoothMoveVelocity = Vector3.zero;
    }

    public void SetGrounded(bool grounded)
    {
        _grounded = grounded;
    }
    
    private void FixedUpdate()
    {
        if (!_photonView.IsMine || _matchEnded)
            return;
        _rigidbody.MovePosition(_rigidbody.position + transform.TransformDirection(_moveAmount) * Time.fixedDeltaTime);
    }

    public void IncrementBombCount(Bomb explodedBomb)
    {
        if (_photonView.IsMine)
        {
            _availableBombs++;
            if (_availableBombs > MaxBombs) _availableBombs = MaxBombs;
            _bombs.Remove(explodedBomb);
        }
    }

    public void IncreaseMaxBombs(int amount = 1)
    {
        var newMax = MaxBombs + amount;
        if (newMax < 0) amount -= newMax;
        else if (newMax > _absoluteMaxBombs) amount -= (newMax - _absoluteMaxBombs);

        MaxBombs += amount;
        _availableBombs += amount;
        if (_availableBombs < 0) _availableBombs = 0;
    }

    public void IncreaseFirepower(int amount = 1)
    {
        _currentFirepower += amount;
        if (_currentFirepower < 0)
            _currentFirepower = 0;
        else if (_currentFirepower > _absoluteMaxFirepower)
            _currentFirepower = _absoluteMaxFirepower;
    }

    public void SetRemoteBombs()
    {
        _remoteBombs = true;
    }

    private void OnTriggerEnter(Collider c)
    {
        if (_dead) return;

        // TODO: Can we have players track their status separately then sync somehow to everyone else?
        // if (!_photonView.IsMine) return;

        HandleExplosionCollision(c);
        HandlePowerupCollision(c);
    }

    private void HandleExplosionCollision(Collider c)
    {
        var explosion = c.GetComponent<Explosion>();
        if (explosion != null)
        {
            // TODO: Handle having more than 1 hp (via item or stats or whatever).
            // TODO: Cooldown on how rapidly the player can get hit.
            _playerModel.SetActive(false);
            _dead = true;
            Debug.Log($"Player {GetName()} was killed by an explosion!");
        }
    }
    
    private void HandlePowerupCollision(Collider c)
    {
        var powerup = c.GetComponent<Powerup>();
        if (powerup != null && !powerup.AlreadyPickedUp())
            powerup.PickUp(this);
    }
}