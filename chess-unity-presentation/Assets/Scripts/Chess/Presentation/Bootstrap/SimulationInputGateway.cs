using System;
using UnityEngine;

namespace Chess.Presentation
{
    public sealed class SimulationInputGateway : MonoBehaviour, IChessInputGateway
    {
        public event Action<MoveRequest> MoveRequested;

        public bool Enabled { get; private set; } = true;

        public void SetInputEnabled(bool enabled)
        {
            Enabled = enabled;
        }

        public bool Submit(in MoveRequest request)
        {
            if (!Enabled)
            {
                return false;
            }

            MoveRequested?.Invoke(request);
            return true;
        }
    }
}
