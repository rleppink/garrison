using System;
using UnityEngine;

namespace Garrison.Shared.Round
{
    public sealed class SpawnPoints : MonoBehaviour
    {
        [SerializeField] private Transform[] points = Array.Empty<Transform>();

        public int Count => points.Length;

        public Transform GetPoint(int index)
        {
            if (points.Length == 0)
                return null;

            int wrappedIndex = Mathf.Abs(index) % points.Length;
            return points[wrappedIndex];
        }
    }
}
