Public Class PrintRequest
    Public Property ReportName As String
    Public Property PrinterName As String

    Public Property UseDB As Boolean
    Public Property ServerName As String
    Public Property DatabaseName As String
    Public Property DBUser As String
    Public Property DBPassword As String
    Public Property Parameters As Dictionary(Of String, Object)
End Class
