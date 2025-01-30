using System;

namespace Knife.RealBlood
{
    /// <summary>
    /// Observerable interface
    /// </summary>
    public interface ISimpleObserverable
    {
        void AddListener(Action callback);
        void RemoveListener(Action callback);
    }
}