using System;

namespace AssetInventory
{
    public class TimedCache<T>
    {
        private T _cachedValue;
        private DateTime? _expiryTime;

        public void SetValue(T value, TimeSpan ttl)
        {
            _cachedValue = value;
            _expiryTime = DateTime.UtcNow.Add(ttl);
        }

        public bool TryGetValue(out T value)
        {
            if (_expiryTime.HasValue && DateTime.UtcNow <= _expiryTime.Value)
            {
                value = _cachedValue;
                return true;
            }

            value = default(T);
            _expiryTime = null; // Ensure the expiry time is reset if the value has expired
            return false;
        }

        public void Clear()
        {
            _cachedValue = default(T);
            _expiryTime = null;
        }
    }
}
