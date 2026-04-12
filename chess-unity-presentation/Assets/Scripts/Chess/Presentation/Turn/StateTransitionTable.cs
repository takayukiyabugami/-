using System.Collections.Generic;

namespace Chess.Presentation
{
    public static class StateTransitionTable
    {
        private static readonly IReadOnlyDictionary<TurnState, HashSet<TurnState>> Table =
            new Dictionary<TurnState, HashSet<TurnState>>
            {
                { TurnState.Idle, new HashSet<TurnState> { TurnState.Selecting, TurnState.Locked } },
                { TurnState.Selecting, new HashSet<TurnState> { TurnState.MoveRequested, TurnState.Idle, TurnState.Locked } },
                { TurnState.MoveRequested, new HashSet<TurnState> { TurnState.AnimatingMove, TurnState.Selecting, TurnState.Locked } },
                { TurnState.AnimatingMove, new HashSet<TurnState> { TurnState.ResolvingCapture, TurnState.PromotionPending, TurnState.SwitchingTurn, TurnState.Locked } },
                { TurnState.ResolvingCapture, new HashSet<TurnState> { TurnState.PromotionPending, TurnState.SwitchingTurn, TurnState.Locked } },
                { TurnState.PromotionPending, new HashSet<TurnState> { TurnState.SwitchingTurn, TurnState.Locked } },
                { TurnState.SwitchingTurn, new HashSet<TurnState> { TurnState.Idle, TurnState.Locked } },
                { TurnState.Locked, new HashSet<TurnState> { TurnState.Idle } },
            };

        public static bool CanTransition(TurnState from, TurnState to)
        {
            return Table.TryGetValue(from, out HashSet<TurnState> allowed) && allowed.Contains(to);
        }
    }
}
