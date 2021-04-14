using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

/// <summary>
/// Author: Josh H.
/// Support: assetstore.joshh@gmail.com
/// </summary>

namespace JoshH.UI
{
    [CustomEditor(typeof(UIGradient))]
    [CanEditMultipleObjects]
    public class UIGradientEditor : Editor
    {

        private SerializedProperty _blendMode;
        private SerializedProperty _intensity;
        private SerializedProperty _gradientType;

        private SerializedProperty _linearColor1;
        private SerializedProperty _linearColor2;

        private SerializedProperty _cornerColorUpperLeft;
        private SerializedProperty _cornerColorUpperRight;
        private SerializedProperty _cornerColorLowerRight;
        private SerializedProperty _cornerColorLowerLeft;

        private SerializedProperty _linearGradient;

        private SerializedProperty _angle;

        private AnimBool[] animTypeBools;

        private static readonly string[] typeLabels = new string[] { "Linear", "Corner", "ComplexLinear" };

        private void OnEnable()
        {
            _blendMode = serializedObject.FindProperty("blendMode");
            _intensity = serializedObject.FindProperty("intensity");
            _gradientType = serializedObject.FindProperty("gradientType");

            _linearColor1 = serializedObject.FindProperty("linearColor1");
            _linearColor2 = serializedObject.FindProperty("linearColor2");

            _cornerColorUpperLeft = serializedObject.FindProperty("cornerColorUpperLeft");
            _cornerColorUpperRight = serializedObject.FindProperty("cornerColorUpperRight");
            _cornerColorLowerRight = serializedObject.FindProperty("cornerColorLowerRight");
            _cornerColorLowerLeft = serializedObject.FindProperty("cornerColorLowerLeft");

            _linearGradient = serializedObject.FindProperty("linearGradient");

            _angle = serializedObject.FindProperty("angle");

            animTypeBools = new AnimBool[typeLabels.Length];
            SetAnimTypeBools(_gradientType.enumValueIndex);
        }

        protected void SetAnimTypeBools(int choosen)
        {
            for (int i = 0; i < animTypeBools.Length; i++)
            {
                if (animTypeBools[i] == null)
                {
                    animTypeBools[i] = new AnimBool(choosen == i);
                    animTypeBools[i].valueChanged.AddListener(Repaint);
                }
                animTypeBools[i].target = choosen == i;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_blendMode);

            EditorGUILayout.PropertyField(_intensity);

            GradientTypeGUI();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_angle);

            serializedObject.ApplyModifiedProperties();
        }

        void GradientTypeGUI()
        {
            if (_gradientType.hasMultipleDifferentValues)
            {
                int idx = GUILayout.Toolbar(-1, typeLabels);
                if (idx != -1)
                {
                    _gradientType.enumValueIndex = idx;
                }
            }
            else
            {
                _gradientType.enumValueIndex = GUILayout.Toolbar(_gradientType.enumValueIndex, typeLabels);
                SetAnimTypeBools(_gradientType.enumValueIndex);
                ColorFields();
            }
        }

        private void ColorFields()
        {
            EditorGUILayout.Space();
            LinearColorGUI();
            CornerColorGUI();
            ComplexLinearGUI();
        }

        private void LinearColorGUI()
        {
            if (EditorGUILayout.BeginFadeGroup(animTypeBools[0].faded))
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.PropertyField(_linearColor1);
                        EditorGUILayout.PropertyField(_linearColor2);
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginVertical(GUILayout.Width(40));
                    {
                        GUILayout.Space(8);
                        if (GUILayout.Button('\u2191'.ToString() + '\u2193'.ToString())) // Swap "icon"
                        {
                            SwapLinearColors();
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndFadeGroup();
        }

        private void ComplexLinearGUI()
        {
            if (EditorGUILayout.BeginFadeGroup(animTypeBools[2].faded))
            {
                EditorGUILayout.PropertyField(_linearGradient);
            }
            EditorGUILayout.EndFadeGroup();
        }

        private void SwapLinearColors()
        {
            if (!_linearColor1.hasMultipleDifferentValues && !_linearColor2.hasMultipleDifferentValues)
            {
                Color c = _linearColor1.colorValue;
                _linearColor1.colorValue = _linearColor2.colorValue;
                _linearColor2.colorValue = c;
            }
            else
            {
                foreach (UIGradient item in this.targets)
                {
                    Color c = item.LinearColor1;
                    item.LinearColor1 = item.LinearColor2;
                    item.LinearColor2 = c;

                    //Mesh Modification is not triggered without this -> changes not visible
                    item.ForceUpdateGraphic();
                }
            }
        }

        private void CornerColorGUI()
        {
            if (EditorGUILayout.BeginFadeGroup(animTypeBools[1].faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField("Upper Left", GUILayout.MaxWidth(90));
                        _cornerColorUpperLeft.colorValue = EditorGUILayout.ColorField(_cornerColorUpperLeft.colorValue);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField("Upper Right", GUILayout.MaxWidth(90));
                        _cornerColorUpperRight.colorValue = EditorGUILayout.ColorField(_cornerColorUpperRight.colorValue);
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField("Lower Left", GUILayout.MaxWidth(90));
                        _cornerColorLowerLeft.colorValue = EditorGUILayout.ColorField(_cornerColorLowerLeft.colorValue);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField("Lower Right", GUILayout.MaxWidth(90));
                        _cornerColorLowerRight.colorValue = EditorGUILayout.ColorField(_cornerColorLowerRight.colorValue);
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
        }
    }
}