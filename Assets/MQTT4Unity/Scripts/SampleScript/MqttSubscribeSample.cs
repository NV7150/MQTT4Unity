using System;
using TMPro;
using UnityEngine;

namespace MQTT4Unity.SampleScript {
    public class MqttSubscribeSample : MonoBehaviour {
        [SerializeField] private MqttComManager mqttMan;
        [SerializeField] private string mqttTopic;
        [SerializeField] private TextMeshProUGUI text;

        void Start() {
            mqttMan.Subscribe(mqttTopic, MsgArrived);
        }

        void MsgArrived(string msg) {
            text.text += msg + Environment.NewLine;
        }
    }
}
