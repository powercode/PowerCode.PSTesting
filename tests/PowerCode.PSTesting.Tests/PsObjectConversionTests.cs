using System.Collections;
using System.Collections.Specialized;
using System.Management.Automation;

namespace PowerCode.PSTesting.Tests;


public class PsObjectConversionTests
{

    [Test]
    public async Task ConvertsDateTimeMember()
    {
        var theDate = new DateTime(2023, 10, 1, 12, 0, 0);
        var psObject = new PSObject(1)
            .AddProperty("Date", theDate);
        var result = PsObjectConverter.ConvertToString(psObject);
        await Assert.That(result).IsLineEqualTo("""
                                            [PSCustomObject] @{
                                              Date = [datetime]::new(2023, 10, 1, 12, 0, 0, [DateTimeKind]::Unspecified)
                                            }
                                            """);
    }


    [Test]
    public async Task ConvertsTimeSpanMember()
    {
        var psObject = new PSObject(1)
            .AddProperty("Time", new TimeSpan(1, 2, 3, 4));
        var result = PsObjectConverter.ConvertToString(psObject);
        await Assert.That(result).IsLineEqualTo("""
                                                [PSCustomObject] @{
                                                  Time = [timespan]::new(1, 2, 3, 4)
                                                }
                                                """);
    }

    [Test]
    public async Task ConvertsHashTables()
    {
        var psObject = new PSObject(2)
            .AddProperty("Hash", new Hashtable { { "Key1", "Value1" } })
            .AddProperty("Ordered", new OrderedDictionary { { "Key1", "Value1" }, { "Key2", "Value2" } });
        var result = PsObjectConverter.ConvertToString(psObject);
        await Assert.That(result).IsLineEqualTo("""
        [PSCustomObject] @{
          Hash = @{
            Key1 = 'Value1'
          }
          Ordered = [ordered] @{
            Key1 = 'Value1'
            Key2 = 'Value2'
          }
        }
        """);
    }

    [Test]
    public async Task ConvertsCustomObjectWithTypeName()
    {
        var psObject = new PSObject(1)
            .AddProperty("Name", "John")
            .AddTypeName("Person");
        var result = PsObjectConverter.ConvertToString(psObject);
        await Assert.That(result).IsLineEqualTo("""
                                                [PSCustomObject] @{
                                                  PSTypeName = 'Deserialized.Person'
                                                  Name = 'John'
                                                }
                                                """);
    }


    [Test]
    public async Task ConvertsType()
    {
        var psObject = new PSObject(2)
            .AddProperty("Type", typeof(int));
        string[] excludeProperties = ["DeclaredMembers", "DeclaredProperties", "DeclaredMethods", "ImplementedInterfaces", "CustomAttributes", "AssemblyQualifiedName", "Assembly", "MetadataToken"];
        var result = PsObjectConverter.ConvertToString(psObject, depth:1, excludeProperties: excludeProperties);
        await Assert.That(result).IsLineEqualTo("""
            [PSCustomObject] @{
              Type = [PSCustomObject] @{
                PSTypeName = 'Deserialized.System.RuntimeType'
                IsCollectible = $false
                DeclaringMethod = $null
                FullName = 'System.Int32'
                Namespace = 'System'
                GUID = [guid]::Parse('ac33e7bc-587c-33d5-89a4-218626424743')
                IsEnum = $false
                IsByRefLike = $false
                IsConstructedGenericType = $false
                IsGenericType = $false
                IsGenericTypeDefinition = $false
                GenericParameterAttributes = $null
                IsSZArray = $false
                GenericParameterPosition = $null
                ContainsGenericParameters = $false
                StructLayoutAttribute = 'System.Runtime.InteropServices.StructLayoutAttribute'
                IsFunctionPointer = $false
                IsUnmanagedFunctionPointer = $false
                Name = 'Int32'
                DeclaringType = $null
                BaseType = 'System.ValueType'
                IsGenericParameter = $false
                IsTypeDefinition = $true
                IsSecurityCritical = $true
                IsSecuritySafeCritical = $false
                IsSecurityTransparent = $false
                MemberType = [System.Reflection.MemberTypes]::TypeInfo
                Module = 'System.Private.CoreLib.dll'
                ReflectedType = $null
                TypeHandle = 'System.RuntimeTypeHandle'
                UnderlyingSystemType = 'int'
                GenericTypeParameters = ''
                DeclaredConstructors = ''
                DeclaredEvents = ''
                DeclaredFields = 'Int32 m_value Int32 MaxValue Int32 MinValue'
                DeclaredNestedTypes = ''
                IsNested = $false
                IsArray = $false
                IsByRef = $false
                IsPointer = $false
                IsGenericTypeParameter = $false
                IsGenericMethodParameter = $false
                IsVariableBoundArray = $false
                HasElementType = $false
                GenericTypeArguments = ''
                Attributes = [System.Reflection.TypeAttributes]'Public, SequentialLayout, Sealed, Serializable, BeforeFieldInit'
                IsAbstract = $false
                IsImport = $false
                IsSealed = $true
                IsSpecialName = $false
                IsClass = $false
                IsNestedAssembly = $false
                IsNestedFamANDAssem = $false
                IsNestedFamily = $false
                IsNestedFamORAssem = $false
                IsNestedPrivate = $false
                IsNestedPublic = $false
                IsNotPublic = $false
                IsPublic = $true
                IsAutoLayout = $false
                IsExplicitLayout = $false
                IsLayoutSequential = $true
                IsAnsiClass = $true
                IsAutoClass = $false
                IsUnicodeClass = $false
                IsCOMObject = $false
                IsContextful = $false
                IsMarshalByRef = $false
                IsPrimitive = $true
                IsValueType = $true
                IsSignatureType = $false
                TypeInitializer = $null
                IsSerializable = $true
                IsVisible = $true
              }
            }
            """);
    }

    [Test]
    public async Task HandlesNonLetterPropertyNames()
    {

        var psObject = new PSObject(4)
            .AddProperty("Name", "John Doe")
            .AddProperty("A.B", "value")
            .AddProperty("AB:", "value");
        var result = PsObjectConverter.ConvertToString(psObject);
        await Assert.That(result).IsLineEqualTo("""
                                                [PSCustomObject] @{
                                                  Name = 'John Doe'
                                                  'A.B' = 'value'
                                                  'AB:' = 'value'
                                                }
                                                """);

    }

    [Test]
    public async Task ConvertsObjectWithException()
    {
        try
        {
            throw new Exception("TheException");
        }
        catch (Exception e)
        {
            var psObject = new PSObject(11)
                .AddProperty("Name", "John Doe")
                .AddProperty("Exception", e);
            var result = PsObjectConverter.ConvertToString(psObject, excludeProperties: ["StackTrace"], depth: 1);
            await Assert.That(result).IsLineEqualTo("""
                                                                      [PSCustomObject] @{
                                                                        Name = 'John Doe'
                                                                        Exception = [PSCustomObject] @{
                                                                          PSTypeName = 'Deserialized.System.Exception'
                                                                          TargetSite = 'Void MoveNext()'
                                                                          Message = 'TheException'
                                                                          Data = 'System.Collections.ListDictionaryInternal'
                                                                          InnerException = $null
                                                                          HelpLink = $null
                                                                          Source = 'PowerCode.PSTesting.Tests'
                                                                          HResult = -2146233088
                                                                        }
                                                                      }
                                                                      """);
        }
    }


    [Test]
    public async Task ConvertsHashTablesCompressed()
    {
        var psObject = new PSObject(11)
            .AddProperty("Hash", new Hashtable { { "Key1", "Value1" } })
            .AddProperty("Ordered", new OrderedDictionary { { "Key1", "Value1" }, { "Key2", "Value2" } });
        var result = PsObjectConverter.ConvertToString(psObject, noIndent: true);
        await Assert.That(result).IsEqualTo("""
                                            [PSCustomObject] @{Hash = @{Key1 = 'Value1'}; Ordered = [ordered] @{Key1 = 'Value1'; Key2 = 'Value2'}}
                                            """);
    }

    [Test]
    public async Task ConvertsArrayOfPsObject()
    {
        object[] psObjects =
        [
            new PSObject(1).AddProperty("Name", "Object1"),
            new PSObject(2).AddProperty("Name", "Object2")
        ];
        var result = PsObjectConverter.ConvertToString(psObjects, noIndent: true);
        await Assert.That(result)
            .IsEqualTo("""
                       @([PSCustomObject] @{Name = 'Object1'}, [PSCustomObject] @{Name = 'Object2'})
                       """);
    }


    [Test]
    [DependsOn(nameof(ConvertsHashTables))]
    public async Task ConvertsObjectsWithPrimitivesAndPsObject()
    {
        var psObject = new PSObject(11)
            .AddProperty("Name", "John Doe")
            .AddProperty("Age", 30)
            .AddProperty("Big", 30ul)
            .AddProperty("IsActive", true)
            .AddProperty("Date", new DateTime(2023, 10, 1, 12, 0, 12))
            .AddProperty("TimeSpan", new TimeSpan(1, 2, 3, 4, 5))
            .AddProperty("Nothing", null)
            .AddProperty("Hash", new Hashtable { { "Key1", "Value1" } })
            .AddProperty("Ordered", new OrderedDictionary { { "Key1", "Value1" }, { "Key2", "Value2" } })
            .AddProperty("Address", new PSObject(3)
                .AddProperty("Street", "123 Main St")
                .AddProperty("City", "Anytown")
                .AddProperty("State", "CA")
                .AddTypeName("StreetAddress"))
            .AddProperty("Tags", new[] { "Tag1", "Tag2", "Tag3" });

        var result = PsObjectConverter.ConvertToString(psObject);
        await Assert.That(result).IsLineEqualTo("""
                                                [PSCustomObject] @{
                                                  Name = 'John Doe'
                                                  Age = 30
                                                  Big = 30ul
                                                  IsActive = $true
                                                  Date = [datetime]::new(2023, 10, 1, 12, 0, 12, [DateTimeKind]::Unspecified)
                                                  TimeSpan = [timespan]::new(1, 2, 3, 4, 5, 0)
                                                  Nothing = $null
                                                  Hash = @{
                                                    Key1 = 'Value1'
                                                  }
                                                  Ordered = [ordered] @{
                                                    Key1 = 'Value1'
                                                    Key2 = 'Value2'
                                                  }
                                                  Address = [PSCustomObject] @{
                                                    PSTypeName = 'Deserialized.StreetAddress'
                                                    Street = '123 Main St'
                                                    City = 'Anytown'
                                                    State = 'CA'
                                                  }
                                                  Tags = @('Tag1', 'Tag2', 'Tag3')
                                                }
                                                """);
    }

    [Test]
    public async Task ConvertsFileInfo()
    {
        var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

        var assembly = new FileInfo(assemblyPath);


        var result = PsObjectConverter.ConvertToString(assembly, includeProperties: ["Name"]);
        await Assert.That(result).IsEqualTo("""
            [PSCustomObject] @{
              PSTypeName = 'Deserialized.System.IO.FileInfo'
              Name = 'PowerCode.PSTesting.Tests.dll'
            }
            """);
    }

    [Test]
    public async Task HandlesParameterizedProperty()
    {

        var psObject = new PSObject(1)
            .AddProperty("TestObj", new TestObject());
        var result = PsObjectConverter.ConvertToString(psObject);
        await Assert.That(result)
            .IsLineEqualTo("""
                           [PSCustomObject] @{
                             TestObj = [PSCustomObject] @{
                               PSTypeName = 'Deserialized.PowerCode.PSTesting.Tests.TestObject'
                               Field = [Uri]::new('MyRelativeUri', [UriKind]::Relative)
                               Name = 'TestObject'
                               Value = 42
                             }
                           }
                           """);
    }

    [Test]
    public async Task HandlesThrowingProperty()
    {
        var psObject = new PSObject(11)
            .AddProperty("Name", "John Doe")
            .AddProperty("Level1", new PSObject(1)
                .AddProperty("Level2Name", "MyName")
                .AddProperty("TestObject", new ThrowingTestObject())
            );
        var result = PsObjectConverter.ConvertToString(psObject, depth: 2);
        await Assert.That(result).IsLineEqualTo("""
                                                [PSCustomObject] @{
                                                  Name = 'John Doe'
                                                  Level1 = [PSCustomObject] @{
                                                    Level2Name = 'MyName'
                                                    TestObject = [PSCustomObject] @{
                                                      PSTypeName = 'Deserialized.PowerCode.PSTesting.Tests.ThrowingTestObject'
                                                      Name = 'TestObject'
                                                      Value = $null
                                                    }
                                                  }
                                                }
                                                """);
    }

    [Test]
    public async Task LimitsDepth()
    {
        var psObject = new PSObject(11)
            .AddProperty("Name", "John Doe")
            .AddProperty("Level1", new PSObject(1)
                .AddProperty("Level2Name", "MyName")
                .AddProperty("Level2", new PSObject(2)
                    .AddProperty("Level3", "Level3Value")
                    )
                );
        var result = PsObjectConverter.ConvertToString(psObject, depth: 1);
        await Assert.That(result).IsLineEqualTo("""
                                            [PSCustomObject] @{
                                              Name = 'John Doe'
                                              Level1 = [PSCustomObject] @{
                                                Level2Name = 'MyName'
                                                Level2 = '@{Level3=Level3Value}'
                                              }
                                            }
                                            """);
    }
}