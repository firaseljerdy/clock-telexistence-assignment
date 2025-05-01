using UnityEngine;
using UniRx;
using Zenject;
using System;
using System.Collections.Generic;
using System.Linq;
using ClockApp.UI;

namespace ClockApp.Controllers
{
    [System.Serializable]
    public struct ScreenPrefabMapping
    {
        public TabMode Mode;
        public GameObject ScreenPrefab;
    }

    public enum TabMode
    {
        Clock,
        Timer,
        Stopwatch
    }

    public class TabController : MonoBehaviour
    {
        // --- Injected Dependencies ---
        [Inject] private TabStyleSettings _styleSettings;
        [Inject] private DiContainer _container;

        [Header("Tab Setup")]
        [SerializeField] private GameObject _tabElementPrefab;
        [SerializeField] private Transform _tabContainer;

        [Header("Screen Setup")]
        [SerializeField] private Transform _screenContainer;
        [SerializeField] private List<ScreenPrefabMapping> _screenMappings;

        private readonly ReactiveProperty<TabMode> _currentMode = new ReactiveProperty<TabMode>(TabMode.Clock);
        public IReadOnlyReactiveProperty<TabMode> CurrentMode => _currentMode;

        private GameObject _currentScreenInstance; // Hold the currently active screen instance

        void Start()
        {
            // --- Validation ---
            if (_tabElementPrefab == null) Debug.LogError("TabElementPrefab not assigned", this);
            if (_tabContainer == null) Debug.LogError("TabContainer not assigned", this);
            if (_screenContainer == null) Debug.LogError("ScreenContainer not assigned", this);
            if (_screenMappings == null || _screenMappings.Count == 0) Debug.LogError("ScreenMappings not configure", this);

            // --- Instantiation ---
            InstantiateTabs();

            // --- State Change Subscription ---
            _currentMode
                .ObserveOnMainThread()
                .Subscribe(UpdateActiveScreen)
                .AddTo(this);

            // --- Initialize UI ---
            UpdateActiveScreen(_currentMode.Value);
        }

        private void InstantiateTabs()
        {
            foreach (Transform child in _tabContainer) Destroy(child.gameObject);
            //Debug.Log($"Instantiating tabs into container: {_tabContainer.name}");
            foreach (TabMode mode in Enum.GetValues(typeof(TabMode)))
            {
                Debug.Log($"Attempting to instantiate tab for mode: {mode}");
                GameObject tabGo = null;
                try
                {
                    tabGo = _container.InstantiatePrefab(_tabElementPrefab, _tabContainer);
                    //Debug.Log($" Instantiated Tab: {tabGo?.name ?? "NULL"}", tabGo);
                }
                catch (Exception ex) { Debug.LogError($"Error instantiating tab prefab for mode {mode}: {ex}"); continue; }

                var tabElement = tabGo?.GetComponent<TabElement>();
                if (tabElement != null) { try { tabElement.Initialize(mode); Debug.Log($"  Successfully initialized TabElement for mode: {mode}"); } catch (Exception ex) { Debug.LogError($"Error initializing TabElement for mode {mode}: {ex}", tabGo); } }
                else if (tabGo != null) { Debug.LogError($"Instantiated TabElementPrefab for mode {mode} is missing the TabElement component!", tabGo); }
            }
            //Debug.Log("Finished instantiating tabs");
        }

        // Called by TabElement via injected reference
        public void SwitchTab(TabMode mode)
        {
            //Debug.Log($"SwitchTab called with mode: {mode}. Current mode is: {_currentMode.Value}");
            if (_currentMode.Value == mode) return;
            _currentMode.Value = mode;
        }

        // Responsible for creating/destroying screen prefabs
        private void UpdateActiveScreen(TabMode activeMode)
        {
            //Debug.Log($"UpdateActiveScreen executing for mode: {activeMode}");
            // Destroy the previous screen instance
            if (_currentScreenInstance != null)
            {
                Destroy(_currentScreenInstance);
                _currentScreenInstance = null;
            }

            // Find the prefab for the new active mode
            ScreenPrefabMapping mapping = _screenMappings.FirstOrDefault(m => m.Mode == activeMode);
            if (mapping.ScreenPrefab == null)
            {
                Debug.LogError($"No ScreenPrefab mapped for TabMode: {activeMode}!", this);
                return;
            }

            // Instantiate the new screen prefab using Zenject
            try
            {
                // Instantiate into the screen container.
                _currentScreenInstance = _container.InstantiatePrefab(mapping.ScreenPrefab, _screenContainer);
                //Debug.Log($"Instantiated screen: {mapping.ScreenPrefab.name} for mode {activeMode}", _currentScreenInstance);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to instantiate ScreenPrefab for mode {activeMode}: {ex}", this);
                _currentScreenInstance = null;
            }
        }
    }
}