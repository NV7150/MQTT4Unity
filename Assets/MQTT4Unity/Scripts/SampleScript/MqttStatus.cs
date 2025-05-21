using System;
using MQTTnet.Diagnostics;
using TMPro;
using UnityEngine;

namespace MQTT4Unity.SampleScript {
    public class MqttStatus : MonoBehaviour {
        [SerializeField] private MqttComManager mqttComManager;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI statusLog;
        
        // Start is called before the first frame update
        void Start() {
            mqttComManager.OnLog += msg => statusLog.text += msg + Environment.NewLine;
        }

        // Update is called once per frame
        void Update() {
            statusText.text = "Status: " + (mqttComManager.IsConnected ? "Connected" : "Disconnected");
        }
    }
}
