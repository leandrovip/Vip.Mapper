using System;
using System.Collections.Generic;

namespace Vip.Mapper
{
    public static partial class AutoMapper
    {
        #region Configuration

        /// <summary>
        ///     Contains the methods and members responsible for this libraries configuration concerns.
        /// </summary>
        public static class Configuration
        {
            static Configuration()
            {
                IdentifierAttributeType = typeof(Id);

                ApplyDefaultTypeConverters();
            }

            /// <summary>
            ///     Current version of Vip.Mapper.
            /// </summary>
            public static readonly Version Version = new Version("1.0.1.0");

            /// <summary>
            ///     The attribute Type specifying that a field or property is an identifier.
            /// </summary>
            public static Type IdentifierAttributeType;

            /// <summary>
            ///     Convention for finding an identifier.
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            public delegate string ApplyIdentifierConvention(Type type);

            /// <summary>
            ///     Conventions for finding an identifier.
            /// </summary>
            public static readonly List<ApplyIdentifierConvention> IdentifierConventions = new List<ApplyIdentifierConvention>();

            /// <summary>
            ///     Type converters used to convert values from one type to another.
            /// </summary>
            public static readonly List<ITypeConverter> TypeConverters = new List<ITypeConverter>();

            /// <summary>
            ///     Applies default conventions for finding identifiers.
            /// </summary>
            public static void ApplyDefaultIdentifierConventions()
            {
                IdentifierConventions.Add(type => "Id");
                IdentifierConventions.Add(type => type.Name + "Id");
            }

            /// <summary>
            ///     Applies the default ITypeConverters for converting values to different types.
            /// </summary>
            public static void ApplyDefaultTypeConverters()
            {
                TypeConverters.Add(new GuidConverter());
                TypeConverters.Add(new EnumConverter());
                TypeConverters.Add(new ValueTypeConverter());
            }

            /// <summary>
            ///     Adds an identifier for the specified type.
            ///     Replaces any identifiers previously specified.
            /// </summary>
            /// <param name="type">Type</param>
            /// <param name="identifier">Identifier</param>
            public static void AddIdentifier(Type type, string identifier)
            {
                AddIdentifiers(type, new List<string> {identifier});
            }

            /// <summary>
            ///     Adds identifiers for the specified type.
            ///     Replaces any identifiers previously specified.
            /// </summary>
            /// <param name="type">Type</param>
            /// <param name="identifiers">Identifiers</param>
            public static void AddIdentifiers(Type type, IEnumerable<string> identifiers)
            {
                var typeMap = Cache.TypeMapCache.GetOrAdd(type, InternalHelpers.CreateTypeMap(type));

                typeMap.Identifiers = identifiers;
            }

            /// <summary>
            ///     Defines methods that can convert values from one type to another.
            /// </summary>
            public interface ITypeConverter
            {
                /// <summary>
                ///     Converts the given value to the requested type.
                /// </summary>
                /// <param name="value">Value to convert.</param>
                /// <param name="type">Type the value is to be converted to.</param>
                /// <returns>Converted value.</returns>
                object Convert(object value, Type type);

                /// <summary>
                ///     Indicates whether it can convert the given value to the requested type.
                /// </summary>
                /// <param name="value">Value to convert.</param>
                /// <param name="type">Type the value needs to be converted to.</param>
                /// <returns>Boolean response.</returns>
                bool CanConvert(object value, Type type);

                /// <summary>
                ///     Order to execute an <see cref="ITypeConverter" /> in.
                /// </summary>
                int Order { get; }
            }

            /// <summary>
            ///     Converts values to Guids.
            /// </summary>
            public class GuidConverter : ITypeConverter
            {
                #region Implementation of ITypeConverter

                /// <summary>
                ///     Converts the given value to the requested type.
                /// </summary>
                /// <param name="value">Value to convert.</param>
                /// <param name="type">Type the value is to be converted to.</param>
                /// <returns>Converted value.</returns>
                public object Convert(object value, Type type)
                {
                    object convertedValue = null;

                    if (value is string s) convertedValue = new Guid(s);
                    if (value is byte[] bytes) convertedValue = new Guid(bytes);

                    return convertedValue;
                }

                /// <summary>
                ///     Indicates whether it can convert the given value to the requested type.
                /// </summary>
                /// <param name="value">Value to convert.</param>
                /// <param name="type">Type the value needs to be converted to.</param>
                /// <returns>Boolean response.</returns>
                public bool CanConvert(object value, Type type)
                {
                    return type == typeof(Guid);
                }

                /// <summary>
                ///     Order to execute an <see cref="ITypeConverter" /> in.
                /// </summary>
                public int Order => 100;

                #endregion
            }

            /// <summary>
            ///     Converts values to Enums.
            /// </summary>
            public class EnumConverter : ITypeConverter
            {
                #region Implementation of ITypeConverter

                /// <summary>
                ///     Converts the given value to the requested type.
                /// </summary>
                /// <param name="value">Value to convert.</param>
                /// <param name="type">Type the value is to be converted to.</param>
                /// <returns>Converted value.</returns>
                public object Convert(object value, Type type)
                {
                    // Handle Nullable types
                    var conversionType = Nullable.GetUnderlyingType(type) ?? type;

                    object convertedValue = Enum.Parse(conversionType, value.ToString());

                    return convertedValue;
                }

                /// <summary>
                ///     Indicates whether it can convert the given value to the requested type.
                /// </summary>
                /// <param name="value">Value to convert.</param>
                /// <param name="type">Type the value needs to be converted to.</param>
                /// <returns>Boolean response.</returns>
                public bool CanConvert(object value, Type type)
                {
                    var conversionType = Nullable.GetUnderlyingType(type) ?? type;
                    return conversionType.IsEnum;
                }

                /// <summary>
                ///     Order to execute an <see cref="ITypeConverter" /> in.
                /// </summary>
                public int Order => 100;

                #endregion
            }

            /// <summary>
            ///     Converts values types.
            /// </summary>
            public class ValueTypeConverter : ITypeConverter
            {
                #region Implementation of ITypeConverter

                /// <summary>
                ///     Converts the given value to the requested type.
                /// </summary>
                /// <param name="value">Value to convert.</param>
                /// <param name="type">Type the value is to be converted to.</param>
                /// <returns>Converted value.</returns>
                public object Convert(object value, Type type)
                {
                    // Handle Nullable types
                    var conversionType = Nullable.GetUnderlyingType(type) ?? type;

                    var convertedValue = System.Convert.ChangeType(value, conversionType);

                    return convertedValue;
                }

                /// <summary>
                ///     Indicates whether it can convert the given value to the requested type.
                /// </summary>
                /// <param name="value">Value to convert.</param>
                /// <param name="type">Type the value needs to be converted to.</param>
                /// <returns>Boolean response.</returns>
                public bool CanConvert(object value, Type type)
                {
                    return type.IsValueType && !type.IsEnum && type != typeof(Guid);
                }

                /// <summary>
                ///     Order to execute an <see cref="ITypeConverter" /> in.
                /// </summary>
                public int Order => 1000;

                #endregion
            }
        }

        #endregion Configuration
    }
}