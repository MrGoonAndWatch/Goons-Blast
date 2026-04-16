using UnityEngine;

public abstract class TileWithProperties : MonoBehaviour
{
    public abstract void SetDefaultProperties();
    public abstract void LoadProperties(string propertyJson);

    protected T LoadProperties<T>(string propertyJson)
    {
        return JsonUtility.FromJson<T>(propertyJson);
    }
}
