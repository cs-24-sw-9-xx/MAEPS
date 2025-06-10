using UnityEngine;

namespace Maes.UI
{
    public class UIDestroyer : MonoBehaviour
    {
#if !MAEPS_GUI
        private void Awake()
        {
            Destroy(gameObject);
        }
#endif
    }
}