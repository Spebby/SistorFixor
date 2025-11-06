using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;


namespace Fixor {
    public class Timer : MonoBehaviour {
        float _maxTime;
        float _timer;
        TextMeshProUGUI _text;
        
        public Action Notify;
        
        void Start() {
            _maxTime = ServiceLocator.LevelData.timerLength;
            _text = gameObject.GetComponentInChildren<TextMeshProUGUI>();
            if (_maxTime <= 0) return;

            Drawer drawer = GetComponent<Drawer>();
            if (ServiceLocator.LevelData.shouldAnimate) {
                drawer?.ToggleDrawer(); // open if there is a drawer
            } else {
                drawer?.SetOpen();
            }
            StartCoroutine(Countdown());
        }

        IEnumerator Countdown() {
            _timer = _maxTime;
            while (_timer > 0) {
                _text.text = _timer.ToString("F2", CultureInfo.InvariantCulture);
                yield return new WaitForEndOfFrame();
                _timer -= Time.deltaTime;
            }

            _text.text = "00.00";
            Notify?.Invoke();
        }
    }
}