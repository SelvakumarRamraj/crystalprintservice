Imports System.Web.Mvc
Imports CrystalDecisions.CrystalReports.Engine
Imports CrystalDecisions.Shared


Namespace Controllers
    Public Class PrintController
        Inherits Controller

        ' GET: Print
        'Function Index() As ActionResult
        '    Return View()
        'End Function

        '<HttpPost>
        'Function PrintReport(<FromBody> req As PrintRequest) As ActionResult
        '    Try
        '        ' 1. Build report path
        '        Dim rptPath As String = "D:\CrystalReports\" & req.ReportName & ".rpt"
        '        If Not IO.File.Exists(rptPath) Then
        '            Return Json(New With {.Status = "Error", .Message = "Report not found"})
        '        End If

        '        ' 2. Load report
        '        Dim rpt As New ReportDocument()
        '        rpt.Load(rptPath)

        '        ' 3. Set database login
        '        CrystalReportLogOn(rpt, req.ServerName, req.DatabaseName, req.DBUser, req.DBPassword)

        '        ' 4. Apply parameters
        '        For Each p In req.Parameters
        '            rpt.SetParameterValue(p.Key, p.Value)
        '        Next

        '        ' 5. Set printer and print
        '        rpt.PrintOptions.PrinterName = req.PrinterName
        '        rpt.PrintToPrinter(1, False, 0, 0)

        '        rpt.Close()
        '        rpt.Dispose()

        '        Return Json(New With {.Status = "Success", .Message = "Printed successfully"})
        '    Catch ex As Exception
        '        Return Json(New With {.Status = "Error", .Message = ex.Message})
        '    End Try
        'End Function

        <HttpPost>
        Function PrintReport(req As PrintRequest) As ActionResult
            Try
                ' 1. Report path (D: drive – Wine safe)
                Dim rptPath As String = "D:\CrystalReports\" & req.ReportName & ".rpt"
                If Not IO.File.Exists(rptPath) Then
                    Return Json(New With {.Status = "Error", .Message = "Report not found"})
                End If

                ' 2. Load report
                Dim rpt As New CrystalDecisions.CrystalReports.Engine.ReportDocument()
                rpt.Load(rptPath)

                ' 3. Set DB login (ONLY if required)
                If req.UseDB Then
                    CrystalReportLogOn(rpt, req.ServerName, req.DatabaseName, req.DBUser, req.DBPassword)
                End If

                ' 4. Apply parameters
                If req.Parameters IsNot Nothing Then
                    For Each p In req.Parameters
                        rpt.SetParameterValue(p.Key, p.Value)
                    Next
                End If

                ' 5. Print
                rpt.PrintOptions.PrinterName = req.PrinterName
                rpt.PrintToPrinter(1, False, 0, 0)

                rpt.Close()
                rpt.Dispose()

                Return Json(New With {.Status = "Success", .Message = "Printed successfully"})

            Catch ex As Exception
                Return Json(New With {.Status = "Error", .Message = ex.Message})
            End Try
        End Function

        <HttpPost>
        Public Function ViewReport(req As PrintRequest) As ActionResult
            Try
                Dim rptPath As String = "D:\CrystalReports\" & req.ReportName & ".rpt"
                If Not IO.File.Exists(rptPath) Then
                    Return Json(New With {.Status = "Error", .Message = "Report not found"})
                End If

                Dim rpt As New ReportDocument()
                rpt.Load(rptPath)

                ' DB login
                If req.UseDB Then
                    CrystalReportLogOn(rpt, req.ServerName, req.DatabaseName, req.DBUser, req.DBPassword)
                End If

                ' Parameters
                If req.Parameters IsNot Nothing Then
                    For Each p In req.Parameters
                        rpt.SetParameterValue(p.Key, p.Value)
                    Next
                End If

                ' Export to PDF stream
                Dim stream As IO.Stream =
            rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat)

                rpt.Close()
                rpt.Dispose()

                stream.Seek(0, IO.SeekOrigin.Begin)

                ' Return PDF to browser
                Return File(stream, "application/pdf", req.ReportName & ".pdf")

            Catch ex As Exception
                Return Json(New With {.Status = "Error", .Message = ex.Message})
            End Try
        End Function


        Public Sub CrystalReportLogOn(ByVal reportParameters As ReportDocument, ByVal serverName As String, ByVal databaseName As String, ByVal userName As String, ByVal password As String)
            Dim logOnInfo As TableLogOnInfo
            Dim subRd As ReportDocument
            Dim sects As Sections
            Dim ros As ReportObjects
            Dim sro As SubreportObject

            For Each t As CrystalDecisions.CrystalReports.Engine.Table In reportParameters.Database.Tables
                logOnInfo = t.LogOnInfo
                logOnInfo.ConnectionInfo.ServerName = serverName
                logOnInfo.ConnectionInfo.DatabaseName = databaseName
                logOnInfo.ConnectionInfo.UserID = userName
                logOnInfo.ConnectionInfo.Password = password
                t.ApplyLogOnInfo(logOnInfo)
            Next

            sects = reportParameters.ReportDefinition.Sections
            For Each sect As Section In sects
                ros = sect.ReportObjects
                For Each ro As ReportObject In ros
                    If ro.Kind = ReportObjectKind.SubreportObject Then
                        sro = DirectCast(ro, SubreportObject)
                        subRd = sro.OpenSubreport(sro.SubreportName)
                        For Each t As CrystalDecisions.CrystalReports.Engine.Table In subRd.Database.Tables
                            logOnInfo = t.LogOnInfo
                            logOnInfo.ConnectionInfo.ServerName = serverName
                            logOnInfo.ConnectionInfo.DatabaseName = databaseName
                            logOnInfo.ConnectionInfo.UserID = userName
                            logOnInfo.ConnectionInfo.Password = password
                            t.ApplyLogOnInfo(logOnInfo)
                        Next
                    End If
                Next
            Next
        End Sub



    End Class

    '**json format
    '     {
    '  "ReportName": "SalesInvoice",
    '  "PrinterName": "\\\\WINPC\\HP_LASER",
    '  "UseDB": true,
    '  "ServerName": "SQLSERVER",
    '  "DatabaseName": "ERPDB",
    '  "DBUser": "sa",
    '  "DBPassword": "password",
    '  "Parameters": {
    '    "InvoiceNo": "INV1001"
    '  }
    '}


    '     {
    '  "ReportName": "SalesInvoice",
    '  "PrinterName": "\\\\WINPC\\HP_LASER",
    '  "UseDB": false,
    '  "ServerName": "SQLSERVER",
    '  "DatabaseName": "ERPDB",
    '  "DBUser": "sa",
    '  "DBPassword": "password",
    '  "Parameters": {
    '    "InvoiceNo": "INV1001"
    '  }
    '}
End Namespace