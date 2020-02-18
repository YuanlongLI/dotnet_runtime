# Deserializing objects using parameterless constructors with `JsonSerializer`

## Motivation

`JsonSerializer` deserializes instances of objects (`class`es and `struct`s) using public parameterless
constructors. If none is present, and deserialization is attempteed, the serializer throws a `NotSupportedException` with a message stating
that objects without public parameterless constructors, including `interfaces` and `abstract` types, are
supported for deserialization. There is no way to deserialize an instance of an object using a parameterized
constructor.

A common pattern is to make data objects immutable for various reasons. For example, given `Point`:

```C#
public struct Point
{
    public int X { get; }

    public int Y { get; }

    public Point(int x, int y) => (X, Y) = (x, y);
}
```

It would be beneficial if `JsonSerializer` could deserialize `Point` instances using the parameterized constructor
above, given that mapping JSON properties into readonly members is not supported.

Also consider `User`:

```C#
public class User
{
    public string UserName { get; private set; }
    
    public bool Enabled { get; private set; }

    public User()
    {
    }

    public User(string userName, bool enabled)
    {
        UserName = userName;
        Enabled = enabled;
    }
}
```

Although there is work scheduled to support deserializing JSON directly into properties with private setters
(https://github.com/dotnet/runtime/issues/29743), providing parameterized constructor support as an option
increases the scope of support for customers with various design needs.

Deserializing with parameterized constructors also gives the opportunity to do JSON "argument" validation once on
the creation of the instance.

This feature also enables deserialization support for `Tuple<...>` instances.

This feature does not affect serialization.

## New API Proposal

```C#
namespace System.Text.Json.Serialization
{
    /// <summary>
    /// When placed on a constructor, indicates that the constructor should be used to create
    /// instances of the type on deserialization.
    /// <remarks>The constructor must be public.</remarks>
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
    public sealed partial class JsonConstructorAttribute : JsonAttribute
    {
        public JsonConstructorAttribute() { }
    }
}
```

### Example usage

Given an immutable class `Point`,

```C#
public class Point
{
    public int X { get; }

    public int Y { get; }

    public Point() {}

    [JsonConstructor]
    public Point(int x, int y) => (X, Y) = (x, y);
}
```

We can deserialize JSON into an instance of `Point` using `JsonSerializer`:

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""x"":1,""y"":2}");
Console.WriteLine(point.X); // 1
Console.WriteLine(point.Y); // 2
```


## Solutions by other libraries

### `Newtonsoft.Json` (.NET)

`Newtonsoft.Json` provides a `[JsonConstructor]` attribute that allows users to specify which constructor to use.
The attribute can be applied to only one constructor, which may be non-public. This proposal only allows public constructors,
but can easily be extended if there is credible demand for it.

`Newtonsoft.Json` also provides a globally applied
[`ConstructorHandling`](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_ConstructorHandling.htm) which controls
which constructor is used if none is specified with the attribute. The options are

`Default`: First attempt to use the public default constructor, then fall back to a single parameterized constructor,
then to the non-public default constructor.

`AllowNonPublicDefaultConstructor`: Newtonsoft.NET will use a non-public default constructor before falling back to a
parameterized constructor.

These options don't map well to how `JsonSerializer` should behave. Non-public support will not be provided by
default, so configuring selection precedence involving non-public constructors is not applicable.


### `Utf8Json`

`Utf8Json` chooses the constructor with the most matched arguments by name (not case-sensitive). Such best-fit matching
allows for a situation where the size of the JSON payload and shape of the target type dictates how much work the serializer
does. Also, it may have a non-trivial performance impact, and may not always choose the constructor that the user wishes
to be used.

The constructor to use can also be specified with a `[SerializationConstructor]` attribute.

`Utf8Json` does not support non-public constructors, even with the attribute. 

### `Jil` (.NET)

`Jil` only supports deserialization using a parameterless constructor (may be non-public), and doesn't provide options
to configure the behavior.

### `Jackson` (Java)

`Jackson` provides an annotation type called
[`JsonCreator`](https://fasterxml.github.io/jackson-annotations/javadoc/2.7/com/fasterxml/jackson/annotation/JsonCreator.html)
which is very similar in functionality to the `JsonConstructor` attributes in `Newtonsoft.Json`
and proposed in this spec.

```Java
@JsonCreator
public BeanWithCreator(
    @JsonProperty("id") int id, 
    @JsonProperty("theName") String name) {
    this.id = id;
    this.name = name;
}
```

As shown, a `@JsonProperty` annotation can be placed on a parameter to indicate the JSON name. Extending the `JsonProperty`
attribute to be placed on constructor parameters was considered, but `Newtonsoft.Json` does not support this, which
suggests that there's not a big customer need for this behavior.

In addition to constructors, the `JsonCreator` can be applied to factory creator methods. There
hasn't been any demand for this from our customers. Support for object deserialization with factory
creation methods can be considered in the future.

## Rules

### Non-`public` constructors

Only `public` constructors are allowed, even when using the `[JsonConstructor]` attribute. The serializer only honors
the attribute when placed on public constructors.

Given `Point`,

```C#
public class Point
{
    public int X { get; }

    public int Y { get; }

    [JsonConstructor]
    private Point() {};
}
```

The class is not supported for deserialization because there's no `public` constructor to use:

```C#
Point point = JsonSerializer.Deserialize<Point>("{}"); // Throws `NotSupportedException.`
```

Given `Point`,

```C#
public class Point
{
    public int X { get; }

    public int Y { get; }

    public Point() {}

    [JsonConstructor]
    private Point(int x, int y) => (X, Y) = (x, y);
}
```

The public parameterless constructor is used, as non-public constructors are not supported:

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""x"":1,""y"":2}");
Console.WriteLine(point.X); // 0
Console.WriteLine(point.Y); // 0
```

To support non-`public` constructors in the future, we would likely have to add to add a global option enabling support,
so we don't cause breaking changes e.g. throwing execptions due to having duplicate `[JsonConstructor]` attributes placed
which were previously ignored.

### Attribute presence

#### Without `[JsonConstructor]`

##### A public parameterless constructor will always be used if present

Given `Point`,

```C#
public class Point
{
    public int X { get; }

    public int Y { get; }

    public Point() {}

    public Point(int x, int y) => (X, Y) = (x, y);
}
```

The public parameterless constructor is used.

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""x"":1,""y"":2}");
Console.WriteLine(point.X); // 0
Console.WriteLine(point.Y); // 0
```

##### For `struct`s, a single parameterized constructor will be used over the default constructor

This is because `struct`s always have default constructors, and there's no way for a user to remove it.

Given `Point`,

```C#
public struct Point
{
    public int X { get; }

    public int Y { get; }

    public Point(int x, int y) => (X, Y) = (x, y);
}
```

A not supported.

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""x"":1,""y"":2}");
Console.WriteLine(point.X); // 1
Console.WriteLine(point.Y); // 2
```

Aside from this caveat, the behavior for `class`es and `struct`s are exactly the same.

##### A single parameterized constructor will always be used if there's no public parameterless constructor

Given `Point`,

```C#
public class Point
{
    public int X { get; }

    public int Y { get; }

    public Point(int x, int y) => (X, Y) = (x, y);
}
```

The singular parameterized constructor is used.

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""x"":1,""y"":2}");
Console.WriteLine(point.X); // 1
Console.WriteLine(point.Y); // 2
```

Given another definition for `Point`,

```C#
public class Point
{
    public int X { get; }

    public int Y { get; }

    public int Z { get; }

    public Point(int x, int y) => (X, Y) = (x, y);

    public Point(int x, int y, int z = 3) => (X, Y, Z) = (x, y, z);
}
```

A `NotSupportedException` is thrown because it is not clear which constructor to use. This may be resolved by using
the `[JsonConstructor]`

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""x"":1,""y"":2,""z"":3}");
Console.WriteLine(point.X); // 1
Console.WriteLine(point.Y); // 2
```

#### Using [JsonConstructor]

##### `[JsonConstructor]` can only be used on one public parameterless constructor

Given `Point`,

```C#
public class Point
{
    public int X { get; }

    public int Y { get; }

    public int Z { get; }

    [JsonConstructor]
    public Point() {}

    public Point(int x, int y) => (X, Y) = (x, y);

    [JsonConstructor]
    public Point(int x, int y, int z = 3) => (X, Y, Z) = (x, y, z);
}
```

An `InvalidOperationException` is thrown:

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""x"":1,""y"":2,""z"":3}"); // Throws `InvalidOperationException`
```

### Parameter name matching

#### Parameter name matching is case insensitive

People serialize readonly properties, might be serialized as PascalCase (if default settings).
Constructor arguments are usually written as camel case.

Given class `Point`:

```C#
public class Point
{
    public int X { get; }

    public int Y { get; }

    public Point(int x, int y) => (X, Y) = (x, y);
}
```

The following deserialization scenarios will work:

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""X"":1,""Y"":2}");
Console.WriteLine(point.X);
Console.WriteLine(point.Y);

point = JsonSerializer.Deserialize<Point>(@"{""x"":1,""y"":2}");
Console.WriteLine(point.X);
Console.WriteLine(point.Y);

...
```

Using case-insensitive matching allows the deserialization of
[`Tuple`](https://docs.microsoft.com/dotnet/api/system.tuple-1.-ctor?view=netcore-3.1#System_Tuple_1__ctor__0_),
instances where the `itemN` constructor arguments are camel case, but the `ItemN` properties are pascal case.
Using case-sensitive matching would force users to configure options (set to case-insensitive) just to deserialize built-in types.

`Newtonsoft.Json` uses case-insensitive parameter comparison.

Open questions:

- Is the reasoning for this worth the potential perf hit? i.e, should it be case sensitive by default
  - There are usually not a lot of constructor parameters, so the number of parameter comparisons should be small.
  - If we choose case-sensitive matching by default, we'll have to provide an option for people to change it.

#### Parameter matching uses `PropertyNamingPolicy`

A major scenario enabled by the feature is the round-trippability of "immutable" types that have read-only properties.

For example, given `Point`:

```C#
public class Point
{
    public int XValue { get; }

    public int YValue { get; }

    public Point(int xValue, int yValue) => (XValue, YValue) = (xValue, yValue);
}
```

Serializing a new instance of `Point` using a `SnakeCase` policy would yield something like the following:

```C#
string serialized = JsonSerializer.Serialize(new Point(1, 2));
Console.WriteLine(serialized); // {"x_value":1,"y_value":2}
```

If constructor parameter matching doesn't honor `PropertyNamingPolicy`, there won't be any matches on deserialization
when it is used.

Another option is to provide a separate option to specify a `JsonNamingPolicy` for constructor parameters.
This would cause friction for users as the general intention would be for the constructor parameters to use the
`PropertyNamingPolicy`. Also, the policy for constructor parameters and properties would almost always be the same.

### If no JSON maps to a constructor parameter, then default values are used.

This is consistent with `Newtonsoft.Json`. If no JSON maps to a constructor parameter, the following fallbacks are used in order:

- default value on constructor parameter
- CLR `default` value for the parameter type

Given `Person`,

```C#
public struct Person
{

    public string Name { get; }

    public int Age { get; }

    public Point Point { get; }

    public Person(string name, int age, Point point = new Point(1, 2))
    {
        Name = name;
        Age = age;
        Point = point;
    }
}
```

When there are no matches for a constructor parameter, a default value is used:

```C#
Person person = JsonSerializer.Deserialize<Person>("{}");
Console.WriteLine(person.Name); // null
Console.WriteLine(person.Age); // 0
Console.WriteLine(person.Point.X); 1
Console.WriteLine(person.Point.Y); 2
```

Another option is to throw, but this behavior would be unecessarily prohibitive for users, and would give the serializer
more work to do to determine which parameters had no JSON.

### `options.IgnoreNullValues` is honored when deserializing constructor arguments

This is helpful to avoid `JsonException` when null is applied to value types.

Given `PointWrapper` and `Point_3D`:

```C#
public class PointWrapper
{
	public Point_3D Point { get; }

	public PointWrapper(Point_3D point) {}
}
	
public struct Point_3D
{
	public int X { get; }

	public int Y { get; }

	public int Z { get; }
}
```

We can ignore `null` and not pass it as an argument to a non-nullable parameter. A default value will be passed instead.
Default behavior without `IgnoreNullValue` would be to preemptively throw a `JsonException`

```C#
var options = new JsonSerializerOptions { IgnoreNullValues = true };
var obj = JsonSerializer.Deserialize<PointWrapper>(@"{""point"":null}"); // obj.Point is `default`
```

`Newtonsoft.Json` fails witherror:

```
Unhandled exception. Newtonsoft.Json.JsonSerializationException: Error converting value {null} to type 'Program+Point_3D'. Path 'Point', line 1, position 21.
```

### Specified constructors cannot have more than 64 parameters

This is an implementation detail. The maximum number of arguments that can be compiled into IL is 65,536.

`ldarg.0-3` and `ladarg.S` allow 256 arguments. `ldarg` can allow more.

We expect most users to have significantly less than 64 parameters, but we can respond to user feedback.

### Members are never set with JSON properties that matched constructor parameters after construction

Doing this can override modifications done in constructor. `Newtonsoft.Json` has the same behavior.

Given `Point`,

```C#
public struct Point
{
    [JsonPropertyName("A")]
    public int X { get; set; }

    [JsonPropertyName("b")]
    public int Y { get; set; }

    public Point_PropertiesHavePropertyNames(int a, int b)
    {
        X = 40;
        Y = 60;
    }
}
```

We can expect the following behavior:

```C#
var obj = JsonSerializer.Deserialize<Point>(@"{""A"":1,""b"":2}");
Assert.Equal(40, obj.X); // Would be 1 if property were set directly after object construction.
Assert.Equal(60, obj.Y); // Would be 2 if property were set directly after object construction.
```

This behavior also applies to property name matches (from JSON to CLR properties) due to naming policy.

### Multiple constructor parameter names

Similar to property and dictionary key deserialization, the last JSON property that matches a constructor parameter wins.

```C#
// u0078 is "x"
Point point = JsonSerializer.Deserialize<Point>(@"{""\u0078"":1,""\u0079"":2,""x"":4}");
Assert.Equal(4, point.X); // Note, the value isn't 1 as first seen
Assert.Equal(2, point.Y);
```

### JSON property name collisions (JSON mapping to multiple constructor parameters)

Under this design, the only way for there to be collisions between constructor parameters is if a `PropertyNamingPolicy`
yields the same name

Given `Point` and a dubious naming policy:

```C#
public class ManyToOneNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        return "JsonName";
    }
}
```

Deserialization scenarios which would lead to collisions will throw an `InvalidOperationException`:

```C#
var options = new JsonSerializerOptions { PropertyNamingPolicy: new ManyToOneNamingPolicy() };
Point point = JsonSerializer.Deserialize<Point>(@"{}", options); // throws `InvalidOperationException`
```

### Serializer features work in the same way

All the rules for `JsonExtensionData`, `ReferenceHandling` semantics, and other serializer features remain the same.
