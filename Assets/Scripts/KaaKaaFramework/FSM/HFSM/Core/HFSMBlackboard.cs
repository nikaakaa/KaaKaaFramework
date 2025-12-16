using System;
using System.Collections.Generic;
using UnityEngine;

namespace KaaKaaFramework.HFSM
{
    /// <summary>
    /// HFSM黑板系统 - 用于状态间共享数据
    /// </summary>
    [Serializable]
    public class HFSMBlackboard
    {
        private Dictionary<string, object> _data = new Dictionary<string, object>();
        private Dictionary<string, Action<object>> _listeners = new Dictionary<string, Action<object>>();

        /// <summary>
        /// 设置数据
        /// </summary>
        public void Set<T>(string key, T value)
        {
            _data[key] = value;
            
            if (_listeners.TryGetValue(key, out var listener))
            {
                listener?.Invoke(value);
            }
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (_data.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// 尝试获取数据
        /// </summary>
        public bool TryGet<T>(string key, out T value)
        {
            if (_data.TryGetValue(key, out var obj) && obj is T typedValue)
            {
                value = typedValue;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// 是否包含指定键
        /// </summary>
        public bool Contains(string key)
        {
            return _data.ContainsKey(key);
        }

        /// <summary>
        /// 移除数据
        /// </summary>
        public bool Remove(string key)
        {
            _listeners.Remove(key);
            return _data.Remove(key);
        }

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear()
        {
            _data.Clear();
            _listeners.Clear();
        }

        /// <summary>
        /// 注册数据变化监听
        /// </summary>
        public void AddListener(string key, Action<object> callback)
        {
            if (_listeners.ContainsKey(key))
            {
                _listeners[key] += callback;
            }
            else
            {
                _listeners[key] = callback;
            }
        }

        /// <summary>
        /// 移除数据变化监听
        /// </summary>
        public void RemoveListener(string key, Action<object> callback)
        {
            if (_listeners.ContainsKey(key))
            {
                _listeners[key] -= callback;
            }
        }

        /// <summary>
        /// 获取所有键
        /// </summary>
        public IEnumerable<string> GetAllKeys()
        {
            return _data.Keys;
        }
    }
}
