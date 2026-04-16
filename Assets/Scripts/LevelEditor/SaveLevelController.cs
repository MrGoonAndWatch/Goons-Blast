using System;
using System.IO;
using Assets.Scripts.Constants;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using SaveLevelResult = Assets.Scripts.Constants.GameConstants.SaveLevelResult;
using SaveWindowState = Assets.Scripts.Constants.GameConstants.SaveWindowState;

public class SaveLevelController : MonoBehaviour
{
    private PlayerControls _editorControls;
    private LevelEditorController _levelEditor;

    [SerializeField]
    private GameObject _saveUi;
    [SerializeField]
    private GameObject _paramsWindow;
    [SerializeField]
    private GameObject _errorWindow;
    [SerializeField]
    private GameObject _overwriteWindow;
    [SerializeField]
    private GameObject _successWindow;
    [SerializeField]
    private TMP_InputField _levelNameInput;
    [SerializeField]
    private TMP_Text _saveErrorMessageLabel;
    [SerializeField]
    private TMP_Text _overwriteMessageLabel;
    [SerializeField]
    private TMP_Text _successMessageLabel;

    private InputAction _toggleSaveMenu;
    private InputAction _cancel;
    private InputAction _confirm;

    private bool _saveWindowOpen;
    private string _previousSaveFile;
    private SaveWindowState _state;

    #region Setup & Teardown
    void Awake()
    {
        _state = SaveWindowState.ParameterWindow;
        _editorControls = new PlayerControls();
        _levelEditor = GetComponent<LevelEditorController>();
        if(_levelEditor == null)
            Debug.LogError("Couldn't find LevelEditorController!!!");
    }

    void OnEnable()
    {
        _toggleSaveMenu = _editorControls.Editor.SaveMenu;
        _confirm = _editorControls.EditorSaveMenu.Confirm;
        _cancel = _editorControls.EditorSaveMenu.Cancel;

        _toggleSaveMenu.Enable();
        _confirm.Enable();
        _cancel.Enable();

        _toggleSaveMenu.performed += OnToggleSaveMenu;
        _confirm.performed += OnConfirmButton;
        _cancel.performed += OnCancelButton;
    }

    void OnDisable()
    {
        _toggleSaveMenu.Disable();
        _confirm.Disable();
        _cancel.Disable();
    }
    #endregion

    #region Callbacks
    private void OnToggleSaveMenu(InputAction.CallbackContext context)
    {
        ToggleSaveMenu();
    }

    private void OnConfirmButton(InputAction.CallbackContext context)
    {
        if (!_saveWindowOpen) return;

        switch (_state)
        {
            case SaveWindowState.ParameterWindow:
                SaveLevel(false);
                break;
            case SaveWindowState.ErrorWindow:
                ReturnToParamsWindow();
                break;
            case SaveWindowState.OverwriteWindow:
                SaveLevel(true);
                break;
            case SaveWindowState.SuccessWindow:
                CloseSaveMenu();
                break;
        }
    }

    private void OnCancelButton(InputAction.CallbackContext context)
    {
        if (!_saveWindowOpen) return;

        switch (_state)
        {
            case SaveWindowState.ParameterWindow:
            case SaveWindowState.ErrorWindow:
                CloseSaveMenu();
                break;
            case SaveWindowState.OverwriteWindow:
                ReturnToParamsWindow();
                break;
        }
    }
    #endregion

    public void ToggleSaveMenu()
    {
        _saveWindowOpen = !_saveWindowOpen;
        if(_saveWindowOpen) _saveUi.SetActive(true);
        else CloseSaveMenu();
    }

    public void CloseSaveMenu()
    {
        ReturnToParamsWindow();
        _saveUi.SetActive(false);
        _saveWindowOpen = false;
        _levelEditor.EnableInputs();
    }

    public void OnSaveLevel(bool overwrite)
    {
        _overwriteWindow.SetActive(false);

        if (!overwrite && IsSameFile()) overwrite = true;
        var result = SaveLevel(overwrite);
        _paramsWindow.SetActive(false);
        switch (result)
        {
            case SaveLevelResult.ConfirmOverwrite:
                _overwriteMessageLabel.text = $"WARNING: Level with name '{_levelNameInput.text}' already exists. Overwrite?";
                _overwriteWindow.SetActive(true);
                _state = SaveWindowState.OverwriteWindow;
                break;
            case SaveLevelResult.Failure:
                _errorWindow.SetActive(true);
                _state = SaveWindowState.ErrorWindow;
                break;
            case SaveLevelResult.Success:
                _previousSaveFile = _levelNameInput.text;
                _successMessageLabel.text = $"Successfully saved level '{_levelNameInput.text}'!";
                _successWindow.SetActive(true);
                _state = SaveWindowState.SuccessWindow;
                break;
            default:
                Debug.LogError($"Got unexpected SaveFile result '{result}'.");
                CloseSaveMenu();
                break;
        }
    }

    private bool IsSameFile()
    {
        return _levelNameInput.text.Equals(_previousSaveFile);
    }

    public void ReturnToParamsWindow()
    {
        _errorWindow.SetActive(false);
        _overwriteWindow.SetActive(false);
        _successWindow.SetActive(false);
        _paramsWindow.SetActive(true);
        _state = SaveWindowState.ParameterWindow;
    }

    private SaveLevelResult SaveLevel(bool overwrite)
    {
        if (string.IsNullOrEmpty(_levelNameInput.text))
        {
            _saveErrorMessageLabel.text = "You must specify a name for your level.";
            return SaveLevelResult.Failure;
        }

        try
        {
            return SaveLevelData(overwrite);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save level due to Exception file saving file: {e}");
            _saveErrorMessageLabel.text = $"Failed to save level, encountered error '{e.Message}'";
            return SaveLevelResult.Failure;
        }
    }

    private SaveLevelResult SaveLevelData(bool overwrite)
    {
        var dir = GetCustomLevelDirPath();
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        var filePath = GetLevelFilepath();
        
        if (!overwrite && File.Exists(filePath))
            return SaveLevelResult.ConfirmOverwrite;
        
        var levelDataJson = JsonUtility.ToJson(_levelEditor.GetLevelData(), false);
        File.WriteAllText(filePath, levelDataJson);
        return SaveLevelResult.Success;
    }

    private static string GetCustomLevelDirPath()
    {
        // TODO: Contextually select Versus folder or Campaign folder based on file type.
        return Path.Combine(Application.persistentDataPath, GameConstants.LevelFilePaths.CustomLevelFolder, GameConstants.LevelFilePaths.VersusFolder);
    }

    private string GetLevelFilepath()
    {
        var dir = GetCustomLevelDirPath();
        var filePath = Path.Combine(dir, $"{_levelNameInput.text}.level");
        return filePath;
    }
}
