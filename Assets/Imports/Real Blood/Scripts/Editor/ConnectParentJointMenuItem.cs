using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Knife.RealBlood
{
    public class ConnectParentJointMenuItem : Editor
    {
        [MenuItem("Knife/Tools/Connect to parent RB")]
        public static void ConnectParent()
        {
            var joints = Selection.GetFiltered<Joint>(SelectionMode.Unfiltered);

            foreach (var j in joints)
            {
                j.connectedBody = j.transform.parent.GetComponent<Rigidbody>();
            }
        }
    }
}