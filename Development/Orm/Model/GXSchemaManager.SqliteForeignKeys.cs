using Gurux.Service.Orm.Common.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Gurux.Service.Orm.Model
{
    public partial class GXSchemaManager
    {
        // SQLite requires a table rebuild to change a foreign key. Keep the original
        // definitions so checks, defaults, collations, generated columns and options survive.
        private void UpdateSqliteForeignKeys(IDbTransaction? transaction, Type type, List<GXForeignKeySchema> expected)
        {
            IDbConnection connection = transaction?.Connection ?? Connection;
            object? Scalar(string sql, IDbTransaction? tx)
            {
                using var command = connection.CreateCommand();
                command.Transaction = tx;
                command.CommandText = sql;
                return command.ExecuteScalar();
            }
            void Run(string sql, IDbTransaction? tx) => ExecuteNonQuery(this, connection, tx, OnSqlExecuted, sql);
            string table = Builder.GetTableName(type, false);
            string literal = "'" + table.Replace("'", "''") + "'";
            string original = Convert.ToString(Scalar("SELECT sql FROM sqlite_master WHERE type='table' AND name=" + literal, transaction))
                ?? throw new InvalidOperationException("SQLite table definition was not found.");
            var tokens = SqliteDefinitionTokens(original);
            int open = tokens.FindIndex(t => t.Text == "(");
            int close = tokens.FindLastIndex(t => t.Text == ")" && t.Depth == 0);
            if (open < 0 || close <= open)
                throw new NotSupportedException("The SQLite table definition cannot be rebuilt safely.");
            var definitions = new List<string>();
            var unchangedColumns = expected.Where(k => !string.IsNullOrEmpty(k.Name))
                .Select(k => k.Columns.OrderBy(c => c.Position).Select(c => c.Column).ToArray()).ToList();
            int start = tokens[open].End;
            foreach (var token in tokens.Skip(open + 1).Take(close - open))
            {
                if (token.Depth == 1 && token.Text == "," || token.Start == tokens[close].Start)
                {
                    string? definition = RemoveSqliteForeignKey(original.Substring(start, token.Start - start), unchangedColumns);
                    if (!string.IsNullOrWhiteSpace(definition)) definitions.Add(definition);
                    start = token.End;
                }
            }
            definitions.AddRange(expected.Where(k => string.IsNullOrEmpty(k.Name)).Select(BuildForeignKeyClause));
            string temporary = "__gx_fk_" + Guid.NewGuid().ToString("N");
            string create = "CREATE TABLE " + QuoteSchemaIdentifier(temporary) + " (" + string.Join(",\n", definitions) + ")" + original.Substring(tokens[close].End);
            var objects = new List<string>();
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT sql FROM sqlite_master WHERE tbl_name=" + literal + " AND type IN ('index','trigger') AND sql IS NOT NULL";
                using var reader = command.ExecuteReader();
                while (reader.Read()) objects.Add(reader.GetString(0));
            }
            var columns = new List<string>();
            var allColumns = new List<string>();
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "PRAGMA table_xinfo(" + literal + ")";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string name = reader.GetString(1);
                    allColumns.Add(name);
                    if (Convert.ToInt32(reader[6]) == 0) columns.Add(name);
                }
            }
            bool withoutRowId = tokens.Skip(close + 1).Any(t => t.Text == "WITHOUT");
            if (!withoutRowId)
            {
                string? rowid = new[] { "rowid", "_rowid_", "oid" }.FirstOrDefault(n => !allColumns.Contains(n, StringComparer.OrdinalIgnoreCase));
                if (rowid != null) columns.Insert(0, rowid);
            }
            object? sequence = null;
            if (tokens.Any(t => t.Text == "AUTOINCREMENT"))
                sequence = Scalar("SELECT seq FROM sqlite_sequence WHERE name=" + literal, transaction);
            bool enforcement = Convert.ToInt32(Scalar("PRAGMA foreign_keys", transaction)) != 0;
            bool legacyAlter = Convert.ToInt32(Scalar("PRAGMA legacy_alter_table", transaction)) != 0;
            if (transaction != null && enforcement)
                throw new NotSupportedException("SQLite foreign keys must be disabled before starting a transaction that rebuilds foreign keys. Run UpdateTable without an existing transaction.");
            IDbTransaction? owned = null;
            string savepoint = "gx_fk_" + Guid.NewGuid().ToString("N");
            try
            {
                if (enforcement) Run("PRAGMA foreign_keys=OFF", null);
                if (Convert.ToInt32(Scalar("PRAGMA foreign_keys", transaction)) != 0)
                    throw new InvalidOperationException("SQLite foreign key enforcement could not be disabled before the rebuild.");
                if (transaction == null) transaction = owned = connection.BeginTransaction();
                Run("SAVEPOINT " + savepoint, transaction);
                try
                {
                    Run(create, transaction);
                    string names = string.Join(",", columns.Select(QuoteSchemaIdentifier));
                    Run("INSERT INTO " + QuoteSchemaIdentifier(temporary) + " (" + names + ") SELECT " + names + " FROM " + QuoteSchemaIdentifier(table), transaction);
                    Run("DROP TABLE " + QuoteSchemaIdentifier(table), transaction);
                    // Views may reference the temporarily absent original table. Legacy
                    // rename skips reparsing/replacing those references, which stay valid.
                    if (!legacyAlter) Run("PRAGMA legacy_alter_table=ON", transaction);
                    Run("ALTER TABLE " + QuoteSchemaIdentifier(temporary) + " RENAME TO " + QuoteSchemaIdentifier(table), transaction);
                    if (!legacyAlter) Run("PRAGMA legacy_alter_table=OFF", transaction);
                    foreach (string sql in objects) Run(sql, transaction);
                    if (sequence != null && sequence != DBNull.Value)
                        Run("UPDATE sqlite_sequence SET seq=MAX(seq," + Convert.ToInt64(sequence).ToString(System.Globalization.CultureInfo.InvariantCulture) + ") WHERE name=" + literal, transaction);
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = "PRAGMA foreign_key_check";
                        using var reader = command.ExecuteReader();
                        if (reader.Read()) throw new InvalidOperationException(
                            $"SQLite foreign key validation failed in table '{reader.GetValue(0)}', row '{reader.GetValue(1)}', referencing '{reader.GetValue(2)}'; the table rebuild was rolled back.");
                    }
                    Run("RELEASE SAVEPOINT " + savepoint, transaction);
                }
                catch
                {
                    Run("ROLLBACK TO SAVEPOINT " + savepoint, transaction);
                    Run("RELEASE SAVEPOINT " + savepoint, transaction);
                    throw;
                }
                owned?.Commit();
            }
            finally
            {
                owned?.Dispose();
                if (!legacyAlter) Run("PRAGMA legacy_alter_table=OFF", owned == null ? transaction : null);
                if (enforcement) Run("PRAGMA foreign_keys=ON", null);
            }
        }

        private static string? RemoveSqliteForeignKey(string definition, List<string[]> unchangedColumns)
        {
            var tokens = SqliteDefinitionTokens(definition);
            int first = tokens.Count > 0 && tokens[0].Text == "CONSTRAINT" ? 2 : 0;
            bool tableConstraint = first < tokens.Count && tokens[first].Text == "FOREIGN";
            string[] columns;
            if (tableConstraint)
            {
                int open = tokens.FindIndex(first, t => t.Text == "(");
                int close = tokens.FindIndex(open + 1, t => t.Text == ")" && t.Depth == 0);
                if (open < 0 || close < 0) throw new NotSupportedException("Cannot parse SQLite foreign key columns.");
                columns = tokens.Skip(open + 1).Take(close - open - 1).Where(t => t.Text != ",")
                    .Select(t => SqliteUnquoteIdentifier(t.Text)).ToArray();
            }
            else columns = tokens.Count == 0 ? Array.Empty<string>() : new[] { SqliteUnquoteIdentifier(tokens[0].Text) };
            if (unchangedColumns.Any(c => c.SequenceEqual(columns, StringComparer.OrdinalIgnoreCase))) return definition;
            if (tableConstraint) return null;
            int reference = tokens.FindIndex(t => t.Depth == 0 && t.Text == "REFERENCES");
            if (reference < 0) return definition;
            int begin = reference >= 2 && tokens[reference - 2].Text == "CONSTRAINT" ? reference - 2 : reference;
            int end = reference + 2; // REFERENCES table-name
            if (end < tokens.Count && tokens[end].Text == "(")
            {
                ++end;
                while (end < tokens.Count && !(tokens[end].Text == ")" && tokens[end].Depth == 0)) ++end;
                ++end;
            }
            while (end < tokens.Count)
            {
                string word = tokens[end].Text;
                if (word == "MATCH") end += 2;
                else if (word == "ON")
                {
                    end += 2;
                    if (end < tokens.Count && (tokens[end].Text == "SET" || tokens[end].Text == "NO")) end += 2;
                    else ++end;
                }
                else if (word == "DEFERRABLE") ++end;
                else if (word == "NOT" && end + 1 < tokens.Count && tokens[end + 1].Text == "DEFERRABLE") end += 2;
                else if (word == "INITIALLY") end += 2;
                else break;
            }
            int finish = end < tokens.Count ? tokens[end].Start : definition.Length;
            return RemoveSqliteForeignKey(definition.Substring(0, tokens[begin].Start) + " " + definition.Substring(finish), unchangedColumns);
        }

        private static string SqliteUnquoteIdentifier(string name)
        {
            if (name.Length > 1 && (name[0] == '"' || name[0] == '`' || name[0] == '\''))
                return name.Substring(1, name.Length - 2).Replace(new string(name[0], 2), name[0].ToString());
            if (name.Length > 1 && name[0] == '[') return name.Substring(1, name.Length - 2);
            return name;
        }

        // Quoted strings/identifiers and comments are opaque; only unquoted tokens
        // participate in recognizing constraints and top-level commas.
        private static List<(string Text, int Start, int End, int Depth)> SqliteDefinitionTokens(string sql)
        {
            var result = new List<(string Text, int Start, int End, int Depth)>();
            int depth = 0;
            for (int pos = 0; pos < sql.Length;)
            {
                if (char.IsWhiteSpace(sql[pos])) { ++pos; continue; }
                if (pos + 1 < sql.Length && sql[pos] == '-' && sql[pos + 1] == '-')
                { while (pos < sql.Length && sql[pos] != '\n') ++pos; continue; }
                if (pos + 1 < sql.Length && sql[pos] == '/' && sql[pos + 1] == '*')
                {
                    int finish = sql.IndexOf("*/", pos + 2, StringComparison.Ordinal);
                    if (finish < 0) throw new NotSupportedException("Unterminated SQLite SQL comment.");
                    pos = finish + 2; continue;
                }
                int start = pos;
                char ch = sql[pos++];
                bool quoted = ch == '\'' || ch == '"' || ch == '`' || ch == '[';
                if (quoted)
                {
                    char closing = ch == '[' ? ']' : ch;
                    while (pos < sql.Length)
                    {
                        if (sql[pos++] != closing) continue;
                        if (ch != '[' && pos < sql.Length && sql[pos] == closing) { ++pos; continue; }
                        break;
                    }
                }
                else if (char.IsLetterOrDigit(ch) || ch == '_')
                    while (pos < sql.Length && (char.IsLetterOrDigit(sql[pos]) || sql[pos] == '_' || sql[pos] == '$')) ++pos;
                if (ch == ')' && !quoted) --depth;
                result.Add((quoted ? sql.Substring(start, pos - start) : sql.Substring(start, pos - start).ToUpperInvariant(), start, pos, depth));
                if (ch == '(' && !quoted) ++depth;
            }
            return result;
        }
    }
}
