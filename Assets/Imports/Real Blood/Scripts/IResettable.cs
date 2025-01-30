using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Resettable interface, used to reset some components and systems
    /// Used in demo only, but you can use to create system that will reset some objects when use object pooling
    /// </summary>
    public interface IResettable
    {
        void ResetComponent();
    }
}