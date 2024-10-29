using UnityEngine;
using TMPro;

namespace MAES.Assets.Scripts.UI.Patrolling
{
    class Stats : MonoBehaviour
    {
        private float _totalDistanceTravled = 0f;
        public float TotalDistanceTravled
        {
            get => _totalDistanceTravled;
            set
            {
                _totalDistanceTravled = value;
                _totalDistanceTravledText.text = $"The total patrolling distance traveled: {_totalDistanceTravled} meters";
            }
        }
        [SerializeField]
        private TextMeshProUGUI _totalDistanceTravledText;

        private int _currentGraphIdleness = 0;
        public int CurrentGraphIdleness
        {
            get => _currentGraphIdleness;
            set
            {
                _currentGraphIdleness = value;
                _currentGraphIdlenessText.text = $"Current graph idleness: {_currentGraphIdleness} mins";
            }
        }
        [SerializeField]
        private TextMeshProUGUI _currentGraphIdlenessText;

        private int _worstGraphIdleness = 0;
        public int WorstGraphIdleness
        {
            get => _currentGraphIdleness;
            set
            {
                _worstGraphIdleness = value;
                _worstGraphIdlenessText.text = $"Worst graph idleness: {_worstGraphIdleness} mins";
            }
        }
        [SerializeField]
        private TextMeshProUGUI _worstGraphIdlenessText;


        private float _averageGraphIdleness = 0f;
        public float AverageGraphIdleness
        {
            get => _averageGraphIdleness;
            set
            {
                _averageGraphIdleness = value;
                _averageGraphIdlenessText.text = $"Average graph idleness: {_averageGraphIdleness} mins";
            }
        }
        [SerializeField]
        private TextMeshProUGUI _averageGraphIdlenessText;
    }
}
