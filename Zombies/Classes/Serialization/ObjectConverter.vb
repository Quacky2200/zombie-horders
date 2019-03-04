Option Strict Off

Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Net
Imports Zombies.[Shared]

'Imports Microsoft.Xna.Framework
'Imports ProjectZ.Shared.Drawing
'Imports ProjectZ.Shared.Drawing.UI
'Imports ProjectZ.Shared.Extensions

Namespace [Shared].Serialization

    Public Class ObjectConverter

#Region "Serialization Dictionary"

        Public Shared Property SerializationConverters As New Dictionary(Of Type, Func(Of Object, Object)) From {
            {GetType(Point), Function(obj) obj},
            {GetType(Thickness), Function(obj) obj},
            {GetType(IPAddress), Function(obj) obj.ToString},
            {GetType(IPEndPoint), Function(obj) ConvertIPEndPoint(obj)},
            {GetType(Guid), Function(obj) Example_GUID_Converter(obj)}
        }

        Private Shared Function ConvertToMetaData(obj As Object) As Object
            Return New MetaData(obj)
        End Function

        Private Shared Function Example_GUID_Converter(obj As Guid) As Object
            Return obj.ToString
        End Function
        Private Shared Function ConvertIPEndPoint(Obj As IPEndPoint) As Object
            ' so look. You check the proeprties of the class being serialized.
            ' IPEndPoint contains an IP dDrress class, port is an integer, already serializable
            ' look, you run this through a serializer, you get the serialized properties, you return them.
            ' the properties in the metadata are only the ones with serializer functions in the converter dictionary
            ' this gives us only the important values, like Address and port
            ' run this and you will see what i mean
            Dim MD As MetaData = New MetaData(Obj)
            Return MD.Properties.Values
        End Function
 
#End Region

#Region "Deserialization Dictionary"

        Public Shared ReadOnly Property DeserializationTypes As Dictionary(Of String, Type)
            Get
                If Not _Initialized Then
                    Dim Assemblies As Assembly() = AppDomain.CurrentDomain.GetAssemblies()
                    For Each a As Assembly In Assemblies
                        For Each C As Type In a.GetExportedTypes().Where(Function(t) t.IsClass)
                            _DeserializationTypes.Add(C.FullName, C)
                        Next
                    Next

                    _Initialized = True
                End If
                Return _DeserializationTypes
            End Get
        End Property
        Private Shared _DeserializationTypes As New Dictionary(Of String, Type)
        Private Shared _Initialized As Boolean = False

#End Region

        Public Shared ReflectionFlags As BindingFlags = BindingFlags.Instance Or BindingFlags.[Public]

        Public Overloads Shared Function SerializeProperties(Obj As Object) As Dictionary(Of String, Object)
            Return SerializeProperties(Obj, {"Scene", "Parent"})
        End Function

        Public Overloads Shared Function SerializeProperties(Obj As Object, IgnorePropertyNames As String()) As Dictionary(Of String, Object)
            Dim Value As New Dictionary(Of String, Object)
            Dim type As Type = Obj.[GetType]()
            Dim properties As PropertyInfo() = type.GetProperties(ReflectionFlags)

            For Each p As PropertyInfo In properties
                If IgnorePropertyNames.Contains(p.Name) Then Continue For
                Dim v As Object = p.GetValue(Obj, Nothing)
                If v IsNot Nothing AndAlso (IsSerializable(v) OrElse Convertable(v.GetType)) Then
                    Value.Add(p.Name, GetSerializedValue(v))
                End If
            Next
            Return Value
        End Function

        Public Shared Function DeserializeProperties(Obj As Object) As Dictionary(Of String, Object)
            Dim Value As New Dictionary(Of String, Object)
            Dim type As Type = Obj.[GetType]()
            Dim properties As PropertyInfo() = type.GetProperties(ReflectionFlags)

            For Each p As PropertyInfo In properties
                Dim v As Object = p.GetValue(Obj, Nothing)
                If v IsNot Nothing AndAlso (IsSerializable(v) OrElse Convertable(v.GetType)) Then
                    Value.Add(p.Name, GetDeserializedValue(v))
                End If
            Next
            Return Value
        End Function

        Public Shared Sub SetProperties(Obj As Object, props As Dictionary(Of String, String))

            Dim type As Type = Obj.[GetType]()
            Dim properties As PropertyInfo() = type.GetProperties(ReflectionFlags)

            For Each p As PropertyInfo In properties
                For Each prop In props
                    If p.Name = prop.Key Then

                    End If
                Next
                'p.SetValue(Obj,)
            Next
        End Sub

        Private Shared Function IsSerializable(obj As Object) As Boolean
            Return (TypeOf obj Is Runtime.Serialization.ISerializable) OrElse (Attribute.IsDefined(obj.GetType, GetType(SerializableAttribute)))
        End Function
        Public Shared Function Convertable(T As Type) As Boolean
            For Each T2 As Type In SerializationConverters.Keys
                Dim isSubClass As Boolean = T.IsSubclassOf(T2)
                Dim isBaseConvertable As Boolean = T Is T2.BaseType
                Dim isAssignable As Boolean = T.IsAssignableFrom(T2)
                If isSubClass Or isAssignable Or isBaseConvertable Then
                    Return True
                End If
            Next
            Return False
        End Function

        Public Shared Function TryConvertObject(Obj As Object) As Object
            Dim T As Type = Obj.GetType
            For Each T2 As Type In SerializationConverters.Keys
                Dim isSubClass As Boolean = T.IsSubclassOf(T2)
                Dim isAssignable As Boolean = T.IsAssignableFrom(T2)
                If isSubClass Or isAssignable Then
                    ' if can be serialized, then call the serializer function associated with the type
                    Return SerializationConverters(T2).Invoke(Obj)
                End If
            Next
            Return Obj
        End Function
        Public Shared Function GetSerializedValue(Obj As Object) As Object
            Return TryConvertObject(Obj)
        End Function

        Public Shared Function GetDeserializedValue(Str As String) As Object
            Return Newtonsoft.Json.JsonConvert.DeserializeObject(Str)
        End Function

    End Class

End Namespace
