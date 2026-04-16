using System;
using Assets.Scripts.Constants;
using Photon.Realtime;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.MainMenu
{
    public class RoomListItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _roomNameText;
        [SerializeField] private TMP_Text _playerCountText;
        [SerializeField] private TMP_Text _maxPlayerText;
        [SerializeField] private TMP_Text _gameModeText;
        [SerializeField] private TMP_Text _timerSettingsText;
        [SerializeField] private TMP_Text _mapText;

        private RoomInfo _roomInfo;

        public void SetUp(RoomInfo roomInfo)
        {
            _roomNameText.text = roomInfo.Name;
            _playerCountText.text = roomInfo.PlayerCount.ToString("D");
            _maxPlayerText.text = roomInfo.MaxPlayers.ToString("D");
            _gameModeText.text = "(unknown)";
            _timerSettingsText.text = "(unknown)";
            _mapText.text = "(unknown)";

            if (roomInfo.CustomProperties.ContainsKey(GameConstants.RoomCustomProperties.MatchSettings))
            {
                var matchSettingsJson = roomInfo.CustomProperties[GameConstants.RoomCustomProperties.MatchSettings].ToString();
                var matchSettings = JsonUtility.FromJson<GameConstants.MatchSettings>(matchSettingsJson);
                if (matchSettings != null)
                {
                    _gameModeText.text = matchSettings.MatchType.ToString();
                    _timerSettingsText.text = matchSettings.TimerSeconds > 0
                        ? TimeSpan.FromSeconds(matchSettings.TimerSeconds).ToString(@"mm\:ss")
                        : "(no limit)";
                }
            }
            if (roomInfo.CustomProperties.ContainsKey(GameConstants.RoomCustomProperties.MatchMap))
                _mapText.text = roomInfo.CustomProperties[GameConstants.RoomCustomProperties.MatchMap].ToString();

            _roomInfo = roomInfo;
        }

        public void OnClick()
        {
            Launcher.Instance.JoinRoom(_roomInfo);
        }
    }
}
