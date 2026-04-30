using System.Threading;
using System.Threading.Tasks;

namespace Othello.Contract;

public interface IOthelloAI
{
    string Name { get; }
    Task<Move?> GetMoveAsync(BoardState board, DiscColor yourColor, CancellationToken ct);
}
