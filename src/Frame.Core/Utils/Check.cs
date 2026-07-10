using System;
using System.Diagnostics.CodeAnalysis;

namespace Frame.Core.Utils
{
    public static class Check
    {
        public static T NotNull<T>(
            [NotNull] T? value,
            [NotNull] string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return value;
        }

        public static T NotNull<T>(
            [NotNull] T? value,
            [NotNull] string parameterName,
            string message)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName, message);
            }

            return value;
        }
    }
}
