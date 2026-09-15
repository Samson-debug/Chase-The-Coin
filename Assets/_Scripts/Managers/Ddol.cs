using UnityEngine;

namespace ChaseTheCoin.Manager
{
    public class Ddol : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}