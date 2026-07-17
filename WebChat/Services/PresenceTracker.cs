namespace WebChat.Services
{
    public class PresenceTracker
    {
        // In-memory online-user tracking. A user is "online" while they have at
        // least one live SignalR connection (multiple tabs = multiple connections,
        // hence the count). Single-server only — scaling out would need a backplane.

        private readonly Dictionary<string, List<string>> _onlineUsers = new();

        private readonly object _lock = new();

        public bool Connect(String userId)
        {
            lock (_lock)
            {
                if (_onlineUsers.TryGetValue(userId, out var connections))
                {
                    connections.Add(Guid.NewGuid().ToString());
                    return false; // user was already online
                }
                else
                {
                    _onlineUsers[userId] = new List<string> { Guid.NewGuid().ToString() };
                    return true; // user is now online
                }

            }  // true when this disconnect took the user online -> offline.
        }

        public bool Disconnect(String userId)
        {
            lock (_lock)
            {
                if (_onlineUsers.TryGetValue(userId, out var connections))
                {
                    connections.RemoveAt(connections.Count - 1);
                    if (connections.Count == 0)
                    {
                        _onlineUsers.Remove(userId);
                        return true; // user is now offline
                    }
                }
                return false; // user is still online
            }
        }

        public bool IsOnline(string userId)
        {
            lock (_lock)
            {
                return _onlineUsers.ContainsKey(userId);
            }
        }
    }
}
