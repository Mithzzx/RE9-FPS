using System;
using UnityEngine;

namespace Knife.RealBlood.LiquidSurface
{
    /// <summary>
    /// Simple transform placer that can by setupped in Inspector
    /// </summary>
    [Serializable]
    public class TransformPlacer
    {
        [SerializeField] private Transform referenceTransform;
        [SerializeField] private Vector3 targetForwardAxis = new Vector3(0, -1, 0);
        [SerializeField] private Vector3 targetUpAxis = new Vector3(1, 0, 0);
        [SerializeField] private Vector3 heightOffsetAxis = new Vector3(0, 1, 0);
        [SerializeField] private float height = 3f;
        [SerializeField] private float localUpAxisRotation = 0f;
        [SerializeField] private bool setParent = true;

        public void Place(Transform transform)
        {
            Vector3 forwardAxis = referenceTransform.rotation * targetForwardAxis;
            Vector3 upAxis = referenceTransform.rotation * targetUpAxis;
            Vector3 offsetAxis = referenceTransform.rotation * heightOffsetAxis;

            transform.position = referenceTransform.position + offsetAxis * height;
            transform.rotation = Quaternion.LookRotation(forwardAxis, Quaternion.AngleAxis(localUpAxisRotation, forwardAxis) * upAxis);
            transform.SetParent(referenceTransform);
        }

        public Vector2 GetOffset(Transform transform)
        {
            Vector3 offset = referenceTransform.InverseTransformPoint(transform.position);

            return new Vector2(offset.x, offset.z);
        }

        public void DrawGizmos(Vector2 size)
        {
            Vector3 cubeCenter = Vector3.up * height / 2f;
            Vector3 cubeSize = new Vector3(size.x, height, size.y);

            Vector3 forwardAxis = referenceTransform.rotation * targetForwardAxis;

            Matrix4x4 gizmoMatrix = Matrix4x4.TRS(referenceTransform.position, Quaternion.AngleAxis(localUpAxisRotation, forwardAxis) * referenceTransform.rotation, Vector3.one);
            Gizmos.matrix = gizmoMatrix;
            Gizmos.DrawWireCube(cubeCenter, cubeSize);
        }
    }
}