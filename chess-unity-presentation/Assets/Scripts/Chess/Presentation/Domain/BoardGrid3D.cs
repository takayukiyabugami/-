using System;
using Chess.Domain;
using UnityEngine;

namespace Chess.Presentation
{
    public sealed class BoardGrid3D : MonoBehaviour
    {
        [Tooltip("64 anchors ordered by domain index (a1..h8).")]
        [SerializeField] private Transform[] squareAnchors = new Transform[64];
        [SerializeField] private float pieceYOffset = 0f;

        public Vector3 GetWorldPosition(SquareCoord coord)
        {
            if (!coord.IsOnBoard)
            {
                throw new ArgumentOutOfRangeException(nameof(coord));
            }

            Transform anchor = squareAnchors[coord.ToIndex()];
            if (anchor == null)
            {
                throw new InvalidOperationException($"Anchor missing for {coord}.");
            }

            return anchor.position + Vector3.up * pieceYOffset;
        }

        public bool TryGetWorldPosition(SquareCoord coord, out Vector3 world)
        {
            world = Vector3.zero;
            if (!coord.IsOnBoard)
            {
                return false;
            }

            Transform anchor = squareAnchors[coord.ToIndex()];
            if (anchor == null)
            {
                return false;
            }

            world = anchor.position + Vector3.up * pieceYOffset;
            return true;
        }

        public void ConfigureAnchors(Transform[] anchors, float yOffset = 0f)
        {
            if (anchors == null || anchors.Length != 64)
            {
                throw new ArgumentException("BoardGrid3D requires exactly 64 anchors.", nameof(anchors));
            }

            squareAnchors = anchors;
            pieceYOffset = yOffset;
        }
    }
}
