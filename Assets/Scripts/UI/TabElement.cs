using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using Zenject;
using System;
using ClockApp.Controllers; // For TabMode

namespace ClockApp.UI
{
    /// <summary>
    /// Represents a single tab element in the navigation.
    /// </summary>
    public class TabElement : MonoBehaviour
    {
        // --- Injected Dependencies ---
        [Inject] private TabController _tabController;
        [Inject] private TabStyleSettings _styleSettings;

        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _textElement;

        private TabMode _myMode;

        // Called by TabController after instantiation to assign the specific mode. Only sets data, does not set up subscriptions
        public void Initialize(TabMode mode)
        {
            _myMode = mode;
            if (_textElement != null)
            {
                _textElement.text = mode.ToString(); 
            }
        }

        void Start()
        {
            Debug.Log($"TabElement Start() called for mode: {_myMode}", this);

            if (_tabController == null || _styleSettings == null)
            {
                Debug.LogError($"TabElement for mode {_myMode}: Dependencies not injected correctly", this);
                return;
            }

            // --- Button Click Subscription ---
            //Debug.Log($"TabElement for mode {_myMode}: Checking Button reference: {_button == null}", this);
            _button?.OnClickAsObservable()
                .Subscribe(_ => _tabController.SwitchTab(_myMode)) // Notify controller
                .AddTo(this);

            // Current mode to update visuals 
            _tabController.CurrentMode
                .ObserveOnMainThread()
                .Subscribe(mode => UpdateVisuals(mode == _myMode))
                .AddTo(this);

            // Ensure the initial visual state is correct based on the controller's starting mode
            UpdateVisuals(_tabController.CurrentMode.Value == _myMode);
        }

        private void UpdateVisuals(bool isSelected)
        {
            if (_textElement == null) return;

            _textElement.fontSize = isSelected ? _styleSettings.SelectedFontSize : _styleSettings.DeselectedFontSize;
            _textElement.color = isSelected ? _styleSettings.SelectedColor : _styleSettings.DeselectedColor;

            if (_button != null) _button.interactable = !isSelected;
        }

        void OnValidate()
        {
            if (_textElement == null) _textElement = GetComponentInChildren<TMP_Text>();
            if (_button == null) _button = GetComponent<Button>();
        }
    }
}