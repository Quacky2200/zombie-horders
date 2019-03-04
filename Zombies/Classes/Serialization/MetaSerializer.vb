Option Strict Off

Imports System.Collections.Generic
Imports Newtonsoft.Json
'Imports ProjectZ.Shared.Drawing
'Imports ProjectZ.Shared.Drawing.UI

Namespace [Shared].Serialization

    Public Class MetaSerializer

        Public Shared Property Settings As New JsonSerializerSettings

        'Public Overloads Shared Function Serialize(Scene As Scene) As String
        '    Return Serialize(New MetaData(Scene))
        'End Function

        'Public Overloads Shared Function Serialize(SceneElement As SceneElement) As String
        '    Return Serialize(New MetaData(SceneElement))
        'End Function

        Public Overloads Shared Function Serialize(Obj As Object) As String
            Return JsonConvert.SerializeObject(Obj, Settings)
        End Function

        Public Overloads Shared Function Deserialize(Of T)(JSON As String) As Object
            Dim MetaData As MetaData = JsonConvert.DeserializeObject(Of MetaData)(JSON, Settings)
            Dim NewScene As Object = Activator.CreateInstance(Of T)()
            ObjectConverter.DeserializeProperties(MetaData)

            Return NewScene
        End Function

        'Public Overloads Shared Function DeserializeScene(Of T)(ByRef SceneManager As SceneManager, JSON As String) As T
        '    Dim MetaData As MetaData = JsonConvert.DeserializeObject(Of MetaData)(JSON, Settings)
        '    Dim NewScene As Object = Activator.CreateInstance(Of T)
        '    Dim dict As Dictionary(Of String, Object) = ObjectConverter.DeserializeProperties(MetaData)
        '    NewScene.InitialConstructor(SceneManager)
        '    Return NewScene
        'End Function

        'Public Overloads Shared Function DeserializeScene(ByRef SceneManager As SceneManager, JSON As String) As Scene
        '    Dim MetaData As MetaData = JsonConvert.DeserializeObject(Of MetaData)(JSON, Settings)
        '    Dim sType As Type = ObjectConverter.DeserializationTypes(MetaData.DataType)
        '    Dim NewScene As Scene = Activator.CreateInstance(sType)
        '    Dim dict As Dictionary(Of String, Object) = ObjectConverter.DeserializeProperties(MetaData)
        '    NewScene.InitialConstructor(SceneManager)
        '    Return NewScene
        'End Function

        'Public Shared Function DeserializeSceneElement(Of T)(JSON As String) As T
        '    Dim MetaData As MetaData = JsonConvert.DeserializeObject(Of MetaData)(JSON, Settings)
        '    Dim NewScene As Object = Activator.CreateInstance(Of T)
        '    ObjectConverter.DeserializeProperties(MetaData)

        '    Return NewScene
        'End Function

    End Class

End Namespace