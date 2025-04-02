using System.Collections;

using UnityEngine;

namespace Maes.Timing
{
    public class TimeStart : MonoBehaviour
    {
        private float _awakeTime;

        private void Awake()
        {
            _awakeTime = Time.realtimeSinceStartup;
        }

        private IEnumerator Start()
        {
            var awaitEndTime = Time.realtimeSinceStartup;
            Debug.LogFormat("Awake to start update took {0}s", awaitEndTime - _awakeTime);
            var startTime = Time.realtimeSinceStartup;

            // Waits until all update stuff has been called.
            yield return new WaitForEndOfFrame();
            var endTime = Time.realtimeSinceStartup;
            Debug.LogFormat("Start to end of frame took {0}s", endTime - startTime);
            Destroy(this);
        }
    }
}