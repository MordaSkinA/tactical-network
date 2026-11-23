using GvGPoc.Models;

namespace GvGPoc.State;

public interface IRosterStore
{
    IReadOnlyList<SquadRosterDto> GetRoster();
    Task SetRosterAsync(IReadOnlyList<SquadRosterDto> roster);
}
