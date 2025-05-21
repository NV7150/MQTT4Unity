using UnityEngine;

namespace MQTT4Unity.SampleScript {
    public class MqttPublishSample : MonoBehaviour {
        [SerializeField] private MqttComManager mqttMan;
        
        [SerializeField] private string mqttTopic;
        
        public string Msg { get; set; }

        public void PublishTest() {
            mqttMan.Publish(mqttTopic, Msg);
        }
        
    }
}
