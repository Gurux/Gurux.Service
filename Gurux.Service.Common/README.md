# Gurux.Service.Common

Shared attributes, interfaces, and schema models for Gurux Service object-relational mapping (ORM).
The assembly is named `Gurux.Service.Common`; its root namespace is `Gurux.Service.Orm.Common`.
Database connections, SQL generation, and schema discovery are implemented in the surrounding Gurux Service project.

## Mapping an entity

Reference `Gurux.Service.Common.csproj` from your application or library, then use the mapping attributes on your entity types:

```csharp
using Gurux.Service.Orm.Common;

[Alias("Customers")]
[IndexCollection(true, nameof(Email), Name = "IX_Customers_Email")]
public class Customer : IUnique<int>
{
    [PrimaryKey]
    [AutoIncrement]
    public int Id { get; set; }

    [IsRequired]
    public string Email { get; set; } = string.Empty;

    [Ignore]
    public string DisplayLabel => $"{Id}: {Email}";
}
```

These attributes describe the mapping. The consuming ORM applies them when it performs database operations.

## Attributes

| Attribute | Purpose |
| --- | --- |
| `Alias` | Supplies the table name for a mapped class. |
| `PrimaryKey` | Marks a property or field as a primary key column. |
| `AutoIncrement` | Marks a property or field as an auto-incrementing column. |
| `IsRequired` | Controls whether a column requires a non-null value; the parameterless constructor requires one. |
| `Index` | Describes index options, including uniqueness and sort direction; the parameterless constructor creates a unique index. |
| `IndexCollection` | Describes an index over one or more named columns, with an optional index name. |
| `ForeignKey` | Describes a referenced entity type, an optional mapping table, and delete/update actions. |
| `Relation` | Identifies a related entity type. |
| `Filter` | Selects the comparison used by a filter and optionally the value for which it is omitted. |
| `Ignore` | Excludes a member from database mapping, supported JSON serialization, or both. |
| `TimeStorageUnit` | Selects milliseconds or seconds for storage of a time value. |

Related enumerations are in `Gurux.Service.Orm.Common.Enums`. Attribute support and generated SQL depend on the consuming ORM and database provider.

## Interfaces and callbacks

- `IUnique<T>` exposes an entity's identifier through a read-only `Id` contract.
- `IGXRequest` and `IGXRequest<T>` are marker interfaces for JSON requests.
- `GXTableBase` provides overridable `BeforeAdd`, `AfterAdd`, `BeforeUpdate`, `AfterUpdate`, `BeforeRemove`, and `AfterRemove` callbacks. Their default implementations do nothing.

## Schema models

The `Gurux.Service.Orm.Common.Model` namespace contains mutable metadata models:

| Type | Describes |
| --- | --- |
| `GXTableSchema` | A table's catalog, schema, name, columns, indexes, and foreign keys. |
| `GXColumnSchema` | A column's database/.NET type, nullability, constraints, and other metadata. |
| `GXIndex` | An index name, uniqueness, and indexed columns. |
| `GXIndexColumn` | A column name, sort direction, and position within an index. |
| `GXForeignKeySchema` | A foreign key constraint, its referenced table, column pairs, and referential actions. |
| `GXForeignKeyColumnSchema` | A referencing/referenced column-name pair and its position within the foreign key. |

### Foreign key column order

`Column` and `ReferencedColumn` hold names, rather than references to `GXColumnSchema` objects. This allows a foreign key to be represented without loading the referenced table's column objects.

`Position` is the **zero-based position of the column pair within the foreign key**. It is independent of the column's ordinal position in either table. For example:

```csharp
using Gurux.Service.Orm.Common.Model;

var key = new GXForeignKeySchema
{
    Name = "FK_OrderLines_Orders",
    ReferencedSchema = "dbo",
    ReferencedTable = "Orders",
    Columns =
    [
        new GXForeignKeyColumnSchema
        {
            Column = "TenantId",
            ReferencedColumn = "TenantId",
            Position = 0
        },
        new GXForeignKeyColumnSchema
        {
            Column = "OrderId",
            ReferencedColumn = "Id",
            Position = 1
        }
    ]
};
```

This describes `(TenantId, OrderId)` referencing `Orders(TenantId, Id)`. Keep positions consistent when editing column pairs; the model does not automatically sort its list or renumber entries. Consumers can order pairs by `Position`.

`GXIndexColumn.Position` is also zero-based. `GXColumnSchema.Ordinal` is one-based.

### Serialization and equality

`GXColumnSchema.Type` is excluded from JSON and XML serialization. `TypeName` exposes its full .NET type name for serialization and resolves that name when assigned. An unresolved type name can throw; assigning null or whitespace clears the type.

`GXColumnSchema.Parent` is excluded from JSON, XML, and SOAP serialization. Column equality uses object identity: two separate instances with identical metadata are not equal.

## Building

To build the .NET 10 target from this directory with the .NET 10 SDK:

```powershell
dotnet build Gurux.Service.Common.csproj -p:TargetFrameworks=net10.0 -p:GeneratePackageOnBuild=false
```

The project generates an XML API documentation file alongside the assembly. The current project file lists `net10.0;net90` as its target frameworks; the command above explicitly selects `net10.0`.

## License

GPL-2.0-only. See the source file headers for the license notice.
