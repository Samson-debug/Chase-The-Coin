using Fusion;

namespace ChaseTheCoin.Player
{
    /// <summary>
    /// Holds the networked input data for the player.
    /// </summary>
    public struct NetworkInputData : INetworkInput
    {
        public float MovementInput;
        public NetworkButtons Buttons;
    }
}
