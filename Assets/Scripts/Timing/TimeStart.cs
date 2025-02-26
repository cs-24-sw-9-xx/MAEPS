using UnityEngine;

namespace Maes
{
    public class TimeStart : MonoBehaviour
    {
        private int _update = 0;

        private float _awakeTime;
        private float _startTime;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Awake()
        {
            _awakeTime = Time.realtimeSinceStartup;
        }

        private void Start()
        {
            var endTime = Time.realtimeSinceStartup;
            Debug.LogFormat("Awake to start update took {0}s", endTime - _awakeTime);
            _startTime = Time.realtimeSinceStartup;
        }

        // Update is called once per frame
        private void Update()
        {
            if (_update == 2)
            {
                var endTime = Time.realtimeSinceStartup;
                Debug.LogFormat("Start to second update took {0}s", endTime - _startTime);
                _update = 3; // Stop spamming
            }
            else if (_update < 2)
            {
                _update++;
            }
        }
    }
}