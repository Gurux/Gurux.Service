using Gurux.Service.Orm;
using Gurux.Service.DB;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.Service_Simple_Unit_Test;

[TestClass]
public sealed class UpdateRelationTests
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    public void ManyToManyCollectionIsNotAColumnInUpdate(int state)
    {
        var value = new User2 { Id = 7, Name = "Updated", Groups = state switch
        {
            0 => null!, 1 => [], _ => [new UserGroup2 { Id = 3, Name = "Group" }]
        } };
        var cache = new GXQueryCache(DatabaseType.MSSQL);
        var sql = GXUpdateArgs.Update(value, cache).ToString(false);
        Assert.AreEqual("UPDATE User2 SET Name = 'Updated' WHERE Id = 7", sql);
        var range = GXUpdateArgs.UpdateRange(new[] { value }, cache).ToString(false);
        Assert.AreEqual(sql, range);
    }

    [TestMethod]
    public void OneToManyCollectionIsSkippedAndOneToOneReferenceIsUpdated()
    {
        var cache = new GXQueryCache(DatabaseType.MSSQL);
        var parent = new Parent2 { Id = 4, Name = "Parent", Childrens = [] };
        Assert.AreEqual("UPDATE Parent2 SET ParentName = 'Parent' WHERE Id = 4",
            GXUpdateArgs.Update(parent, cache).ToString(false));
        var child = new Child2 { Id = 5, Name = "Child", Parent = parent };
        var sql = GXUpdateArgs.Update(child, cache).ToString(false);
        StringAssert.Contains(sql, "Parent = 4");
        StringAssert.Contains(sql, "Name = 'Child'");
    }
}
