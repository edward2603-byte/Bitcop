using Microsoft.Data.Sqlite;

namespace WorkControl.Agent;

public sealed class LocalEventStore(string databasePath)
{
    private string ConnectionString => new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();

    public void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE IF NOT EXISTS PendingEvents (EventId TEXT PRIMARY KEY, AgentId TEXT NOT NULL, EventType TEXT NOT NULL, OccurredAtUtc TEXT NOT NULL, PayloadJson TEXT NOT NULL);";
        command.ExecuteNonQuery();
    }

    public void Enqueue(AgentControlEvent item)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO PendingEvents VALUES ($id, $agent, $type, $occurred, $payload);";
        command.Parameters.AddWithValue("$id", item.EventId.ToString());
        command.Parameters.AddWithValue("$agent", item.AgentId);
        command.Parameters.AddWithValue("$type", item.EventType);
        command.Parameters.AddWithValue("$occurred", item.OccurredAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$payload", item.PayloadJson);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<AgentControlEvent> GetPending()
    {
        var items = new List<AgentControlEvent>();
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT EventId, AgentId, EventType, OccurredAtUtc, PayloadJson FROM PendingEvents ORDER BY OccurredAtUtc;";
        using var reader = command.ExecuteReader();
        while (reader.Read()) items.Add(new AgentControlEvent(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2), DateTimeOffset.Parse(reader.GetString(3)), reader.GetString(4)));
        return items;
    }

    public void Remove(Guid eventId)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM PendingEvents WHERE EventId = $id;";
        command.Parameters.AddWithValue("$id", eventId.ToString());
        command.ExecuteNonQuery();
    }
}
