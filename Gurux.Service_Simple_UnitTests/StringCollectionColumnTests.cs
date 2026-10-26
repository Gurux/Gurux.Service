using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json;
using Gurux.Service.DB;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Model;
using Microsoft.Data.Sqlite;

namespace Gurux.Service_Simple_Unit_Test;

[TestClass]
public class StringCollectionColumnTests
{
    [DataContract]
    public class CollectionRow : IUnique<int>
    {
        [DataMember] public int Id { get; set; }
        [DataMember, StringLength(4096)] public IEnumerable<string>? Arvo { get; set; } = ["constructor default"];
        [DataMember] public string? Name { get; set; }
    }

    [DataContract]
    public class ConcreteRow : IUnique<int>
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public List<string>? Values { get; set; }
        [DataMember] public string[]? Array { get; set; }
        [DataMember] public IReadOnlyList<string>? ReadOnly { get; set; }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void InsertAndUpdateRoundtripJsonNullAndEmptyCollections(bool range)
    {
        using var sqlite = new SqliteConnection("Data Source=:memory:");
        using var db = new GXDbConnection(sqlite, null);
        var schema = new GXSchemaManager(db);
        schema.CreateTable<CollectionRow>(false, false);
        string[] expected = ["one", "a,b", "quote\"", "O'Reilly", @"C:\temp\file", "\r\n", "\0", "ää漢字🙂", "", null!];
        CollectionRow[] rows = [new() { Id = 1, Arvo = expected.Where(_ => true), Name = "keep" },
            new() { Id = 2, Arvo = null }, new() { Id = 3, Arvo = [] }];
        if (range) db.Insert(GXInsertArgs.InsertRange(rows));
        else foreach (var row in rows) db.Insert(GXInsertArgs.Insert(row));

        var raw = (string)Scalar(sqlite, "SELECT Arvo FROM CollectionRow WHERE Id = 1");
        CollectionAssert.AreEqual(expected, JsonSerializer.Deserialize<string[]>(raw));
        var selected = db.Select<CollectionRow>(GXSelectArgs.SelectAll<CollectionRow>()).OrderBy(r => r.Id).ToArray();
        CollectionAssert.AreEqual(expected, selected[0].Arvo!.ToArray());
        Assert.IsNull(selected[1].Arvo, "SQL NULL must override a property's constructor default.");
        Assert.IsEmpty(selected[2].Arvo!);
        Assert.AreEqual("[]", Scalar(sqlite, "SELECT Arvo FROM CollectionRow WHERE Id = 3"));
        Assert.AreEqual(DBNull.Value, Scalar(sqlite, "SELECT Arvo FROM CollectionRow WHERE Id = 2"));

        selected[0].Arvo = ["updated", "duplicate", "duplicate"];
        selected[1].Arvo = [];
        selected[2].Arvo = null;
        if (range) db.Update(GXUpdateArgs.UpdateRange(selected, r => r.Arvo!));
        else foreach (var row in selected) db.Update(GXUpdateArgs.Update(row, r => r.Arvo!));
        var updated = db.Select<CollectionRow>(GXSelectArgs.SelectAll<CollectionRow>()).OrderBy(r => r.Id).ToArray();
        CollectionAssert.AreEqual(new[] { "updated", "duplicate", "duplicate" }, updated[0].Arvo!.ToArray());
        Assert.IsEmpty(updated[1].Arvo!);
        Assert.IsNull(updated[2].Arvo);
        Assert.AreEqual("keep", updated[0].Name, "Selected-column updates must preserve other fields.");
        Assert.IsFalse(schema.GetTableChanges(typeof(CollectionRow)).Any(c => c.Contains(nameof(CollectionRow.Arvo))),
            "Schema comparison must recognize string collections as text columns.");
    }

    [TestMethod]
    public void ReadsConcreteAndReadOnlyStringCollections()
    {
        using var sqlite = new SqliteConnection("Data Source=:memory:");
        using var db = new GXDbConnection(sqlite, null);
        new GXSchemaManager(db).CreateTable<ConcreteRow>(false, false);
        var row = new ConcreteRow { Id = 1, Values = ["a", "b"], Array = ["c"], ReadOnly = ["d"] };
        db.Insert(GXInsertArgs.Insert(row));
        var read = db.SingleOrDefault<ConcreteRow>(GXSelectArgs.SelectAll<ConcreteRow>())!;
        CollectionAssert.AreEqual(row.Values, read.Values);
        CollectionAssert.AreEqual(row.Array, read.Array);
        CollectionAssert.AreEqual(row.ReadOnly.ToArray(), read.ReadOnly!.ToArray());
    }

    [TestMethod]
    [DataRow(DatabaseType.SqLite)]
    [DataRow(DatabaseType.MSSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    public void InsertAndUpdateUseOneTextLiteralPerCollection(DatabaseType provider)
    {
        var cache = new GXQueryCache(provider);
        var row = new CollectionRow { Id = 1, Arvo = ["a", "b"] };
        StringAssert.Contains(GXInsertArgs.Insert(row, cache).ToString(false), "'[\"a\",\"b\"]'");
        StringAssert.Contains(GXUpdateArgs.Update(row, r => r.Arvo!, cache).ToString(false), "'[\"a\",\"b\"]'");
    }

    private static object Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar()!;
    }
}
