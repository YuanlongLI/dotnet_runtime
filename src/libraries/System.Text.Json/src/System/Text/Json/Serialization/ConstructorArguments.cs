// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    internal sealed partial class ConstructorArguments<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>
    {
        public TArg0 Arg0 { get; set; } = default!;
        public TArg1 Arg1 { get; set; } = default!;
        public TArg2 Arg2 { get; set; } = default!;
        public TArg3 Arg3 { get; set; } = default!;
        public TArg4 Arg4 { get; set; } = default!;
        public TArg5 Arg5 { get; set; } = default!;
        public TArg6 Arg6 { get; set; } = default!;
    }
}
