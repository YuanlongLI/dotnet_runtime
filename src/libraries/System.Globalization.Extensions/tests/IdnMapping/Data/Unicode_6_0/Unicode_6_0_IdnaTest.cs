// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.Globalization.Tests
{
    /// <summary>
    /// Class to read data obtained from http://www.unicode.org/Public/idna.  For more information read the information
    /// contained in Data\6.0\IdnaTest.txt
    ///
    /// The structure of the data set is a semicolon delimited list with the following columns:
    ///
    /// Column 1: type - T for transitional, N for nontransitional, B for both
    /// Column 2: source - the source string to be tested
    /// Column 3: toUnicode - the result of applying toUnicode to the source, using the specified type
    /// Column 4: toASCII - the result of applying toASCII to the source, using nontransitional
    ///
    /// If the value of toUnicode or toASCII is the same as source, the column will be blank.
    /// </summary>
    public class Unicode_6_0_IdnaTest : Unicode_IdnaTest
    {
        public Unicode_6_0_IdnaTest(string line, int lineNumber)
        {
            var split = line.Split(';');

            Type = ConvertStringToType(split[0].Trim());
            Source = EscapedToLiteralString(split[1], lineNumber);
            UnicodeResult = new ConformanceIdnaUnicodeTestResult(EscapedToLiteralString(split[2], lineNumber), Source);
            ASCIIResult = new ConformanceIdnaTestResult(EscapedToLiteralString(split[3], lineNumber), UnicodeResult.Value);
            LineNumber = lineNumber;
        }

        private static IdnType ConvertStringToType(string idnType)
        {
            switch (idnType)
            {
                case "T":
                    return IdnType.Transitional;
                case "N":
                    return IdnType.Nontransitional;
                case "B":
                    return IdnType.Both;
                default:
                    throw new ArgumentOutOfRangeException(nameof(idnType), "Unknown idnType");
            }
        }
    }
}
