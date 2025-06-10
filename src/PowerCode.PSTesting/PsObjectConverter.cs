using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Numerics;
using System.Reflection;

namespace PowerCode.PSTesting;

public class PsObjectConverter
{
    private const string PsCustomObjectTypeName = "System.Management.Automation.PSCustomObject";

    private record PreprocessContext(int MaxDepth, CancellationToken CancellationToken);

    public static string ConvertToString(object? inputObject, string[]? includeProperties = null, string[]? excludeProperties = null, bool noIndent = false, int depth = 4, CancellationToken cancellationToken = default)
    {
        var objectToSerialize = PreprocessValue(inputObject, currentDepth: 0, new PreprocessContext(depth, cancellationToken));
        cancellationToken.ThrowIfCancellationRequested();
        
        var builder = new PsObjectTextBuilder(includeProperties, excludeProperties, !noIndent, cancellationToken);
        builder.AppendObject(objectToSerialize);
        return builder.ToString();
    }

    // Pre-process the object so that it serializes the same, except that properties whose
    // values cannot be evaluated are treated as having the value null.
    private static object? PreprocessValue(object? inputObject, int currentDepth, PreprocessContext context)
    {
        if (IsNull(inputObject))
        {
            return null;
        }

        PSObject? pso = null;
        if (inputObject is PSObject p)
        {
            pso = p;
            inputObject = pso.BaseObject;
        }
        
        object? returnValue;
        bool isPurePsObj = false;
        bool isCustomObj = false;

        if (inputObject == NullString.Value || inputObject == DBNull.Value)
        {
            returnValue = null;
        }
        else if(IsPrimitive(inputObject))
        {
            returnValue = inputObject;
        }
        else
        {
            if (currentDepth > context.MaxDepth)
            {
                if (pso is not null && IsPsCustomObject(pso))
                {
                    returnValue = LanguagePrimitives.ConvertTo<string>(pso);
                    isPurePsObj = true;
                }
                else
                {
                    returnValue = LanguagePrimitives.ConvertTo<string>(inputObject);
                }
            }
            else
            {
                (returnValue, isCustomObj) = inputObject switch
                {
                    Hashtable h when h.GetType() == typeof(Hashtable) => ((object)ProcessHashtable(h, currentDepth, context), false),
                    OrderedDictionary o when o.GetType() == typeof(OrderedDictionary) => (ProcessOrderedDictionary(o, currentDepth, context), false),
                    IDictionary dict => (ProcessDictionary(dict, currentDepth, context), false),
                    IEnumerable enumerable => (ProcessEnumerable(enumerable, currentDepth, context), false),
                    _ => (ProcessCustomObject(inputObject, currentDepth, context), true)
                };
            }
            
        }

        returnValue = AddPsProperties(pso, returnValue, currentDepth, isPurePsObj, isCustomObj, context);

        return returnValue;
    }

    private static OrderedDictionary ProcessOrderedDictionary(OrderedDictionary orderedDictionary, int currentDepth, PreprocessContext context)
    {
        var result = new OrderedDictionary(orderedDictionary.Count);
        foreach (DictionaryEntry entry in orderedDictionary)
        {
            result.Add(entry.Key, PreprocessValue(entry.Value, currentDepth + 1, context));
        }

        return result;
    }

    private static Hashtable ProcessHashtable(Hashtable hashtable, int currentDepth, PreprocessContext context)
    {
        var result = new Hashtable(hashtable.Count);
        foreach (DictionaryEntry entry in hashtable)
        {
            result.Add(entry.Key, PreprocessValue(entry.Value, currentDepth + 1, context));
        }

        return result;
    }

    /// <summary>
    /// Add to a base object any properties that might have been added to an object (via PSObject) through the Add-Member cmdlet.
    /// </summary>
    /// <param name="psObj">The containing PSObject, or null if the base object was not contained in a PSObject.</param>
    /// <param name="obj">The base object that might have been decorated with additional properties.</param>
    /// <param name="depth">The current depth into the object graph.</param>
    /// <param name="isPurePsObj">The processed object is a pure PSObject.</param>
    /// <param name="isCustomObj">The processed object is a custom object.</param>
    /// <param name="context">The context for the operation.</param>
    /// <returns>
    /// The original base object if no additional properties had been added,
    /// otherwise a dictionary containing the value of the original base object in the "value" key
    /// as well as the names and values of an additional properties.
    /// </returns>
    private static object? AddPsProperties(object? psObj, object? obj, int depth, bool isPurePsObj, bool isCustomObj, PreprocessContext context)
    {
        if (psObj is not PSObject pso)
        {
            return obj;
        }

        // when isPurePpObj is true, the obj is guaranteed to be a string converted by LanguagePrimitives
        if (isPurePsObj)
        {
            return obj;
        }

        return AppendPsProperties(pso, obj, depth, isCustomObj, context);

    }


    /// <summary>
    /// Append to a dictionary any properties that might have been added to an object (via PSObject) through the Add-Member cmdlet.
    /// If the passed in object is a custom object (not a simple object, not a dictionary, not a list, get processed in ProcessCustomObject method),
    /// we also take Adapted properties into account. Otherwise, we only consider the Extended properties.
    /// When the object is a pure PSObject, it also gets processed in "ProcessCustomObject" before reaching this method, so we will
    /// iterate both extended and adapted properties for it. Since it's a pure PSObject, there will be no adapted properties.
    /// </summary>
    /// <param name="psObj">The containing PSObject, or null if the base object was not contained in a PSObject.</param>
    /// <param name="result">The patched result object</param>
    /// <param name="depth">The current depth into the object graph.</param>
    /// <param name="isCustomObject">The processed object is a custom object.</param>
    /// <param name="context">The context for the operation.</param>
    private static PSObject AppendPsProperties(PSObject psObj, object? result, int depth, bool isCustomObject,
        PreprocessContext context)
    {
        // if the psObj is a DateTime or String type, we don't serialize any extended or adapted properties
        if (IsPrimitive(psObj.BaseObject))
        {
            return psObj;
        }
        

        // serialize only Extended and Adapted properties.
        var includeAdaptedProperties = isCustomObject;
        PSPropertyInfo[] srcPropertiesToSearch = [.. GetProperties(psObj, includeAdaptedProperties)];

        var receiver = result as PSObject ?? new PSObject();
        if (receiver.TypeNames[0] is PsCustomObjectTypeName && psObj.TypeNames[0] is { } typeName && typeName != PsCustomObjectTypeName)
        {
            receiver.TypeNames.Insert(0, "Deserialized." + typeName);
        }
        foreach (PSPropertyInfo prop in srcPropertiesToSearch)
        {
            object? value = null;
            try
            {
                value = prop.Value;
            }
            catch (Exception)
            {
                // ignored
            }

            
            if (receiver.Properties[prop.Name] is null)
            {
                
                var preprocessValue = PreprocessValue(value, depth + 1, context);
                receiver.Properties.Add(new PSNoteProperty(prop.Name, preprocessValue));
            }
        }

        return receiver;
    }

    private static IEnumerable<PSPropertyInfo> GetProperties(PSObject psObj, bool includeAdaptedProperties)
    {
        return !includeAdaptedProperties 
            ? GetExtendedProperties(psObj) 
            : GetExtendedProperties(psObj).Concat(GetAdaptedProperties(psObj));
    }

    private static IEnumerable<PSPropertyInfo> GetExtendedProperties(PSObject psObj)
    {
        dynamic dyn = psObj;
        return dyn.psextended.Properties;

    }

    private static  PSMemberInfoCollection<PSPropertyInfo> GetAdaptedProperties(PSObject psObj)
    {
        dynamic dyn = psObj;
        return dyn.psadapted.Properties;
    }


    /// <summary>
    /// Return an alternate representation of the specified aggregate object that serializes the same PSCustomObject, except
    /// that any contained properties that cannot be evaluated are treated as having the value null.
    ///
    /// The result is a PSObject in which all public fields and public gettable properties of the original object
    /// are represented.  If any exception occurs while retrieving the value of a field or property, that entity
    /// is included in the output dictionary with a value of null.
    /// </summary>
    private static PSObject ProcessCustomObject(object o, int currentDepth, PreprocessContext context)
    {
        PSObject result = new();

        var t = o.GetType();
        if (o is not PSCustomObject && (o is not PSObject pso || pso.TypeNames[0] != PsCustomObjectTypeName))
        {
            result.TypeNames.Insert(0, "Deserialized." + t.FullName);
        }
        var properties = result.Properties;
        foreach (var fileInfo in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var fieldValue = GetFieldValue(o, fileInfo, currentDepth, context);
            properties.Add(new PSNoteProperty(fileInfo.Name, fieldValue));
        }

        foreach (var propertyInfo in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var getMethod = propertyInfo.GetGetMethod();
            if (getMethod == null || getMethod.GetParameters().Length != 0) continue;
            
            var propertyValue = GetPropertyValue(o, getMethod, currentDepth, context);
            properties.Add(new PSNoteProperty(propertyInfo.Name, propertyValue));
        }

        return result;
    }

    private static object? GetPropertyValue(object o, MethodInfo getMethod, int currentDepth, PreprocessContext context)
    {
        object? value;
        try
        {
            value = getMethod.Invoke(o, []);
        }
        catch (Exception)
        {
            value = null;
        }

        var preprocessValue = PreprocessValue(value, currentDepth + 1, context);
        return preprocessValue;
    }

    private static object? GetFieldValue(object o, FieldInfo info, int currentDepth, PreprocessContext context)
    {
        var value = info.GetValue(o);
        
        var preprocessValue = PreprocessValue(value, currentDepth + 1, context);
        return preprocessValue;
    }


    /// <summary>
    /// Return an alternate representation of the specified collection that serializes the same PSCustomObject, except
    /// that any contained properties that cannot be evaluated are treated as having the value null.
    /// </summary>
    private static List<object?> ProcessEnumerable(IEnumerable enumerable, int currentDepth, PreprocessContext context)
    {
        List<object?> result = [];

        foreach (object o in enumerable)
        {
            result.Add(PreprocessValue(o, currentDepth + 1, context));
        }

        return result;
    }

    private static PSObject ProcessDictionary(IDictionary dict, int currentDepth, PreprocessContext context)
    {
        PSObject result = new(dict.Count);
        result.TypeNames.Insert(0, $"Deserialized.{dict.GetType().FullName}");
        var properties = result.Properties;
        foreach (DictionaryEntry entry in dict)
        {
            if (entry.Key is not string name)
            {
                // use the error string that matches the message from JavaScriptSerializer
                throw new InvalidOperationException("Cannot serialize a property with a null name");
            }

            properties.Add(new PSNoteProperty(name, PreprocessValue(entry.Value, currentDepth + 1, context)));
        }

        return result;
    }

    private static bool IsPsCustomObject(PSObject pso) => pso.ImmediateBaseObject is PSCustomObject;

    // ReSharper disable once PossibleUnintendedReferenceComparison
    private static bool IsNull(object? inputObject) => inputObject is null || inputObject == AutomationNull.Value;

    private static readonly Type[] s_primitiveTypes =
    [
        typeof(char),
        typeof(string),
        typeof(bool),
        typeof(byte),
        typeof(sbyte),
        typeof(short),
        typeof(int),
        typeof(long),
        typeof(ushort),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(decimal),
        typeof(Half),
        typeof(float),
        typeof(double),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(Enum),
        typeof(Version),
        typeof(Uri),
        typeof(SemanticVersion),
        typeof(BigInteger),
        typeof(PSObject),
    ];

    internal static bool IsPrimitive([NotNullWhen(returnValue: false)] object? value) =>
        value switch
        {
            null => true,
            PSObject { BaseObject: not null } psObject => IsPrimitive(psObject.BaseObject),
            _ => IsPrimitive(value.GetType())
        };

    internal static bool IsPrimitive(Type type) => s_primitiveTypes.Any(t => t.IsAssignableFrom(type)) || type.IsEnum;
}

