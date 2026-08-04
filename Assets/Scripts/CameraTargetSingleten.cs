using UnityEngine;

namespace DefaultNamespace
{
    public class CameraTargetSingleten : MonoBehaviour
    {
        public static CameraTargetSingleten Instance;

        public void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
    }
}