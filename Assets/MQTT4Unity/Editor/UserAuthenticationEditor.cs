using UnityEditor;
using UnityEngine;

namespace MQTT4Unity.Editor {
    [CustomPropertyDrawer(typeof(UserAuthentication))]
    public class UserAuthenticationDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // プロパティを取得
            SerializedProperty enableAuthProp = property.FindPropertyRelative("enableAuth");
            SerializedProperty userNameProp = property.FindPropertyRelative("userName");
            SerializedProperty passwordProp = property.FindPropertyRelative("password");

            // レイアウトの設定
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            enableAuthProp.boolValue = EditorGUI.Toggle(foldoutRect, "Enable Password Auth", enableAuthProp.boolValue);

            if (enableAuthProp.boolValue)
            {
                EditorGUI.indentLevel++;
                Rect userNameRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 5, position.width, EditorGUIUtility.singleLineHeight);
                Rect passwordRect = new Rect(position.x, position.y + 2 * (EditorGUIUtility.singleLineHeight + 5), position.width, EditorGUIUtility.singleLineHeight);

                EditorGUI.PropertyField(userNameRect, userNameProp);
                EditorGUI.PropertyField(passwordRect, passwordProp);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty enableAuthProp = property.FindPropertyRelative("enableAuth");

            if (enableAuthProp.boolValue)
            {
                return 3 * (EditorGUIUtility.singleLineHeight + 5); // Enable時は3行分の高さ
            }
            else
            {
                return EditorGUIUtility.singleLineHeight + 5; // デフォルトの高さ
            }
        }
    }
}