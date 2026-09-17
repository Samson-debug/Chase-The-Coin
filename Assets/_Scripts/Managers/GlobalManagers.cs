using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChaseTheCoin.Manager
{
    [DefaultExecutionOrder(-1000)]
    public class GlobalManagers : MonoBehaviour
    {
        public static GlobalManagers Instance { get; private set; }
        
        private Dictionary<Type, IManager> _managers = new Dictionary<Type, IManager>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            Debug.Log("[GlobalManagers] OnDestroy");
        }

        public bool RegisterManager(IManager manager, bool persistent = false)
        {
            if (manager == null)
            {
                Debug.LogError("[RegisterManager] manager is null.");
                return false;
            }

            Type type = manager.GetType();

            if (!_managers.TryAdd(type, manager))
            {
                Debug.LogWarning($"[RegisterManager] {type.Name} is already registered. Ignoring duplicate.");
                return false;
            }

            if (persistent)
            {
                if (manager is Component component)
                {
                    component.transform.SetParent(transform, false);
                }
                else
                {
                    Debug.LogError($"[RegisterManager] {type.Name} does not derive from Component, cannot parent it.");
                }
            }

            return true;
        }
        
        public T GetManager<T>() where T : class, IManager
        {
            Type type = typeof(T);

            if (_managers.TryGetValue(type, out IManager managerObject))
            {
                return managerObject as T;
            }

            Debug.LogWarning($"GetManager: {type.Name} is not registered yet.");
            return null;
        }
        
        public bool UnregisterManager(IManager manager)
        {
            if (manager == null) return false;

            Type type = manager.GetType();

            if (_managers.TryGetValue(type, out IManager registeredManager) && ReferenceEquals(registeredManager, manager))
            {
                return _managers.Remove(type);
            }

            return false;
        }
        
        #region Debug

        [ContextMenu("Print Dictionary")]
        public void PrintDictionary()
        {
            if (_managers.Count == 0)
            {
                Debug.Log("PrintDictionary: no managers registered.");
                return;
            }

            foreach (KeyValuePair<Type, IManager> manager in _managers)
            {
                string objectName = manager.Value is Component component ? component.name : "N/A";
                Debug.Log($"{manager.Key.Name} | GameObject: {objectName}");
            }
        }

        #endregion
    }
}