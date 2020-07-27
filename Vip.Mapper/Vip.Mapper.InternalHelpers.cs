using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Vip.Mapper
{
    public static partial class AutoMapper
    {
        #region Internal Helpers

        /// <summary>
        ///     Contains the methods and members responsible for this libraries internal concerns.
        /// </summary>
        public static class InternalHelpers
        {
            /// <summary>
            ///     Gets the identifiers for the given type. Returns NULL if not found.
            ///     Results are cached for subsequent use and performance.
            /// </summary>
            /// <remarks>
            ///     If no identifiers have been manually added, this method will attempt
            ///     to first find an <see cref="Vip.Mapper.AutoMapper.Id" /> attribute on the <paramref name="type" />
            ///     and if not found will then try to match based upon any specified identifier conventions.
            /// </remarks>
            /// <param name="type">Type</param>
            /// <returns>Identifier</returns>
            public static IEnumerable<string> GetIdentifiers(Type type)
            {
                var typeMap = Cache.TypeMapCache.GetOrAdd(type, CreateTypeMap(type));

                return typeMap.Identifiers.Any() ? typeMap.Identifiers : null;
            }

            /// <summary>
            ///     Get a Dictionary of a type's property names and field names and their corresponding PropertyInfo or FieldInfo.
            ///     Results are cached for subsequent use and performance.
            /// </summary>
            /// <param name="type">Type</param>
            /// <returns>Dictionary of a type's property names and their corresponding PropertyInfo</returns>
            public static Dictionary<string, object> GetFieldsAndProperties(Type type)
            {
                var typeMap = Cache.TypeMapCache.GetOrAdd(type, CreateTypeMap(type));

                return typeMap.PropertiesAndFieldsInfo;
            }

            /// <summary>
            ///     Creates an instance of the specified type using that type's default constructor.
            /// </summary>
            /// <param name="type">The type of object to create.</param>
            /// <returns>
            ///     A reference to the newly created object.
            /// </returns>
            public static object CreateInstance(Type type)
            {
                return Activator.CreateInstance(type,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    null,
                    CultureInfo.InvariantCulture);
            }

            /// <summary>
            ///     Creates a TypeMap for a given Type.
            /// </summary>
            /// <param name="type">Type</param>
            /// <returns>TypeMap</returns>
            public static Cache.TypeMap CreateTypeMap(Type type)
            {
                var conventionIdentifiers = Configuration.IdentifierConventions.Select(applyIdentifierConvention => applyIdentifierConvention(type)).ToList();

                var fieldsAndProperties = CreateFieldAndPropertyInfoDictionary(type);

                var identifiers = new List<string>();

                foreach (var fieldOrProperty in fieldsAndProperties)
                {
                    var memberName = fieldOrProperty.Key;

                    var member = fieldOrProperty.Value;

                    var fieldInfo = member as FieldInfo;

                    if (fieldInfo != null)
                    {
                        if (fieldInfo.GetCustomAttributes(Configuration.IdentifierAttributeType, false).Length > 0)
                            identifiers.Add(memberName);
                        else if (conventionIdentifiers.Exists(x => x.ToLower() == memberName.ToLower())) identifiers.Add(memberName);
                    }
                    else
                    {
                        var propertyInfo = member as PropertyInfo;

                        if (propertyInfo != null)
                        {
                            if (propertyInfo.GetCustomAttributes(Configuration.IdentifierAttributeType, false).Length > 0)
                                identifiers.Add(memberName);
                            else if (conventionIdentifiers.Exists(x => x.ToLower() == memberName.ToLower())) identifiers.Add(memberName);
                        }
                    }
                }

                var typeMap = new Cache.TypeMap(type, identifiers, fieldsAndProperties);

                return typeMap;
            }

            /// <summary>
            ///     Creates a Dictionary of field or property names and their corresponding FieldInfo or PropertyInfo objects
            /// </summary>
            /// <param name="type">Type</param>
            /// <returns>Dictionary of member names and member info objects</returns>
            public static Dictionary<string, object> CreateFieldAndPropertyInfoDictionary(Type type)
            {
                var dictionary = new Dictionary<string, object>();

                var properties = type.GetProperties();

                foreach (var propertyInfo in properties) dictionary.Add(propertyInfo.Name, propertyInfo);

                var fields = type.GetFields();

                foreach (var fieldInfo in fields) dictionary.Add(fieldInfo.Name, fieldInfo);

                return dictionary;
            }

            /// <summary>
            ///     Gets the Type of the Field or Property
            /// </summary>
            /// <param name="member">FieldInfo or PropertyInfo object</param>
            /// <returns>Type</returns>
            public static Type GetMemberType(object member)
            {
                Type type = null;

                var fieldInfo = member as FieldInfo;

                if (fieldInfo != null)
                {
                    type = fieldInfo.FieldType;
                }
                else
                {
                    var propertyInfo = member as PropertyInfo;

                    if (propertyInfo != null) type = propertyInfo.PropertyType;
                }

                return type;
            }

            /// <summary>
            ///     Sets the value on a Field or Property
            /// </summary>
            /// <param name="member">FieldInfo or PropertyInfo object</param>
            /// <param name="obj">Object to set the value on</param>
            /// <param name="value">Value</param>
            public static void SetMemberValue(object member, object obj, object value)
            {
                var fieldInfo = member as FieldInfo;

                if (fieldInfo != null)
                {
                    value = ConvertValuesTypeToMembersType(value, fieldInfo.Name, fieldInfo.FieldType, fieldInfo.DeclaringType);

                    try
                    {
                        fieldInfo.SetValue(obj, value);
                    }
                    catch (Exception e)
                    {
                        var errorMessage = $"{e.Message}: An error occurred while mapping the value '{value}' of type {value.GetType()} to the member name '{fieldInfo.Name}' of type {fieldInfo.FieldType} on the {fieldInfo.DeclaringType} class.";
                        throw new Exception(errorMessage, e);
                    }
                }
                else
                {
                    var propertyInfo = member as PropertyInfo;

                    if (propertyInfo != null)
                    {
                        value = ConvertValuesTypeToMembersType(value, propertyInfo.Name, propertyInfo.PropertyType, propertyInfo.DeclaringType);

                        try
                        {
                            propertyInfo.SetValue(obj, value, null);
                        }
                        catch (Exception e)
                        {
                            var errorMessage =
                                $"{e.Message}: An error occurred while mapping the value '{value}' of type {value.GetType()} to the member name '{propertyInfo.Name}' of type {propertyInfo.PropertyType} on the {propertyInfo.DeclaringType} class.";
                            throw new Exception(errorMessage, e);
                        }
                    }
                }
            }

            /// <summary>
            ///     Converts the values type to the members type if needed.
            /// </summary>
            /// <param name="value">Object value.</param>
            /// <param name="memberName">Member name.</param>
            /// <param name="memberType">Member type.</param>
            /// <param name="classType">Declarying class type.</param>
            /// <returns>Value converted to the same type as the member type.</returns>
            private static object ConvertValuesTypeToMembersType(object value, string memberName, Type memberType, Type classType)
            {
                if (value == null || value == DBNull.Value)
                    return null;

                var valueType = value.GetType();

                try
                {
                    if (valueType != memberType)
                        foreach (var typeConverter in Configuration.TypeConverters.OrderBy(x => x.Order))
                            if (typeConverter.CanConvert(value, memberType))
                            {
                                var convertedValue = typeConverter.Convert(value, memberType);

                                return convertedValue;
                            }
                }
                catch (Exception e)
                {
                    var errorMessage = $"{e.Message}: An error occurred while mapping the value '{value}' of type {valueType} to the member name '{memberName}' of type {memberType} on the {classType} class.";
                    throw new Exception(errorMessage, e);
                }

                return value;
            }

            /// <summary>
            ///     Gets the value of the member
            /// </summary>
            /// <param name="member">FieldInfo or PropertyInfo object</param>
            /// <param name="obj">Object to get the value from</param>
            /// <returns>Value of the member</returns>
            public static object GetMemberValue(object member, object obj)
            {
                object value = null;

                var fieldInfo = member as FieldInfo;

                if (fieldInfo != null)
                {
                    value = fieldInfo.GetValue(obj);
                }
                else
                {
                    var propertyInfo = member as PropertyInfo;

                    if (propertyInfo != null) value = propertyInfo.GetValue(obj, null);
                }

                return value;
            }

            /// <summary>
            ///     Gets a new or existing instance depending on whether an instance with the same identifiers already existing
            ///     in the instance cache.
            /// </summary>
            /// <param name="type">Type of instance to get</param>
            /// <param name="properties">List of properties and values</param>
            /// <param name="parentHash">Hash from parent object</param>
            /// <returns>
            ///     Tuple of bool, object, int where bool represents whether this is a newly created instance,
            ///     object being an instance of the requested type and int being the instance's identifier hash.
            /// </returns>
            public static Tuple<bool, object, int> GetInstance(Type type, IDictionary<string, object> properties, int parentHash)
            {
                var instanceCache = Cache.GetInstanceCache();

                var identifiers = GetIdentifiers(type);

                object instance = null;

                var isNewlyCreatedInstance = false;

                var identifierHash = 0;

                if (identifiers != null)
                {
                    foreach (var identifier in identifiers)
                        if (properties.ContainsKey(identifier))
                        {
                            var identifierValue = properties[identifier];
                            if (identifierValue != null)
                                identifierHash += identifierValue.GetHashCode() + type.GetHashCode() + parentHash;
                        }

                    if (identifierHash != 0)
                    {
                        if (instanceCache.ContainsKey(identifierHash))
                        {
                            instance = instanceCache[identifierHash];
                        }
                        else
                        {
                            instance = CreateInstance(type);

                            instanceCache.Add(identifierHash, instance);

                            isNewlyCreatedInstance = true;
                        }
                    }
                }

                // An identifier hash with a value of zero means the type does not have any identifiers.
                // To make this instance unique generate a unique hash for it.
                if (identifierHash == 0 && identifiers != null) identifierHash = type.GetHashCode() + parentHash;

                if (instance == null)
                {
                    instance = CreateInstance(type);
                    identifierHash = Guid.NewGuid().GetHashCode();

                    isNewlyCreatedInstance = true;
                }

                return new Tuple<bool, object, int>(isNewlyCreatedInstance, instance, identifierHash);
            }

            /// <summary>
            ///     Populates the given instance's properties where the IDictionary key property names
            ///     match the type's property names case insensitively.
            ///     Population of complex nested child properties is supported by underscoring "_" into the
            ///     nested child properties in the property name.
            /// </summary>
            /// <param name="dictionary">Dictionary of property names and values</param>
            /// <param name="instance">Instance to populate</param>
            /// <param name="parentInstance">Optional parent instance of the instance being populated</param>
            /// <returns>Populated instance</returns>
            public static object Map(IDictionary<string, object> dictionary, object instance, object parentInstance = null)
            {
                var fieldsAndProperties = GetFieldsAndProperties(instance.GetType());

                foreach (var fieldOrProperty in fieldsAndProperties)
                {
                    var memberName = fieldOrProperty.Key.ToLower();

                    var member = fieldOrProperty.Value;

                    // Handle populating simple members on the current type
                    if (dictionary.TryGetValue(memberName, out var value))
                    {
                        SetMemberValue(member, instance, value);
                    }
                    else
                    {
                        var memberType = GetMemberType(member);

                        // Handle populating complex members on the current type
                        if (memberType.IsClass || memberType.IsInterface)
                        {
                            // Try to find any keys that start with the current member name
                            var nestedDictionary = dictionary.Where(x => x.Key.ToLower().StartsWith(memberName + "_")).ToList();

                            // If there weren't any keys
                            if (!nestedDictionary.Any())
                            {
                                // And the parent instance was not null
                                if (parentInstance != null)
                                    // And the parent instance is of the same type as the current member
                                    if (parentInstance.GetType() == memberType)
                                        // Then this must be a 'parent' to the current type
                                        SetMemberValue(member, instance, parentInstance);

                                continue;
                            }

                            var newDictionary = nestedDictionary.ToDictionary(
                                pair => pair.Key.ToLower().Substring(memberName.Length + 1, pair.Key.Length - memberName.Length - 1),
                                pair => pair.Value, StringComparer.OrdinalIgnoreCase);

                            // Try to get the value of the complex member. If the member
                            // hasn't been initialized, then this will return null.
                            var nestedInstance = GetMemberValue(member, instance);

                            // If the member is null and is a class, try to create an instance of the type
                            if (nestedInstance == null && memberType.IsClass) nestedInstance = memberType.IsArray ? new ArrayList().ToArray(memberType.GetElementType()) : CreateInstance(memberType);

                            var genericCollectionType = typeof(IEnumerable<>);

                            if (memberType.IsGenericType && genericCollectionType.IsAssignableFrom(memberType.GetGenericTypeDefinition())
                                || memberType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == genericCollectionType))
                            {
                                var innerType = memberType.GetGenericArguments().FirstOrDefault();

                                if (innerType == null) innerType = memberType.GetElementType();

                                nestedInstance = MapCollection(innerType, newDictionary, nestedInstance, instance);
                            }
                            else
                            {
                                nestedInstance = Map(newDictionary, nestedInstance, instance);
                            }

                            SetMemberValue(member, instance, nestedInstance);
                        }
                    }
                }

                return instance;
            }

            /// <summary>
            ///     Populates the given instance's properties where the IDictionary key property names
            ///     match the type's property names case insensitively.
            ///     Population of complex nested child properties is supported by underscoring "_" into the
            ///     nested child properties in the property name.
            /// </summary>
            /// <param name="type">Underlying instance type</param>
            /// <param name="dictionary">Dictionary of property names and values</param>
            /// <param name="instance">Instance to populate</param>
            /// <param name="parentInstance">Optional parent instance of the instance being populated</param>
            /// <returns>Populated instance</returns>
            public static object MapCollection(Type type, IDictionary<string, object> dictionary, object instance, object parentInstance = null)
            {
                var baseListType = typeof(List<>);

                var listType = baseListType.MakeGenericType(type);

                if (instance == null) instance = CreateInstance(listType);

                // If the dictionnary only contains null values, we return an empty instance
                if (dictionary.Values.FirstOrDefault(v => v != null) == null) return instance;

                var getInstanceResult = GetInstance(type, dictionary, parentInstance?.GetHashCode() ?? 0);

                // Is this a newly created instance? If false, then this item was retrieved from the instance cache.
                var isNewlyCreatedInstance = getInstanceResult.Item1;

                var isArray = instance.GetType().IsArray;

                var instanceToAddToCollectionInstance = getInstanceResult.Item2;

                instanceToAddToCollectionInstance = Map(dictionary, instanceToAddToCollectionInstance, parentInstance);

                if (isNewlyCreatedInstance)
                {
                    if (isArray)
                    {
                        var arrayList = new ArrayList {instanceToAddToCollectionInstance};

                        instance = arrayList.ToArray(type);
                    }
                    else
                    {
                        var addMethod = listType.GetMethod("Add");

                        addMethod.Invoke(instance, new[] {instanceToAddToCollectionInstance});
                    }
                }
                else
                {
                    var containsMethod = listType.GetMethod("Contains");

                    var alreadyContainsInstance = (bool) containsMethod.Invoke(instance, new[] {instanceToAddToCollectionInstance});

                    if (alreadyContainsInstance == false)
                    {
                        if (isArray)
                        {
                            var arrayList = new ArrayList((ICollection) instance);

                            instance = arrayList.ToArray(type);
                        }
                        else
                        {
                            var addMethod = listType.GetMethod("Add");

                            addMethod.Invoke(instance, new[] {instanceToAddToCollectionInstance});
                        }
                    }
                }

                return instance;
            }

            /// <summary>
            ///     Provides a means of getting/storing data in the host application's
            ///     appropriate context.
            /// </summary>
            public interface IContextStorage
            {
                /// <summary>
                ///     Get a stored item.
                /// </summary>
                /// <typeparam name="T">Object type</typeparam>
                /// <param name="key">Item key</param>
                /// <returns>Reference to the requested object</returns>
                T Get<T>(string key);

                /// <summary>
                ///     Stores an item.
                /// </summary>
                /// <param name="key">Item key</param>
                /// <param name="obj">Object to store</param>
                void Store(string key, object obj);

                /// <summary>
                ///     Removes an item.
                /// </summary>
                /// <param name="key">Item key</param>
                void Remove(string key);
            }

            /// <summary>
            ///     Provides a means of getting/storing data in the host application's
            ///     appropriate context.
            /// </summary>
            /// <remarks>
            ///     For ASP.NET applications, it will store in the data in the current HTTPContext.
            ///     For all other applications, it will store the data in the logical call context.
            /// </remarks>
            public class InternalContextStorage : IContextStorage
            {
                /// <summary>
                ///     Get a stored item.
                /// </summary>
                /// <typeparam name="T">Object type</typeparam>
                /// <param name="key">Item key</param>
                /// <returns>Reference to the requested object</returns>
                public T Get<T>(string key)
                {
                    try
                    {
                        return (T) CallContext.GetData(key);
                    }
                    catch (Exception ex)
                    {
                        Logging.Logger.Log(Logging.LogLevel.Error, ex, "An error occurred in ContextStorage.Get() retrieving key: {0} for type: {1}.", key, typeof(T));
                    }

                    return default;
                }

                /// <summary>
                ///     Stores an item.
                /// </summary>
                /// <param name="key">Item key</param>
                /// <param name="obj">Object to store</param>
                public void Store(string key, object obj)
                {
                    CallContext.SetData(key, obj);
                }

                /// <summary>
                ///     Removes an item.
                /// </summary>
                /// <param name="key">Item key</param>
                public void Remove(string key)
                {
                    CallContext.RemoveData(key);
                }
            }

            /// <summary>
            ///     Provides a means of getting/storing data in the host application's
            ///     appropriate context.
            /// </summary>
            /// <remarks>
            ///     For ASP.NET applications, it will store in the data in the current HTTPContext.
            ///     For all other applications, it will store the data in the logical call context.
            /// </remarks>
            public static class ContextStorage
            {
                /// <summary>
                ///     Provides a means of getting/storing data in the host application's
                ///     appropriate context.
                /// </summary>
                public static IContextStorage ContextStorageImplementation { get; set; }

                static ContextStorage()
                {
                    ContextStorageImplementation = new InternalContextStorage();
                }

                /// <summary>
                ///     Get a stored item.
                /// </summary>
                /// <typeparam name="T">Object type</typeparam>
                /// <param name="key">Item key</param>
                /// <returns>Reference to the requested object</returns>
                public static T Get<T>(string key)
                {
                    return ContextStorageImplementation.Get<T>(key);
                }

                /// <summary>
                ///     Stores an item.
                /// </summary>
                /// <param name="key">Item key</param>
                /// <param name="obj">Object to store</param>
                public static void Store(string key, object obj)
                {
                    ContextStorageImplementation.Store(key, obj);
                }

                /// <summary>
                ///     Removes an item.
                /// </summary>
                /// <param name="key">Item key</param>
                public static void Remove(string key)
                {
                    ContextStorageImplementation.Remove(key);
                }
            }
        }

        #endregion Internal Helpers
    }
}