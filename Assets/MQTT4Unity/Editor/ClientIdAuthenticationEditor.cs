using UnityEditor;
using UnityEngine;

namespace MQTT4Unity.Editor {
    [CustomPropertyDrawer(typeof(ClientIdAuthentication))]
    public class ClientIdAuthenticationDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // プロパティを取得
            SerializedProperty enableAuthProp = property.FindPropertyRelative("enableAuth");
            SerializedProperty clientIdProp = property.FindPropertyRelative("clientId");

            // レイアウトの設定
            Rect enableAuthRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            enableAuthProp.boolValue = EditorGUI.Toggle(enableAuthRect, "Enable ClientID Auth", enableAuthProp.boolValue);

            if (enableAuthProp.boolValue)
            {
                EditorGUI.indentLevel++;
                Rect clientIdRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 5, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(clientIdRect, clientIdProp);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty enableAuthProp = property.FindPropertyRelative("enableAuth");

            if (enableAuthProp.boolValue)
            {
                return 2 * (EditorGUIUtility.singleLineHeight + 5); // Enable時は2行分の高さ
            }
            else
            {
                return EditorGUIUtility.singleLineHeight + 5; // デフォルトの高さ
            }
        }
    }
}