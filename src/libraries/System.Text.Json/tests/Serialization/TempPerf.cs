// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class PerfTests
    {
        [Fact]
        public static void Test()
        {
            const long Iterations = 1;

            LoginViewModel value = CreateLoginViewModel();

            var objectWithObjectProperty = new { Prop = (object)value };

            for (long i = 0; i < Iterations; i++)
            {
                System.Text.Json.JsonSerializer.Serialize(objectWithObjectProperty);
            }

            //for (long i = 0; i < Iterations; i++)
            //{
            //    System.Text.Json.JsonSerializer.Deserialize<LoginViewModel>(serialized);
            //}
        }


        [Fact]
        public static void Test2()
        {
            const long Iterations = 10000;

            LoginViewModel value = CreateLoginViewModel();
            string serialized = System.Text.Json.JsonSerializer.Serialize(value);

            for (long i = 0; i < Iterations; i++)
            {
                JsonSerializerOptions options = new JsonSerializerOptions();
                System.Text.Json.JsonSerializer.Deserialize<LoginViewModel>(serialized, options);
            }

            //Console.ReadLine();
        }

        public class LoginViewModel
        {
            public string Email { get; set; }
            public string Password { get; set; }
            public bool RememberMe { get; set; }
        }

        private static LoginViewModel CreateLoginViewModel()
            => new LoginViewModel
            {
                Email = "name.familyname@not.com",
                Password = "abcdefgh123456!@",
                RememberMe = true
            };
    }
}
