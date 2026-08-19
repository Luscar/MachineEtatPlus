using System.Data.Common;

namespace MachineEtatPlus.Tests.Infrastructure;

/// <summary>
/// Schema de reference cote client. Le paquet n'impose rien : ces tables sont celles que
/// l'application choisit de creer, et l'implementation de <c>IProcessStore</c> les connait seule.
/// </summary>
internal static class SqliteSchema
{
    public const string Script = @"
CREATE TABLE process_instance (
    id                  TEXT    NOT NULL PRIMARY KEY,
    definition_key      TEXT    NOT NULL,
    definition_version  INTEGER NOT NULL,
    status              INTEGER NOT NULL,
    current_node_id     TEXT    NOT NULL,
    variables           TEXT    NULL,
    parent_instance_id  TEXT    NULL,
    parent_node_id      TEXT    NULL,
    depth               INTEGER NOT NULL,
    correlation_id      TEXT    NULL,
    outcome             TEXT    NULL,
    fault_message       TEXT    NULL,
    fault_detail        TEXT    NULL,
    created_at          TEXT    NOT NULL,
    updated_at          TEXT    NOT NULL,
    completed_at        TEXT    NULL,
    row_version         INTEGER NOT NULL
);

CREATE INDEX ix_instance_parent ON process_instance (parent_instance_id);

CREATE TABLE process_wait (
    id                TEXT NOT NULL PRIMARY KEY,
    instance_id       TEXT NOT NULL,
    node_id           TEXT NOT NULL,
    kind              INTEGER NOT NULL,
    due_at            TEXT NULL,
    signal_name       TEXT NULL,
    correlation_key   TEXT NULL,
    interaction_key   TEXT NULL,
    assignee          TEXT NULL,
    child_instance_id TEXT NULL,
    created_at        TEXT NOT NULL
);

CREATE INDEX ix_wait_instance ON process_wait (instance_id);
CREATE INDEX ix_wait_signal ON process_wait (signal_name, correlation_key);
CREATE INDEX ix_wait_due ON process_wait (due_at);

CREATE TABLE process_history (
    id          TEXT NOT NULL PRIMARY KEY,
    instance_id TEXT NOT NULL,
    event_type  INTEGER NOT NULL,
    node_id     TEXT NULL,
    node_kind   INTEGER NULL,
    outcome     TEXT NULL,
    detail      TEXT NULL,
    occurred_at TEXT NOT NULL
);

CREATE INDEX ix_history_instance ON process_history (instance_id);

CREATE TABLE process_job (
    id           INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    instance_id  TEXT NOT NULL,
    node_id      TEXT NOT NULL,
    kind         INTEGER NOT NULL,
    run_at       TEXT NULL,
    wait_id      TEXT NULL,
    reason       TEXT NULL,
    cancelled    INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX ix_job_instance ON process_job (instance_id);
";

    public static void Create(DbConnection connection)
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText = Script;
        command.ExecuteNonQuery();
    }
}
