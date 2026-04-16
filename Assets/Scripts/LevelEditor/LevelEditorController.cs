using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Constants;
using Photon.Pun;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LevelEditorController : MonoBehaviour
{
    [SerializeField]
    private Transform _cursor;
    [SerializeField]
    private int _tileSize = 1;
    [SerializeField]
    private TMP_Text _currentBlockDisplayText;
    [SerializeField]
    private Image _currentBlockDisplayImage;
    [SerializeField]
    private TilePrefabLookup _lookup;
    [SerializeField]
    private GameObject _mainEditorCanvas;
    [SerializeField]
    private GameObject _minimizedEditorCanvas;
    [SerializeField]
    private PlayerControls _editorControls;
    [SerializeField]
    private GameObject _cameraContainer;
    [SerializeField]
    private GameObject _camera;
    [SerializeField]
    private float _cameraMaxDistanceFromCursor = 8.0f;
    [SerializeField]
    private float _cameraMoveSpeed = 0.5f;
    [SerializeField]
    private GameObject _tilePropertiesUi;
    [SerializeField]
    private GameObject _tilePropertiesCustomContainer;
    [SerializeField]
    private PropertyUiLookup _propertyUiLookup;
    [SerializeField]
    private TileDisplayImageLookup _tileDisplayImageLookup;

    private const float MouseMoveCursorDeadzone = 5;
    private const float ControllerCursorMoveDeadzone = 0.75f;
    private const float HoldCursorMoveCooldown = 0.01f;
    private const float ButtonCursorMoveInitCooldown = 0.5f;
    private const float HorizontalMouseSensitivity = 1.0f;
    private const float VerticalMouseSensitivity = 1.0f;
    private float _verticalLookRotation;

    private InputAction _rotateCamera;
    private InputAction _mouseRotateCameraClick;
    private InputAction _mouseRotateCameraDrag;
    private InputAction _moveXZ;
    private InputAction _moveUp;
    private InputAction _moveDown;
    private InputAction _tileNextForwards;
    private InputAction _tileBack;
    private InputAction _placeTile;
    private InputAction _saveLevel;
    private InputAction _minimizeUi;
    private InputAction _propertiesMenu;
    private InputAction _disableMouseMove;
    private InputAction _mouseWheelScroll;
    private InputAction _resetCamera;

    private bool _disableInputs;
    private bool _propertyMenuOpened;
    private bool _movingHorizontally;
    private bool _movingVertically;
    private bool _placingTiles;
    private bool _mouseMoveDisabled;
    private float _cursorXZMoveCooldown;
    private float _cursorYMoveCooldown;

    private LevelData _levelData;
    private List<LevelEditorTile> _generatedTiles;

    private int _currentX;
    private int _currentY;
    private int _currentZ;
    private TileType _currentBlockType;
    private bool _minimized = true;

    private Vector2 _currentCursorMoveInput;
    private bool _holdingCursorUp;
    private bool _holdingCursorDown;
    private Vector2 _currentMouseDrag;
    private Vector2 _rotateCameraInput;
    private bool _rotatingCamera;
    private bool _holdingRotateMouseButton;

    private TilePropertyParser _currentPropertyParser;

    void Awake()
    {
        _editorControls = new PlayerControls();
    }

    void OnEnable()
    {
        _moveXZ = _editorControls.Editor.MoveXZ;
        _moveUp = _editorControls.Editor.MoveUp;
        _moveDown = _editorControls.Editor.MoveDown;
        _tileNextForwards = _editorControls.Editor.NextTile;
        _tileBack = _editorControls.Editor.PreviousTile;
        _placeTile = _editorControls.Editor.PlaceTile;
        _saveLevel = _editorControls.Editor.SaveMenu;
        _minimizeUi = _editorControls.Editor.HelpMenu;
        _rotateCamera = _editorControls.Editor.RotateCamera;
        _mouseRotateCameraClick = _editorControls.Editor.MouseRotateCameraClick;
        _mouseRotateCameraDrag = _editorControls.Editor.MouseRotateCameraDrag;
        _propertiesMenu = _editorControls.Editor.PropertiesMenu;
        _disableMouseMove = _editorControls.Editor.DisableMouseMove;
        _mouseWheelScroll = _editorControls.Editor.MouseWheelMove;
        _resetCamera = _editorControls.Editor.ResetCamera;

        _moveXZ.Enable();
        _moveUp.Enable();
        _moveDown.Enable();
        _tileNextForwards.Enable();
        _tileBack.Enable();
        _placeTile.Enable();
        _saveLevel.Enable();
        _minimizeUi.Enable();
        _rotateCamera.Enable();
        _mouseRotateCameraClick.Enable();
        _mouseRotateCameraDrag.Enable();
        _propertiesMenu.Enable();
        _disableMouseMove.Enable();
        _mouseWheelScroll.Enable();
        _resetCamera.Enable();

        _moveXZ.performed += OnMoveCursor;
        _moveXZ.canceled += OnStopMovingCursor;
        _moveUp.performed += OnMoveCursorUp;
        _moveUp.canceled += OnMoveCursorUpEnd;
        _moveDown.performed += OnMoveCursorDown;
        _moveDown.canceled += OnMoveCursorDownEnd;
        _tileNextForwards.performed += OnTileNext;
        _tileBack.performed += OnTileBack;
        _placeTile.performed += OnPlaceTile;
        _placeTile.canceled += OnEndPlaceTile;
        _saveLevel.performed += OnSaveLevel;
        _minimizeUi.performed += OnMinimize;
        _rotateCamera.performed += OnRotateCamera;
        _rotateCamera.canceled += OnRotateCameraEnd;
        _mouseRotateCameraClick.performed += OnMouseCameraClickStart;
        _mouseRotateCameraClick.canceled += OnMouseCameraClickEnd;
        _mouseRotateCameraDrag.performed += OnRotateCameraMouse;
        _mouseRotateCameraDrag.canceled += OnMouseMoveEnd;
        _propertiesMenu.performed += OnPropertiesMenu;
        _disableMouseMove.performed += OnDisableMouseMove;
        _mouseWheelScroll.performed += OnMouseWheelScrolled;
        _resetCamera.performed += OnResetCamera;
    }

    void OnDisable()
    {
        _moveXZ.Disable();
        _moveUp.Disable();
        _moveDown.Disable();
        _tileNextForwards.Disable();
        _tileBack.Disable();
        _placeTile.Disable();
        _saveLevel.Disable();
        _minimizeUi.Disable();
        _rotateCamera.Disable();
        _mouseRotateCameraClick.Disable();
        _mouseRotateCameraDrag.Disable();
        _propertiesMenu.Disable();
        _disableMouseMove.Disable();
        _mouseWheelScroll.Disable();
        _resetCamera.Disable();
    }

    private void Start()
    {
        _generatedTiles = new List<LevelEditorTile>();
        _levelData = new LevelData();
        LoadSelectedMap();
        UpdateBlockTypeDisplay();
        UpdateCursor();
    }

    private void LoadSelectedMap()
    {
        if (RoomManager.Instance == null) return;

        var selectedMap = RoomManager.GetMap();
        if (string.IsNullOrEmpty(selectedMap) || !File.Exists(selectedMap)) return;

        LevelData loadedLevelData;
        try
        {
            var levelDataJson = File.ReadAllText(selectedMap);
            loadedLevelData = JsonUtility.FromJson<LevelData>(levelDataJson);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load level '{selectedMap}', got exception: {e}");
            return;
        }

        _levelData = loadedLevelData;
        GenerateTilesFromLevelData(loadedLevelData);
    }

    private void GenerateTilesFromLevelData(LevelData levelData)
    {
        for (var i = 0; i < levelData.Tiles.Count; i++)
        {
            var tile = levelData.Tiles[i];
            AddTileOnScreen(tile);
        }
    }

    private void Update()
    {
        if (_cursorXZMoveCooldown > 0)
            _cursorXZMoveCooldown -= Time.deltaTime;
        if (_cursorYMoveCooldown > 0)
            _cursorYMoveCooldown -= Time.deltaTime;

        if(_rotatingCamera)
            HandleRotateCamera();
        FollowCursorWithCamera();
        HandleBlockPlacement();
        MoveCursorByButtonOrStick(false);
        MoveCursorByMouse();
        MoveCursorUpOrDown();
    }

    public LevelData GetLevelData()
    {
        return _levelData;
    }

    public void EnableInputs()
    {
        _disableInputs = false;
    }

    private void HandleRotateCamera()
    {
        if (_disableInputs) return;

        var config = RoomManager.GetConfigSettings();
        var invertXAxis = config?.InvertXAxisLook ?? false ? -1.0f : 1.0f;
        var invertYAxis = config?.InvertYAxisLook ?? false ? -1.0f : 1.0f;

        _cameraContainer.transform.Rotate(Vector3.up * _rotateCameraInput.x * HorizontalMouseSensitivity * invertXAxis);
        _verticalLookRotation += _rotateCameraInput.y * VerticalMouseSensitivity;
        _verticalLookRotation = Mathf.Clamp(_verticalLookRotation, -90f, 90f);
        _camera.transform.localEulerAngles = Vector3.left * _verticalLookRotation * invertYAxis;
    }

    private void ResetCameraRotation()
    {
        _verticalLookRotation = 0;
        _cameraContainer.transform.rotation = Quaternion.identity;
        _camera.transform.localEulerAngles = Vector3.left * _verticalLookRotation;
    }

    private void FollowCursorWithCamera()
    {
        var distance = (_camera.transform.position - _cursor.position).magnitude;
        if (distance <= _cameraMaxDistanceFromCursor)
            return;

        _cameraContainer.transform.position = Vector3.MoveTowards(_cameraContainer.transform.position, _cursor.position, _cameraMoveSpeed);
    }

    private void HandleBlockPlacement()
    {
        if (!_placingTiles || _disableInputs)
            return;
        SetTile();
    }

    private void OnMouseCameraClickStart(InputAction.CallbackContext context)
    {
        _holdingRotateMouseButton = true;
    }

    private void OnMouseCameraClickEnd(InputAction.CallbackContext context)
    {
        _holdingRotateMouseButton = false;
    }

    private void OnRotateCameraMouse(InputAction.CallbackContext context)
    {
        _currentMouseDrag = context.ReadValue<Vector2>();
        if (_holdingRotateMouseButton) OnRotateCamera(context);
    }

    private void OnRotateCamera(InputAction.CallbackContext context)
    {
        _rotateCameraInput = context.ReadValue<Vector2>();
        _rotatingCamera = true;
    }

    private void OnMouseMoveEnd(InputAction.CallbackContext context)
    {
        _rotatingCamera = false;
    }

    private void OnRotateCameraEnd(InputAction.CallbackContext context)
    {
        _rotatingCamera = false;
    }

    private void OnPropertiesMenu(InputAction.CallbackContext context)
    {
        if (_disableInputs)
        {
            if (_propertyMenuOpened)
                OnClosePropertyMenu();
            return;
        }

        Debug.Log($"ValueType = {context.valueType}");
        var currentTile = GetCurrentTile();
        if (currentTile == null)
            return;
        var customPropertiesPrefab = _propertyUiLookup.GetPropertyUiPrefab(currentTile.Type);
        if (customPropertiesPrefab == null) return;

        _disableInputs = true;
        _propertyMenuOpened = true;
        BuildPropertyUi(customPropertiesPrefab, currentTile);
        _tilePropertiesUi.SetActive(true);
    }

    private void OnDisableMouseMove(InputAction.CallbackContext context)
    {
        if (_disableInputs) return;

        _mouseMoveDisabled = !_mouseMoveDisabled;
    }

    private void OnMouseWheelScrolled(InputAction.CallbackContext context)
    {
        if (_disableInputs) return;

        var scrollValue = context.ReadValue<Vector2>();

        if(scrollValue.y > 0)
            MoveCursorUp();
        else if(scrollValue.y < 0)
            MoveCursorDown();
    }

    private void OnResetCamera(InputAction.CallbackContext context)
    {
        if (_disableInputs) return;
        ResetCameraRotation();
    }

    public void OnClosePropertyMenu()
    {
        // TODO: Maybe add some validation message here?
        if (!_currentPropertyParser.IsValid()) return;
        var currentTile = GetCurrentTile();
        currentTile.Properties = _currentPropertyParser.SerializeProperties();
        _tilePropertiesUi.SetActive(false);
        _disableInputs = false;
        _propertyMenuOpened = false;
    }

    private void BuildPropertyUi(GameObject customPropertiesPrefab, TileData currentTile)
    {
        for(var i = 0; i < _tilePropertiesCustomContainer.transform.childCount; i++)
            Destroy(_tilePropertiesCustomContainer.transform.GetChild(i).gameObject);
        var generatedUi = Instantiate(customPropertiesPrefab, _tilePropertiesCustomContainer.transform);
        _currentPropertyParser = generatedUi.GetComponent<TilePropertyParser>();
        if (_currentPropertyParser == null)
        {
            Debug.LogError("Error! Current tile's property UI prefab is missing TilePropertyParser in parent node!");
            OnClosePropertyMenu();
        }
        else
        {
            _currentPropertyParser.Initialize(currentTile.Properties);
        }
    }

    private void MoveCursorForwards()
    {
        _movingVertically = true;
        _currentZ += _tileSize;
        UpdateCursor();
    }

    private void MoveCursorBackwards()
    {
        _movingVertically = true;
        _currentZ -= _tileSize;
        UpdateCursor();
    }

    private void MoveCursorLeft()
    {
        _movingHorizontally = true;
        _currentX -= _tileSize;
        UpdateCursor();
    }
    private void MoveCursorRight()
    {
        _movingHorizontally = true;
        _currentX += _tileSize;
        UpdateCursor();
    }

    private void MoveCursorUp()
    {
        _currentY += _tileSize;
        UpdateCursor();
    }

    private void MoveCursorDown()
    {
        _currentY -= _tileSize;
        UpdateCursor();
    }

    private void UpdateCursor()
    {
        _cursor.position = new Vector3(_currentX, _currentY, _currentZ);
    }

    private void ToggleMinimize()
    {
        if (_minimized)
        {
            _mainEditorCanvas.SetActive(true);
            _minimizedEditorCanvas.SetActive(false);
        }
        else
        {
            _mainEditorCanvas.SetActive(false);
            _minimizedEditorCanvas.SetActive(true);
        }

        _minimized = !_minimized;
    }

    private void SetTile()
    {
        if (_currentBlockType == TileType.None)
            EraseCurrentTile();
        else
            AddCurrentTile();
    }

    private void EraseCurrentTile()
    {
        var tileToRemove = GetCurrentTile();
        if (tileToRemove == null)
            return;
        _levelData.Tiles.Remove(tileToRemove);

        RemoveCurrentTile();
    }

    private void AddCurrentTile()
    {
        var currentTile = GetCurrentTile();
        if (currentTile != null && currentTile.Type == _currentBlockType)
            return;

        if (currentTile == null)
        {
            currentTile = new TileData {Type = _currentBlockType, X = _currentX, Y = _currentY, Z = _currentZ};
            _levelData.Tiles.Add(currentTile);
        }
        else
        {
            currentTile.Type = _currentBlockType;
            RemoveCurrentTile();
        }

        AddTileOnScreen(currentTile);
    }

    private void AddTileOnScreen(TileData tile)
    {
        var currentPos = new Vector3(tile.X, tile.Y, tile.Z);
        var newObj = Instantiate(_lookup.GetPrefab(tile.Type), currentPos, Quaternion.identity).GameObject();
        _generatedTiles.Add(new LevelEditorTile
        {
            GeneratedObject = newObj,
            X = tile.X,
            Y = tile.Y,
            Z = tile.Z
        });
    }

    private void RemoveCurrentTile()
    {
        var tile = _generatedTiles.FirstOrDefault(t => t.X == _currentX && t.Y == _currentY && t.Z == _currentZ);
        if (tile == null)
            return;
        Destroy(tile.GeneratedObject);
        _generatedTiles.Remove(tile);
    }

    private TileData GetCurrentTile()
    {
        return _levelData.Tiles.FirstOrDefault(d => d.X == _currentX && d.Y == _currentY && d.Z == _currentZ);
    }
    
    private void CycleBlockBackwards()
    {
        var nextBlockIndex = (int)_currentBlockType - 1;
        if (nextBlockIndex < 0)
        {
            var blockTypes = Enum.GetValues(typeof(TileType));
            nextBlockIndex = (int)blockTypes.GetValue(blockTypes.Length - 1);
        }
        _currentBlockType = (TileType)nextBlockIndex;
        UpdateBlockTypeDisplay();
    }

    private void CycleBlockForward()
    {
        var nextBlockIndex = (int)_currentBlockType + 1;
        var blockTypes = Enum.GetValues(typeof(TileType));
        if (nextBlockIndex >= blockTypes.Length)
            nextBlockIndex = 0;
        _currentBlockType = (TileType)nextBlockIndex;
        UpdateBlockTypeDisplay();
    }

    // TODO: Update visual indicator to player when this happens!
    private void UpdateBlockTypeDisplay()
    {
        _currentBlockDisplayText.text = $"Current Tile: {_currentBlockType}";
        var newSprite = _tileDisplayImageLookup.GetBlockDisplayImage(_currentBlockType);
        _currentBlockDisplayImage.sprite = newSprite;
    }

    private void MoveCursorUpOrDown()
    {
        if (_disableInputs || _cursorYMoveCooldown > 0)
            return;

        var moved = false;
        if (_holdingCursorUp)
        {
            MoveCursorUp();
            moved = true;
        }
        if (_holdingCursorDown)
        {
            MoveCursorDown();
            moved = true;
        }

        if(moved)
            _cursorYMoveCooldown = HoldCursorMoveCooldown;
    }

    private void MoveCursorByMouse()
    {
        if (_disableInputs || _mouseMoveDisabled || _cursorXZMoveCooldown > 0) return;

        var moved = false;
        if (_currentMouseDrag.x >= MouseMoveCursorDeadzone)
        {
            MoveCursorRight();
            moved = true;
        }
        else if (_currentMouseDrag.x <= -MouseMoveCursorDeadzone)
        {
            MoveCursorLeft();
            moved = true;
        }

        if (_currentMouseDrag.y >= MouseMoveCursorDeadzone)
        {
            MoveCursorForwards();
            moved = true;
        }
        else if (_currentMouseDrag.y <= -MouseMoveCursorDeadzone)
        {
            MoveCursorBackwards();
            moved = true;
        }

        if (moved)
            _cursorXZMoveCooldown = HoldCursorMoveCooldown;
    }

    private void MoveCursorByButtonOrStick(bool initInput)
    {
        if (_disableInputs || (!initInput && _cursorXZMoveCooldown > 0)) return;

        var moved = false;
        // TODO: Would be nice to use proper deadzone processors but they don't seem to work (everything comes through).
        if (_currentCursorMoveInput.x >= ControllerCursorMoveDeadzone && (!initInput || !_movingHorizontally))
        {
            MoveCursorRight();
            moved = true;
        }
        else if (_currentCursorMoveInput.x <= -ControllerCursorMoveDeadzone && (!initInput || !_movingHorizontally))
        {
            MoveCursorLeft();
            moved = true;
        }

        if (_currentCursorMoveInput.y >= ControllerCursorMoveDeadzone && (!initInput || !_movingVertically))
        {
            MoveCursorForwards();
            moved = true;
        }
        else if (_currentCursorMoveInput.y <= -ControllerCursorMoveDeadzone && (!initInput || !_movingVertically))
        {
            MoveCursorBackwards();
            moved = true;
        }

        if (moved)
            _cursorXZMoveCooldown = initInput ? ButtonCursorMoveInitCooldown : HoldCursorMoveCooldown;
    }
    
    private void OnMoveCursor(InputAction.CallbackContext context)
    {
        _currentCursorMoveInput = context.ReadValue<Vector2>();
        MoveCursorByButtonOrStick(true);
    }

    private void OnStopMovingCursor(InputAction.CallbackContext context)
    {
        _currentCursorMoveInput = context.ReadValue<Vector2>();
        _movingHorizontally = false;
        _movingVertically = false;
    }

    private void OnMoveCursorUp(InputAction.CallbackContext context)
    {
        _holdingCursorUp = true;
        if (_disableInputs)
            return;
        MoveCursorUp();
        _cursorYMoveCooldown = ButtonCursorMoveInitCooldown;
    }

    private void OnMoveCursorUpEnd(InputAction.CallbackContext context)
    {
        _holdingCursorUp = false;
    }

    private void OnMoveCursorDown(InputAction.CallbackContext context)
    {
        _holdingCursorDown = true;
        if (_disableInputs)
            return;
        MoveCursorDown();
        _cursorYMoveCooldown = ButtonCursorMoveInitCooldown;
    }

    private void OnMoveCursorDownEnd(InputAction.CallbackContext context)
    {
        _holdingCursorDown = false;
    }

    private void OnTileNext(InputAction.CallbackContext context)
    {
        if (_disableInputs)
            return;
        CycleBlockForward();
    }

    private void OnTileBack(InputAction.CallbackContext context)
    {
        if (_disableInputs)
            return;
        CycleBlockBackwards();
    }

    private void OnPlaceTile(InputAction.CallbackContext context)
    {
        _placingTiles = true;
    }

    private void OnEndPlaceTile(InputAction.CallbackContext context)
    {
        _placingTiles = false;
    }

    private void OnSaveLevel(InputAction.CallbackContext context)
    {
        if (!_disableInputs) _disableInputs = true;
    }

    private void OnMinimize(InputAction.CallbackContext context)
    {
        if (_disableInputs)
            return;
        ToggleMinimize();
    }

    public void OnExitLevelEditor()
    {
        PhotonNetwork.LoadLevel((int)GameConstants.LevelIndexes.MainMenu);
    }
}
