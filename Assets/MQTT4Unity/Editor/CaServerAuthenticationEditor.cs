using UnityEditor;
using UnityEngine;

namespace MQTT4Unity.Editor {
    [CustomPropertyDrawer(typeof(CaServerAuthentication))]
    public class CaServerAuthenticationDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get properties
            SerializedProperty enableAuthProp = property.FindPropertyRelative("enableCaServerAuth");
            SerializedProperty caCertFileProp = property.FindPropertyRelative("caCertFile");

            // Layout setup
            Rect toggleRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            enableAuthProp.boolValue = EditorGUI.Toggle(toggleRect, "Enable CA Server Auth", enableAuthProp.boolValue);

            if (enableAuthProp.boolValue)
            {
                EditorGUI.indentLevel++;
                Rect certFileRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 5, position.width, EditorGUIUtility.singleLineHeight);

                EditorGUI.PropertyField(certFileRect, caCertFileProp, new GUIContent("CA Certificate File"));
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty enableAuthProp = property.FindPropertyRelative("enableCaServerAuth");

            if (enableAuthProp.boolValue)
            {
                return 2 * (EditorGUIUtility.singleLineHeight + 5); // When enabled: 2 lines height
            }
            else
            {
                return EditorGUIUtility.singleLineHeight + 5; // Default height
            }
        }
    }
} 