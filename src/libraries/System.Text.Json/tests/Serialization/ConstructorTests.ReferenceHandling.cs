//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//using Xunit;

//namespace System.Text.Json.Serialization.Tests
//{
//    public static partial class ConstructorTests
//    {
//        [Fact]
//        public static void Deserialize_MetadataNotHonored()
//        {
//            string json = GetEmployeeJson();

//            // Metadata ignored by default.
//            var employee = JsonSerializer.Deserialize<Employee>(json);

//            Assert.Equal("Mark", employee.Name);
//            Assert.Equal("John", employee.Manager.Name);
//            Assert.Null(employee.Manager.Manager.Name);
//            Assert.Null(employee.Manager.Manager.Manager);

//            //Metadata still ignored for objects with parameterized constructors.
//            employee = JsonSerializer.Deserialize<Employee>(json, new JsonSerializerOptions { ReferenceHandling = ReferenceHandling.Preserve });
//            Assert.Equal("Mark", employee.Name);
//            Assert.Equal("John", employee.Manager.Name);
//            Assert.Null(employee.Manager.Manager.Name);
//            Assert.Null(employee.Manager.Manager.Manager);
//        }

//        [Fact]
//        public static void Serialize_CyclesLeadWorksFine()
//        {
//            var john = new Employee("John");
//            john.Manager = john;

//            var mark = new Employee("Mark");
//            mark.Manager = john;

//            // Cycles lead to exceptions by default.
//            Assert.Throws<JsonException>(() => JsonSerializer.Serialize(mark));

//            // Serialization works fine with option.
//            JsonSerializer.Serialize(mark, new JsonSerializerOptions { ReferenceHandling = ReferenceHandling.Preserve });
//        }

//        private static string GetEmployeeJson()
//        {
//            var john = new Employee("John");
//            john.Manager = john;

//            var mark = new Employee("Mark");
//            mark.Manager = john;

//            return JsonSerializer.Serialize(mark, new JsonSerializerOptions { ReferenceHandling = ReferenceHandling.Preserve });
//        }

//        private class Employee
//        {
//            public string Name { get; }
//            public Employee Manager { get; set; }

//            public Employee(string name)
//            {
//                Name = name;
//            }
//        }

//        [Fact]
//        // Make sure this does not throw a StackOverFlow exception
//        public static void DeserializingObject_ThatContains_RefInConstructorFails()
//        {
//            //Employee_With_EmployeeArg employee = new Employee_With_EmployeeArg();
//            //employee.FullName = "Jet Doe";
//            //employee.Manager = employee;

//            //JsonSerializerOptions options = new JsonSerializerOptions
//            //{
//            //    ReferenceHandling = ReferenceHandling.Preserve
//            //};

//            //string json = JsonSerializer.Serialize(employee, options);
//            //Console.WriteLine(json);
//        }

//        public class Employee_With_EmployeeArg
//        {
//            public string FullName { get; set; }

//            public Employee_With_EmployeeArg Manager { get; set; }

//            public Employee_With_EmployeeArg(Employee_With_EmployeeArg manager = null)
//            {
//                Manager = manager;
//            }
//        }
//    }
//}
