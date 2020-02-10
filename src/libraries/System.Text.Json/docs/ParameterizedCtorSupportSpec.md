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
(https://github.com/dotnet/runtime/issues/29743), providing parameterized constructor as an option increases
the scope of support for customers with various design needs.

Deserializing with parameterized constructors also gives the opportunity to do JSON "argument" validation once on
the creation of the instance.

This feature also enables deserialization support for `ValueTuple<...>` types.

<!-- Add notes about roundtrippability and scenarios around deserialization. -->
Typical scenarios are readonly, so serialization should work fine in most cases. Private setters might be supported in
https://github.com/dotnet/runtime/issues/29743.

## Proposal

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

#### More details

##### What about specifying naming policy or `ConstructorParameterNameAttribute` for constructor parameters?

Newtonsoft.Json doesn't have option for this, and there's no signal of any desire for it. This can be added in the future.

https://stackoverflow.com/questions/43032552/json-net-jsonconstructor-constructor-parameter-names

##### Rule out serialization with deconstructors.

We could soon have support for serializing properties with private getters: https://github.com/dotnet/runtime/issues/29743.
Not clear why someone would want to serialize this.


## Examples

## Compatibility with `Newtonsoft.Json`

## Other solutions (outside BCL)

### `Utf8Json` & `Jil` (.NET)

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

<!-- Add note about @JsonProperty annotation -->

<!-- Add note about @JacksonInject annotation -->

In addition to constructors, the `JsonCreator` can be applied to factory creator methods. There
hasn't been any demand for this from our customers. Support for object deserialization with factory
creation methods can be considered in the future, but a new attribute will probably need to be added.

### Go

### Scala

## Rules

### options.IgnoreNullValues applies to parameter deserialization

This is helpful to avoid `JsonException` when null is applied to value types.

```C#
using System;
using Newtonsoft.Json;
					
public class Program
{
	public static void Main()
	{
		var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
		JsonConvert.DeserializeObject<NullArgTester>(@"{""Point3DStruct"":null}");
	}
	
	public class NullArgTester
	{
		public Point_3D_Struct Point3DStruct { get; set; }

		public NullArgTester(Point_3D_Struct point3DStruct) {}
	}
	
	public struct Point_3D_Struct
	{
		public int X { get; }

		public int Y { get; }

		public int Z { get; }
	}
}
```

`Newtonsoft.Json` fails with equivalent error:

```
Unhandled exception. Newtonsoft.Json.JsonSerializationException: Error converting value {null} to type 'Program+Point_3D_Struct'. Path 'Point3DStruct', line 1, position 21.
```

### Parameter name matching is case-insensitive by default.

- People serialize readonly properties, might be serialized as PascalCase. Constructor arguments usual written as camel case. Case insensitive makes sense by default.
- Allows us to support deserializing built-in types like Tuple

Open question:

- Is the reasoning for this worth the potential perf hit? i.e, should it be case sensitive by default
- If we stick to case-insensitive, should we provide an option for people to change it to case-sensitive comparison?


### If no JSON maps to a constructor parameter, then default values are used.

This is consistent with Newtonsoft.Json, and is more performant than trying to check if a constructor argument was provided.

### Non-public constructors cannot be used for deserialization

### Specified constructors cannot have more than 64 parameters

The invocation of specified

`ldarg.0`, `ladarg.1`, `ladarg.2`, `ladarg.3` allow us to load first four arguments unto the stack.
`ldarg.s` allows us to load another 256. The implementation will be limited to this sum number.

### If there's no matching JSON for a constructor argument, the default value is used

- default value on parameter
- CLR default value

### Attribute presence

#### The serializer will not guess which constructor to use

`NotSupportedException` will be thrown in ambiguous situations.

If no `[JsonConstructor]` is specified, the public parameterless constructor will be used if present

If no `[JsonConstructor]` is specified and there's no public parameterless constructor, but exactly
one public parameterized constructor, the singular parameterized will be used for deserialization.

#### Only one public `[JsonConstructor]` can be specified

#### If a single parameterized ctor. is present on a struct, that constructor will be used

Allows support for KVP, BigInteger, ValueTuple and others

#### If a single public parameterized ctor is present, and no public parameterless ct


### Members are never set with JSON properties that match constructor parameters

Doing this can override modifications done in constructor.

### Does ignore null values apply to constructor arguments

Draft no

### What happens when trying to set null to non-nullable parameter

### Duplicate constructor name

Last one wins

## Interaction with other features

## Future

### Factory create methods
