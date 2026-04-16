using System;
using System.IO;
using System.Linq;
using Assets.Scripts.Constants;
using UnityEditor;
using UnityEngine;
using File = System.IO.File;

#if UNITY_EDITOR
[CustomEditor(typeof(RefreshOfficialLevelListsBehavior))]
public class RefreshOfficialLevelLists : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Refresh Official Level Lists"))
        {
            var campaignLevelsFullPath = Path.Join(Application.dataPath, "Resources", GameConstants.LevelFilePaths.CampaignLevelResourceFolderPath);
            var vsLevelsFullPath = Path.Join(Application.dataPath, "Resources", GameConstants.LevelFilePaths.VsLevelResourceFolderPath);

            if (!Directory.Exists(campaignLevelsFullPath))
                Directory.CreateDirectory(campaignLevelsFullPath);
            if (!Directory.Exists(vsLevelsFullPath))
                Directory.CreateDirectory(vsLevelsFullPath);
            var levelListFilePath = Path.Join(Application.dataPath, "Resources", $"{GameConstants.LevelFilePaths.LevelListFilePath}.txt");

            var campaignLevels = Directory.GetFiles(campaignLevelsFullPath).Where(IsLevelFile);
            var vsLevels = Directory.GetFiles(vsLevelsFullPath).Where(IsLevelFile);
            var officialLevelList = new GameConstants.OfficialLevelList
            {
                CampaignLevels = campaignLevels.Select(TrimFilePath).ToList(),
                VsLevels = vsLevels.Select(TrimFilePath).ToList()
            };

            foreach(var vsLevel in officialLevelList.VsLevels)
                Debug.Log($"VS Level: {vsLevel}");
            var officialLevelListJson = JsonUtility.ToJson(officialLevelList, false);
            File.WriteAllText(levelListFilePath, officialLevelListJson);
        }


    }

    private static bool IsLevelFile(string filepath)
    {
        return filepath.EndsWith(".txt", StringComparison.InvariantCultureIgnoreCase);
    }

    private static string TrimFilePath(string filepath)
    {
        var filename = Path.GetFileName(filepath);
        var indexOfExtension = filename.LastIndexOf('.');
        return filename.Substring(0, indexOfExtension);
    }
}
#endif