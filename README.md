See An [Gurux](http://www.gurux.org/ "Gurux") for an overview.

Join the Gurux Community or follow [@Gurux](http://twitter.com/guruxorg "@Gurux") for project updates.

Open Source Object Relational Mapping (ORM) component for c#, is a part of  Gurux Device Framework. 
For more info check out [Gurux](http://www.gurux.org/ "Gurux").

Purpose of Gurux ORM component is give FAST and SIMPLE to use component that you can use to serialize your database data to C# objects.
Big problem with different databases is that all databases are little bit different and you can't use same SQL sentences to all databases.
Using Gurux ORM component you can serialize your data to different databases. You can use Gurux ORM to create or drop all your tables.
Best part is that in debug mode you can see generated SQL sentence if you move your mouse over argument.

You can use Lambda with Gurux ORM to tell what kind of queries you want to do.

At the moment we are supporting following databases:

* MySql
* Maria BD
* Microsoft SQL
* Oracle
* SQLite

Gurux ORM supports following relations between tables:

* 1:1
* 1:N
* N:N

Creating C# objects
=========================== 

First you should create C# objects that hold the data that you want to save to the database. There is only one limitation at the moment.
All objects must derive from IUnique. This means that they must have unique ID. Reason for this is that we want to identify every object and it's
very slow find objects example by name. It's much faster to use ID. 
You can use DataContract and DataMember attributes to tell what data you want to save to the database.
Relations between objects are told using ForeignKey attribute.

Creating 1:1 object
=========================== 

In this example we have two objects. Company and Country. Each company can be only in one country. So relation is 1:1.

```csharp
[DataContract]
class GXCompany : IUnique<long>
{
    [AutoIncrement]
    [DataMember]
    public long Id
    {
        get;
        set;
    }
    [DataMember]
    public string Name
    {
       get;
       set;
    }
    
    [DataMember(Name = "CountryID")]
    [ForeignKey]
    public GXCountry Country
    {
        get;
        set;
    }

[DataContract]
class GXCountry : IUnique<int>
{
    [DataMember(Name="ID")]
    [AutoIncrement]
    public int Id
    {
        get;
        set;
    }
    [DataMember(Name = "CountryName")]
    public string Name
    {
        get;
        set;
    }	
}
```

Creating 1:N object
=========================== 

In this example we have two objects. Parent and Child. Each parent can have multiple children. So relation is 1:N.

```csharp
[DataContract]
class GXParent : IUnique<int>
{
    [AutoIncrement]
    [DataMember]
    public int Id
    {
        get;
        set;
    }
    [DataMember(Name= "ParentName")]
    public string Name
    {
        get;
        set;
    }

    [DataMember]
    [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
    public GXChild[] Children
    {
        get;
        set;
    }
}

[DataContract]
class GXChild : IUnique<long>
{
    [DataMember]
    [AutoIncrement]
    public long Id
    {
        get;
        set;
    }

    [DataMember, ForeignKey(typeof(GXParent), OnDelete=ForeignKeyDelete.Cascade)]
    public int ParentId
    {
        get;
        set;
    }

    [DataMember]
    public string Name
    {
        get;
        set;
    }
}
```
Because we want to that child knows it's parent only by ID we must told parent's type in ForeignKey parameter.
We also told in foreign key parameter that all children are removed if parent is removed.


Creating N:N object
=========================== 

In this example we have two objects. User and user group. Each user can belong to several user groups. So relation is N:N.

```csharp

[DataContract]
class GXUser : IUnique<int>
{
    [AutoIncrement]
    [DataMember]
    public int Id
    {
        get;
        set;
    }
    [DataMember]
    public string Name
    {
        get;
        set;
    }        
    [DataMember]
    [ForeignKey(typeof(GXUserGroup), typeof(GXUserToUserGroup))]
    public GXUserGroup2[] Groups
    {
        get;
        set;
    }
}

[DataContract]
class GXUserToUserGroup
{
    [DataMember]
    [ForeignKey(typeof(GXUser), OnDelete = ForeignKeyDelete.Cascade)]
    public int UserId
    {
        get;
        set;
    }

    [DataMember]
    [ForeignKey(typeof(GXUserGroup), OnDelete = ForeignKeyDelete.Cascade)]
    public int GroupId
    {
        get;
        set;
    }
}

[DataContract]
class GXUserGroup : IUnique<int>
{
    [DataMember]
    [AutoIncrement]
    public int Id
    {
        get;
        set;
    }

    [DataMember]        
    public string Name
    {
        get;
        set;
    }

    [DataMember]
    [ForeignKey(typeof(GXUser), typeof(GXUserToUserGroup))]
    public GXUser[] Users
    {
        get;
        set;
    }
}
```

With N:N relations we need extra table (GXUserToUserGroup) where we are saving relation information.
We told this to User and user group table in second parameter of ForeignKey attribute.

Making connection to the DB
=========================== 
First you should initialize connection to the DB. Like below:

```csharp
MySqlConnection c = new MySqlConnection("Server=localhost;Database=test;UID=root;Password=");
SqlConnection connection = new SqlConnection("DataBase=test;Server=localhost;Integrated Security=True;");
OracleConnection connection = new OracleConnection("User Id=system;Password=");
SQLiteConnection c = new SQLiteConnection("Data Source=:memory:");
```

After that you create Gurux DB connection component.

```csharp
Connection = new GXDbConnection(c, null);
```

Connection is now ready and you can create your tables to the database. Gurux ORM can walk through relations and create all tables that are connected to others.

```csharp
Connection.CreateTable<GXUser>();
Connection.CreateTable<GXParent>();
Connection.CreateTable<GXCompany>();
```

Insert data.
=========================== 

Data is inserted by GXInsertArgs.

```csharp
GXUser user = new GXUser();
//Fill you class data.

//This generates SQL sentence.
GXInsertArgs arg = GXInsertArgs.Insert(user);
//Insert data to the DB.
Connection.Insert(arg);
```

Select data.
=========================== 
Data is selected by GXSelectArgs. 

```csharp
//Generate SQL sentence where data is search by ID from the DB.
GXSelectArgs arg = GXSelectArgs.SelectById<GXUser>(10);
//Generate SQL sentence where data is search by name like "Gurux.
GXSelectArgs arg = GXSelectArgs.Select<GXUser>(q => q.Name, q => q.Name.Equals("Gurux"));

//Find data from the DB.
List<GXUser> users = Connection.Select<GXUser>(arg);
```

In default ALL relation data is searched from the DB. If you do not want to get relation data you can set Relations to false 
or tell what relation data you do not want to get.

```csharp
//Do not get relation data from other tables.
arg.Relations = false;
//Do not retrieve user group information at all.
arg.Excluded.Add<GXUserGroup>();

//Do not retrieve user group information in User table. 
//If there are other tables where is relation to the user group field they are retrieved.
arg.Excluded.Add<GXUser>(q => q.Groups);

arg.
//Find data from the DB.
List<GXUser> users = Connection.Select<GXUser>(arg);
```

Update data.
=========================== 

Data is updated by GXUpdateArgs.

```csharp
//Update all data to the DB.
GXUpdateArgs arg = GXUpdateArgs.Update(user);
//Udate only name to the DB.
GXUpdateArgs arg = GXUpdateArgs.Update(user, q => q.Name);
//Update data to the DB.
Connection.Update(arg);
```

Delete data.
=========================== 

Data is Deleted by GXDeleteArgs.

```csharp
//Generate SQL sentence where all data is deleted from the table.
GXDeleteArgs arg = GXDeleteArgs.DeleteAll();
//Generate SQL sentence where user is deleted from the DB.
GXDeleteArgs arg = GXDeleteArgs.Delete(user);
//Generate SQL sentence where user is deleted by id.
GXDeleteArgs arg = GXDeleteArgs.DeleteById(10);
//Delete data from the DB.
Connection.Delete(arg);
```

We are updating documentation on Gurux web page. 
If you have problems you can ask your questions in Gurux [Forum](http://www.gurux.org/forum).
