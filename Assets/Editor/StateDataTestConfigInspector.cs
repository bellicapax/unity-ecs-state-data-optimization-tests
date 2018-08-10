using UnityEngine;
using UnityEditor;
using Supyrb;

namespace Inspectors
{
    [CustomEditor(typeof(StateDataTestConfig))]
    public class StateDataTestConfigInspector : Editor
    {
        SerializedProperty _entityCountProperty;
        SerializedProperty _changesPerFrameProperty;
        SerializedProperty _totalStateCountProperty;
        SerializedProperty _interestingStateCountProperty;
        RandomizableInt _changesPerFrame;
        RandomizableInt _interestingStateCount;

        private void OnEnable()
        {
            _entityCountProperty = serializedObject.FindProperty("_entityCount");
            _changesPerFrameProperty = serializedObject.FindProperty("_changesPerFrame");
            _totalStateCountProperty = serializedObject.FindProperty("_totalStateCount");
            _interestingStateCountProperty = serializedObject.FindProperty("_interestingStateCount");
            _changesPerFrame = _changesPerFrameProperty.GetValueFromScriptableObject<RandomizableInt>();
            _interestingStateCount = _interestingStateCountProperty.GetValueFromScriptableObject<RandomizableInt>();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            _entityCountProperty.intValue = Mathf.Max(_entityCountProperty.intValue, 1);
            _changesPerFrame.ConstrainDataExternally(0, _entityCountProperty.intValue);
            _totalStateCountProperty.intValue = Mathf.Max(_totalStateCountProperty.intValue, 1);
            _interestingStateCount.ConstrainDataExternally(1, _totalStateCountProperty.intValue);
            serializedObject.ApplyModifiedProperties();
        }
    }
}