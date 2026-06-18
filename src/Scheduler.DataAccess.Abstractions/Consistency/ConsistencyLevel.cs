namespace Scheduler.DataAccess.Abstractions.Consistency;

public enum ConsistencyLevel
{
    /// <summary>
    /// Routes to the primary node/client.
    /// DocumentDB: plain primary read (default local read concern); correctness from atomic claim.
    /// Cosmos NoSQL: primary client with SDK-managed Session tokens (read-your-writes).
    /// Cassandra: LOCAL_QUORUM on the primary session.
    /// </summary>
    Strong,

    /// <summary>
    /// Routes to the replica node/client.
    /// DocumentDB: replica, local read concern.
    /// Cosmos NoSQL: replica client with per-request Eventual consistency.
    /// Cassandra: LOCAL_ONE on the replica session.
    /// </summary>
    Eventual
}
