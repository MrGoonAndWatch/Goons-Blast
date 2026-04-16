using System;
using Assets.Scripts.Constants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DestructiblePropertiesParser : TilePropertyParser
{
    [SerializeField]
    private TMP_Dropdown _blockContentsPicker;
    [SerializeField]
    private Slider _spawnProbabilitySlider;

    public override void Initialize(string propertiesJson)
    {
        PopulatePickerFromEnum(_blockContentsPicker, typeof(GameConstants.DestructableContents));

        if (string.IsNullOrEmpty(propertiesJson))
        {
            // Note: Setting this to one value then another to force the UI to refresh.
            _blockContentsPicker.value = (int) GameConstants.DestructableContents.BombsUp;
            _blockContentsPicker.value = (int)GameConstants.DestructableContents.Nothing;
            return;
        }
        
        var properties = JsonUtility.FromJson<DestructibleBlockProperties>(propertiesJson);
        _blockContentsPicker.value = (int) properties.Contents;
        _spawnProbabilitySlider.value = properties.SpawnPowerupChance;
    }

    public override string SerializeProperties()
    {
        var properties = new DestructibleBlockProperties
        {
            Contents = (GameConstants.DestructableContents) _blockContentsPicker.value,
            SpawnPowerupChance = _spawnProbabilitySlider.value
        };
        var propertiesJson = JsonUtility.ToJson(properties, false);
        return propertiesJson;
    }

    public override bool IsValid()
    {
        return _spawnProbabilitySlider.value >= 0 && _spawnProbabilitySlider.value <= 1 &&
               Enum.IsDefined(typeof(GameConstants.DestructableContents), _blockContentsPicker.value);
    }
}
