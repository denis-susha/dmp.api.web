namespace DMP.BL.Services.User;

/// <summary>
/// Tracks the SignalR connection ids of each user. Registered as a singleton and accessed concurrently
/// by hub connect/disconnect callbacks and by request handlers that push messages.
/// </summary>
public class UserConnectionService
{
    // A single lock keeps add/remove/snapshot atomic, including removal of a user's entry once
    // their last connection is gone (a per-set lock would race with a concurrent add to that set).
    private readonly Lock _lock = new();
    private readonly Dictionary<Guid, HashSet<string>> _userConnections = [];

    public void AddConnection(Guid userId, string connectionId)
    {
        lock (_lock)
        {
            if (!_userConnections.TryGetValue(userId, out var connections))
            {
                connections = [];
                _userConnections[userId] = connections;
            }

            connections.Add(connectionId);
        }
    }

    public void RemoveConnection(Guid userId, string connectionId)
    {
        lock (_lock)
        {
            if (_userConnections.TryGetValue(userId, out var connections)
                && connections.Remove(connectionId)
                && connections.Count == 0)
            {
                _userConnections.Remove(userId);
            }
        }
    }

    /// <summary>
    /// Returns a snapshot of the user's connection ids, or <c>null</c> if the user has no connections.
    /// </summary>
    public IEnumerable<string>? GetConnectionIds(Guid userId)
    {
        lock (_lock)
        {
            return _userConnections.TryGetValue(userId, out var connections) ? [.. connections] : null;
        }
    }
}
