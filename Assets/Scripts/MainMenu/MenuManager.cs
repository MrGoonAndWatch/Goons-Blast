using UnityEngine;

namespace Assets.Scripts.MainMenu
{
    public class MenuManager : MonoBehaviour
    {
        public static MenuManager Instance;

        [SerializeField] private Menu[] _menus;

        public void Awake()
        {
            if (Instance != null)
            {
                Debug.Log($"Found second instance of MenuManager, destroying old instance with instance id {Instance.GetInstanceID()}...");
                Destroy(Instance.gameObject);
            }

            Instance = this;

            // May not need to include this, put this here in case we're working on menus and forget to add one to the array or disable one.
            IdiotProofing();
        }
        
        private void IdiotProofing()
        {
            Debug.Log("IdiotProofing");
            _menus = FindObjectsByType<Menu>(FindObjectsInactive.Include);
            for(var i = 0; i < _menus.Length; i++)
                CloseMenu(_menus[i]);
            Debug.Log($"_menus.Length = {_menus.Length}");
        }

        public void OpenMenu(MenuType menuType)
        {
            for (var i = 0; i < _menus.Length; i++)
            {
                if (_menus[i].MenuType == menuType)
                    _menus[i].Open();
                else if(_menus[i].Opened)
                    CloseMenu(_menus[i]);
            }
        }

        public void OpenMenu(Menu menu)
        {
            for (var i = 0; i < _menus.Length; i++)
                if (_menus[i].Opened)
                    CloseMenu(_menus[i]);

            menu.Open();
        }

        public void CloseMenu(Menu menu)
        {
            menu.Close();
        }
    }
}
