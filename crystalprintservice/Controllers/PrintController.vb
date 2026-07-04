Imports System.Web.Http
'Imports System.Web.Mvc
Imports CrystalDecisions.CrystalReports.Engine
Imports CrystalDecisions.Shared
Imports System.Net
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports QRCoder
Imports System.IO
Imports System.Drawing
Imports System.Text
'Imports iTextSharp.text
'Imports iTextSharp.text.pdf
'Imports iTextSharp.text.pdf.security
'Imports Org.BouncyCastle.X509
'Imports System.Security.Cryptography.X509Certificates




Namespace Controllers
    Public Class PrintController
        Inherits ApiController

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
        Dim tmpp As String
        <System.Web.Http.HttpPost>
        Function PrintReportprn(req As PrintRequest) As IHttpActionResult
            Try
                ' 1. Report path (D: drive – Wine safe)
                Dim rptPath As String = "D:\Reports\" & req.ReportName & ".rpt"
                If Not System.IO.File.Exists(rptPath) Then
                    Return Json(New With {.Status = "Error", .Message = "Report not found"})
                    'Return Content(HttpStatusCode.NotFound, New With {.Status = "Error", .Message = "Report not found"})
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


                ' 5. GENERATE QR CODE ONLY IF TEXT EXISTS
                ' ------------------------------------------------

                'If Not String.IsNullOrEmpty(req.QRText) Then

                '    Dim qrBytes As Byte() = GenerateQRCode(req.QRText)

                '    Dim ms As New MemoryStream(qrBytes)

                '    rpt.SetParameterValue("QRCodeImage", ms)

                'End If


                If Not String.IsNullOrEmpty(req.QRText) Then

                    Dim qrBitmap As Bitmap = GenerateQRCodeImage(req.QRText)

                    rpt.SetParameterValue("Qrpath", qrBitmap)
                Else
                    rpt.SetParameterValue("Qrpath", Nothing)

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



        <System.Web.Http.HttpPost>
        Function PrintReportold(req As PrintRequest) As IHttpActionResult
            Try
                ' 1. Report path
                Dim rptPath As String = "D:\Reports\" & req.ReportName & ".rpt"
                If Not System.IO.File.Exists(rptPath) Then
                    Return Json(New With {.Status = "Error", .Message = "Report not found"})
                End If

                ' 2. Load report
                Dim rpt As New ReportDocument()
                rpt.Load(rptPath)

                ' 3. DB login
                If req.UseDB Then
                    CrystalReportLogOn(rpt, req.ServerName, req.DatabaseName, req.DBUser, req.DBPassword)
                End If

                ' 4. Parameters
                If req.Parameters IsNot Nothing Then
                    For Each p In req.Parameters

                        'rpt.SetParameterValue(p.Key, p.Value)
                        Dim pf As ParameterFieldDefinition = rpt.DataDefinition.ParameterFields(p.Key)

                        ' Clear old values
                        pf.CurrentValues.Clear()

                        ' Create discrete value
                        Dim dv As New ParameterDiscreteValue()

                        ' LET CRYSTAL HANDLE THE TYPE
                        dv.Value = p.Value

                        ' Apply
                        pf.CurrentValues.Add(dv)
                        rpt.DataDefinition.ParameterFields(p.Key).ApplyCurrentValues(pf.CurrentValues)


                    Next
                End If

                ' 5. EXPORT TO PDF
                Dim pdfDir As String = "D:\Pdf\"
                If Not System.IO.Directory.Exists(pdfDir) Then
                    System.IO.Directory.CreateDirectory(pdfDir)
                End If

                Dim pdfFile As String = pdfDir & req.ReportName & "_" &
               DateTime.Now.ToString("yyyyMMdd_HHmmss") & ".pdf"

                rpt.ExportToDisk(ExportFormatType.PortableDocFormat, pdfFile)

                ' 6. OPTIONAL: Print
                If Not String.IsNullOrEmpty(req.PrinterName) Then
                    rpt.PrintOptions.PrinterName = req.PrinterName
                    rpt.PrintToPrinter(1, False, 0, 0)
                End If

                ' 7. Cleanup
                rpt.Close()
                rpt.Dispose()

                Return Json(New With {
            .Status = "Success",
            .Message = "PDF exported successfully",
            .PdfPath = pdfFile
        })

            Catch ex As Exception
                Return Json(New With {.Status = "Error", .Message = ex.ToString()})
            End Try
        End Function

        Private Function GenerateQRCode(text As String) As Byte()

            Dim qrGen As New QRCodeGenerator()
            Dim qrData As QRCodeData = qrGen.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q)

            Dim qrCode As New BitmapByteQRCode(qrData)

            Dim qrBytes As Byte() = qrCode.GetGraphic(2)

            qrGen.Dispose()

            Return qrBytes

        End Function

        Private Function GenerateQRCodeImage(text As String) As Bitmap

            Dim qrGen As New QRCodeGenerator()

            Dim qrData As QRCodeData =
        qrGen.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q)

            Dim qrCode As New QRCode(qrData)

            Dim qrImage As Bitmap = qrCode.GetGraphic(2)

            qrGen.Dispose()

            Return qrImage

        End Function



        <System.Web.Http.HttpPost>
        Function PrintReportprevold(req As PrintRequest) As HttpResponseMessage
            Dim qrFile As String = ""
            Dim invno As String = ""
            Dim rpt As ReportDocument = Nothing

            Try
                ' 1. Report path
                Dim rptPath As String = "D:\Reports\" & req.ReportName & ".rpt"
                If Not System.IO.File.Exists(rptPath) Then
                    Return New HttpResponseMessage(HttpStatusCode.NotFound)
                End If

                ' 2. Load report
                rpt = New ReportDocument()
                rpt.Load(rptPath)

                ' 3. DB login
                If req.UseDB Then
                    CrystalReportLogOn(rpt, req.ServerName, req.DatabaseName, req.DBUser, req.DBPassword)
                End If

                ' 4. Parameters
                'If req.Parameters IsNot Nothing Then
                '    For Each p In req.Parameters
                '        Dim pf = rpt.DataDefinition.ParameterFields(p.Key)
                '        pf.CurrentValues.Clear()

                '        Dim dv As New ParameterDiscreteValue()
                '        dv.Value = p.Value
                '        pf.CurrentValues.Add(dv)
                '        pf.ApplyCurrentValues(pf.CurrentValues)
                '    Next
                'End If


                If req.Parameters IsNot Nothing Then

                    For Each p In req.Parameters

                        Dim pf As ParameterFieldDefinition = Nothing

                        Try
                            pf = rpt.DataDefinition.ParameterFields(p.Key)
                        Catch ex As Exception
                            pf = Nothing
                        End Try

                        If pf IsNot Nothing Then

                            pf.CurrentValues.Clear()
                            Dim dv As New ParameterDiscreteValue()
                            dv.Value = p.Value
                            pf.CurrentValues.Add(dv)
                            pf.ApplyCurrentValues(pf.CurrentValues)

                        End If

                    Next

                End If



                If Not String.IsNullOrEmpty(req.QRText) Then
                    invno = Convert.ToInt32(req.Parameters("Dockey@")).ToString
                    Dim qrBitmap As Bitmap = GenerateQRCodeImage(req.QRText)
                    'Dim qrFile As String = "D:\Pdf\qr_" & Guid.NewGuid().ToString() & ".png"
                    qrFile = "D:\Pdf\qrcode" & invno.Trim & ".png"

                    'qrFile = "D:\Pdf\qrcode.png"

                    qrBitmap.Save(qrFile, Imaging.ImageFormat.Png)

                    rpt.SetParameterValue("Qrpath", qrFile)
                    qrBitmap.Dispose()
                End If

                'If Not String.IsNullOrEmpty(req.QRText) Then
                '    Dim qrBitmap As Bitmap = GenerateQRCodeImage(req.QRText)

                '    Dim msQR As New MemoryStream()
                '    qrBitmap.Save(msQR, Imaging.ImageFormat.Png)
                '    Dim qrBytes As Byte() = msQR.ToArray()
                '    Dim qrBase64 As String = Convert.ToBase64String(qrBytes)
                '    rpt.SetParameterValue("Qrpath", qrBase64)

                'End If




                invno = Convert.ToInt32(req.Parameters("Dockey@")).ToString
                'Dim finyr As String = req.Parameters("finyr@")

                ' 5. PDF folder
                Dim pdfDir As String = "D:\Pdf\"
                If Not System.IO.Directory.Exists(pdfDir) Then
                    System.IO.Directory.CreateDirectory(pdfDir)
                End If

                'Dim pdfFile As String = pdfDir & req.ReportName & "_" &
                '        DateTime.Now.ToString("yyyyMMdd_HHmmss") & ".pdf"

                'Dim pdfFile As String = pdfDir & req.ReportName & "_" & invno.ToString.Trim & "_" & finyr.Trim & ".pdf"

                Dim pdfFile As String = pdfDir & req.ReportName & "-" & invno.Trim & ".pdf"


                'rpt.Refresh()
                'rpt.VerifyDatabase()

                ' ------------------------------------------------
                ' EXPORT PDF ALWAYS
                ' ------------------------------------------------
                rpt.ExportToDisk(ExportFormatType.PortableDocFormat, pdfFile)


                'If req.UseDigitalSign Then

                '    Dim invno As Integer = Convert.ToInt32(req.Parameters("InvNo"))

                '    Dim finyr As String = req.Parameters("FinYr").ToString()

                '    Dim invtype As String = req.Parameters("InvType").ToString()



                '    Dim signedPdf As String = pdfFile.Replace(".pdf", "_signed.pdf")

                '    'SignInvoicePdf(pdfFile, signedPdf, "INV", 10025, "2025-26")

                '    SignInvoicePdf(pdfFile, signedPdf, invtype, invno, finyr)

                '    pdfFile = signedPdf

                'End If

                Dim pdfBytes As Byte() = System.IO.File.ReadAllBytes(pdfFile)

                rpt.Close()
                rpt.Dispose()
                ' ------------------------------------------------
                ' PRINT IF PRINTER PROVIDED
                ' ------------------------------------------------
                Dim response As New HttpResponseMessage(HttpStatusCode.OK)
                response.Content = New ByteArrayContent(pdfBytes)
                response.Content.Headers.ContentType = New Headers.MediaTypeHeaderValue("application/pdf")


                response.Headers.CacheControl = New Headers.CacheControlHeaderValue() With {
                .NoCache = True,
                .NoStore = True,
                .MustRevalidate = True
                }

                'response.Headers.Pragma.Add(New Headers.NameValueHeaderValue("Pragma", "no-cache"))
                'response.Headers.Add("Expires", "0")

                response.Content.Headers.Expires = DateTimeOffset.UtcNow.AddSeconds(-1)

                ' ------------------------------------------------
                If Not String.IsNullOrEmpty(req.PrinterName) Then

                    response.Content.Headers.ContentDisposition = New Headers.ContentDispositionHeaderValue("inline") With {.FileName = System.IO.Path.GetFileName(pdfFile)}

                Else

                    If req.DigitalSign Then
                        response.Content.Headers.ContentDisposition = New Headers.ContentDispositionHeaderValue("attachment") With {.FileName = System.IO.Path.GetFileName(pdfFile)}

                    Else
                        response.Content.Headers.ContentDisposition = New Headers.ContentDispositionHeaderValue("inline") With {.FileName = System.IO.Path.GetFileName(pdfFile)}

                    End If
                End If

                'Try
                '    If System.IO.File.Exists(qrFile) Then
                '        System.IO.File.Delete(qrFile)
                '    End If

                'Catch
                'End Try

                Return response

            Catch ex As Exception
                If rpt IsNot Nothing Then
                    rpt.Close()
                    rpt.Dispose()
                End If

                Return New HttpResponseMessage(HttpStatusCode.InternalServerError) With {
            .Content = New StringContent(ex.Message)
        }
            End Try
        End Function

        <System.Web.Http.HttpPost>
        Function PrintReportprev(req As PrintRequest) As HttpResponseMessage

            Dim qrFile As String = ""
            Dim invno As String = ""
            Dim rpt As ReportDocument = Nothing

            Try
                ' 1. Report path
                ' Dim rptPath As String = "D:\Reports\" & req.ReportName & ".rpt"
                Dim basePath As String = ConfigurationManager.AppSettings("ReportPath")
                If String.IsNullOrWhiteSpace(basePath) Then
                    Throw New Exception("ReportPath missing in web.config")
                End If

                If String.IsNullOrWhiteSpace(req.ReportName) Then
                    Return New HttpResponseMessage(HttpStatusCode.BadRequest) With {
                    .Content = New StringContent("ReportName missing")
                    }
                End If


                Dim safeReportName As String = Path.GetFileName(req.ReportName)

                Dim rptPath As String = Path.Combine(basePath, safeReportName & ".rpt")


                'Dim rptPath As String = Trim(basePath) & req.ReportName & ".rpt"

                If Not System.IO.File.Exists(rptPath) Then
                    Return New HttpResponseMessage(HttpStatusCode.NotFound)
                End If

                ' 2. Load report
                rpt = New ReportDocument()
                rpt.Load(rptPath)
                rpt.Refresh()


                ' 3. DB login
                If req.UseDB Then
                    'CrystalReportLogOn(rpt, req.ServerName, req.DatabaseName, req.DBUser, req.DBPassword)
                    Dim dbUser As String = ""
                    Dim dbPass As String = ""
                    Dim servername As String = ""
                    req.ServerName = ConfigurationManager.AppSettings("Servername")
                    req.DBUser = ConfigurationManager.AppSettings("dbuser")
                    'req.DBPassword = decodefile(ConfigurationManager.AppSettings("dbpassword"))
                    Try
                        'servername = req.ServerName
                        dbUser = req.DBUser
                        'dbPass = MachineKeyHelper.DecryptString(req.DBPassword)
                        Dim encPass As String = ConfigurationManager.AppSettings("dbpassword")
                        dbPass = CryptoHelper.Decrypt(encPass)


                    Catch
                        Return New HttpResponseMessage(HttpStatusCode.BadRequest) With {
                        .Content = New StringContent("Invalid encrypted DB credentials")
                    }
                    End Try

                    CrystalReportLogOn(rpt, req.ServerName, req.DatabaseName, dbUser, dbPass)


                End If

                ' 4. Parameters
                If req.Parameters IsNot Nothing Then
                    For Each p In req.Parameters

                        Dim pf As ParameterFieldDefinition = Nothing

                        Try
                            pf = rpt.DataDefinition.ParameterFields(p.Key)
                        Catch
                            pf = Nothing
                        End Try

                        If pf IsNot Nothing Then
                            pf.CurrentValues.Clear()

                            Dim dv As New ParameterDiscreteValue()
                            dv.Value = p.Value

                            pf.CurrentValues.Add(dv)
                            pf.ApplyCurrentValues(pf.CurrentValues)
                        End If

                    Next
                End If


                'If req.Parameters IsNot Nothing Then
                '    For Each p In req.Parameters
                '        Try
                '            rpt.SetParameterValue(p.Key, p.Value)
                '        Catch
                '        End Try
                '    Next
                'End If




                ' 5. QR Code (temporary file only if needed)
                'invno = Convert.ToInt32(req.Parameters("Dockey@")).ToString

                'If Not String.IsNullOrEmpty(req.QRText) Then

                '    'invno = Convert.ToInt32(req.Parameters("Dockey@")).ToString

                '    Dim qrBitmap As Bitmap = GenerateQRCodeImage(req.QRText)

                '    'qrFile = System.IO.Path.GetTempPath() & "qr_" & Guid.NewGuid().ToString() & ".png"

                '    'qrFile = "D:\Pdf\qrcode" & invno.Trim & ".png"

                '    qrFile = Path.Combine(ConfigurationManager.AppSettings("QRPath"),
                '                      "qr_" & invno.Trim & ".png")

                '    qrBitmap.Save(qrFile, Imaging.ImageFormat.Png)

                '    rpt.SetParameterValue("Qrpath", qrFile)

                '    qrBitmap.Dispose()



                'End If

                If Not String.IsNullOrWhiteSpace(req.QRText) Then

                    '  SAFE PARAMETER CHECK
                    If req.Parameters Is Nothing OrElse Not req.Parameters.ContainsKey("Dockey@") Then
                        Return New HttpResponseMessage(HttpStatusCode.BadRequest) With {
                        .Content = New StringContent("Missing Dockey@ parameter")
                        }
                    End If
                    If req.Parameters IsNot Nothing AndAlso req.Parameters.ContainsKey("Dockey@") Then
                        invno = req.Parameters("Dockey@").ToString()
                    End If
                    'invno = Convert.ToInt32(req.Parameters("Dockey@")).ToString()

                    Dim qrBitmap As Bitmap = GenerateQRCodeImage(req.QRText)

                    qrFile = Path.Combine(ConfigurationManager.AppSettings("QRPath"), "qr_" & invno.Trim & ".png")

                    qrBitmap.Save(qrFile, Imaging.ImageFormat.Png)

                    rpt.SetParameterValue("Qrpath", qrFile)

                    qrBitmap.Dispose()

                End If





                'invno = Convert.ToInt32(req.Parameters("Dockey@")).ToString

                ' ------------------------------------------------
                ' ✅ EXPORT TO MEMORY (NO FILE SAVE)
                ' ------------------------------------------------
                Dim stream As System.IO.Stream = rpt.ExportToStream(ExportFormatType.PortableDocFormat)

                Dim memoryStream As New System.IO.MemoryStream()
                stream.CopyTo(memoryStream)

                Dim pdfBytes As Byte() = memoryStream.ToArray()

                ' Cleanup
                memoryStream.Close()
                stream.Close()

                rpt.Close()
                rpt.Dispose()

                ' Delete QR temp file
                If Not String.IsNullOrEmpty(qrFile) AndAlso System.IO.File.Exists(qrFile) Then
                    Try
                        System.IO.File.Delete(qrFile)
                    Catch
                    End Try
                End If

                ' ------------------------------------------------
                ' RESPONSE
                ' ------------------------------------------------
                Dim response As New HttpResponseMessage(HttpStatusCode.OK)

                response.Content = New ByteArrayContent(pdfBytes)
                response.Content.Headers.ContentType = New Headers.MediaTypeHeaderValue("application/pdf")

                response.Headers.CacheControl = New Headers.CacheControlHeaderValue() With {
            .NoCache = True,
            .NoStore = True,
            .MustRevalidate = True
        }

                response.Content.Headers.Expires = DateTimeOffset.UtcNow.AddSeconds(-1)

                ' File name only (no physical file)
                Dim fileName As String = req.ReportName & "-" & invno & ".pdf"

                If Not String.IsNullOrEmpty(req.PrinterName) Then
                    response.Content.Headers.ContentDisposition =
                New Headers.ContentDispositionHeaderValue("inline") With {.FileName = fileName}
                Else
                    If req.DigitalSign Then
                        response.Content.Headers.ContentDisposition =
                    New Headers.ContentDispositionHeaderValue("attachment") With {.FileName = fileName}
                    Else
                        response.Content.Headers.ContentDisposition =
                    New Headers.ContentDispositionHeaderValue("inline") With {.FileName = fileName}
                    End If
                End If

                Return response

            Catch ex As Exception

                If rpt IsNot Nothing Then
                    rpt.Close()
                    rpt.Dispose()
                End If

                Dim fullError As String = ex.ToString()

                'System.IO.File.WriteAllText("D:\api_error.txt", fullError)

                Dim logFile As String = "D:\api_error.txt"

                System.IO.File.AppendAllText(logFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & Environment.NewLine & fullError & Environment.NewLine &
                "--------------------------------------------------------" & Environment.NewLine)



                Return New HttpResponseMessage(HttpStatusCode.InternalServerError) With {
            .Content = New StringContent(fullError)
        }

            End Try

        End Function
        <System.Web.Http.HttpPost>
        Function PrintReport(req As PrintRequest) As HttpResponseMessage

            Dim qrFile As String = ""
            Dim invno As String = ""
            Dim rpt As ReportDocument = Nothing

            Try

                '------------------------------------------------------------
                ' 1. Validate Report Path
                '------------------------------------------------------------
                Dim basePath As String = ConfigurationManager.AppSettings("ReportPath")

                If String.IsNullOrWhiteSpace(basePath) Then
                    Throw New Exception("ReportPath missing in web.config")
                End If

                If String.IsNullOrWhiteSpace(req.ReportName) Then
                    Return New HttpResponseMessage(HttpStatusCode.BadRequest) With {
                .Content = New StringContent("ReportName missing")
            }
                End If

                Dim safeReportName As String = Path.GetFileName(req.ReportName)
                Dim rptPath As String = Path.Combine(basePath, safeReportName & ".rpt")

                If Not File.Exists(rptPath) Then
                    Return New HttpResponseMessage(HttpStatusCode.NotFound) With {
                .Content = New StringContent("Report file not found.")
            }
                End If

                '------------------------------------------------------------
                ' 2. Load Crystal Report
                '------------------------------------------------------------
                rpt = New ReportDocument()

                rpt.Load(rptPath)

                'Always verify and refresh
                ' rpt.VerifyDatabase()
                'rpt.Refresh()

                'Dim s As String = ""

                'For Each pf As ParameterFieldDefinition In rpt.DataDefinition.ParameterFields
                '    s &= pf.Name & vbCrLf
                'Next

                'System.IO.File.WriteAllText("D:\ReportParams.txt", s)





                '------------------------------------------------------------
                ' 3. Database Login
                '------------------------------------------------------------
                If req.UseDB Then

                    req.ServerName = ConfigurationManager.AppSettings("Servername")
                    req.DBUser = ConfigurationManager.AppSettings("dbuser")

                    Dim dbUser As String = req.DBUser
                    Dim dbPass As String = ""

                    Try

                        Dim encPass As String = ConfigurationManager.AppSettings("dbpassword")
                        dbPass = CryptoHelper.Decrypt(encPass)

                    Catch ex As Exception

                        Return New HttpResponseMessage(HttpStatusCode.BadRequest) With {
                    .Content = New StringContent("Invalid encrypted database credentials.")
                }

                    End Try

                    'For Each tbl As CrystalDecisions.CrystalReports.Engine.Table In rpt.Database.Tables

                    '    System.IO.File.AppendAllText("D:\CrystalLocation.txt",
                    '        "Name      : " & tbl.Name & vbCrLf &
                    '        "Location  : " & tbl.Location & vbCrLf &
                    '        "Server    : " & tbl.LogOnInfo.ConnectionInfo.ServerName & vbCrLf &
                    '         "Database  : " & tbl.LogOnInfo.ConnectionInfo.DatabaseName & vbCrLf &
                    '        "User      : " & tbl.LogOnInfo.ConnectionInfo.UserID & vbCrLf &
                    '        "------------------------" & vbCrLf)

                    'Next


                    CrystalReportLogOn(rpt, req.ServerName, req.DatabaseName, dbUser, dbPass)

                    rpt.Refresh()

                    'For Each tbl As CrystalDecisions.CrystalReports.Engine.Table In rpt.Database.Tables

                    '    System.IO.File.AppendAllText(
                    '        "D:\CrystalTables.txt",
                    '        "Table : " & tbl.Name & vbCrLf &
                    '        "Location : " & tbl.Location & vbCrLf &
                    '        "------------------" & vbCrLf)

                    'Next

                End If

                '------------------------------------------------------------
                ' 4. Apply Report Parameters
                '------------------------------------------------------------
                If req.Parameters IsNot Nothing Then

                    For Each p In req.Parameters

                        Try

                            rpt.SetParameterValue(p.Key, p.Value)

                        Catch ex As Exception
                            Throw New Exception("Parameter '" & p.Key & "' : " & ex.Message)

                            'Ignore parameters that do not exist in report

                        End Try

                    Next

                End If



                'If req.Parameters IsNot Nothing Then
                '    For Each p In req.Parameters

                '        Dim pf As ParameterFieldDefinition = Nothing

                '        Try
                '            pf = rpt.DataDefinition.ParameterFields(p.Key)
                '        Catch
                '            pf = Nothing
                '        End Try

                '        If pf IsNot Nothing Then
                '            pf.CurrentValues.Clear()

                '            Dim dv As New ParameterDiscreteValue()
                '            dv.Value = p.Value

                '            pf.CurrentValues.Add(dv)
                '            pf.ApplyCurrentValues(pf.CurrentValues)
                '        End If

                '    Next
                'End If




                'Refresh report after parameters
                'rpt.Refresh()

                '------------------------------------------------------------
                ' 5. Generate QR Code (If Required)
                '------------------------------------------------------------
                If Not String.IsNullOrWhiteSpace(req.QRText) Then

                    If req.Parameters Is Nothing OrElse Not req.Parameters.ContainsKey("Dockey@") Then

                        Return New HttpResponseMessage(HttpStatusCode.BadRequest) With {
                    .Content = New StringContent("Missing Dockey@ parameter.")
                }

                    End If

                    invno = req.Parameters("Dockey@").ToString()

                    Dim qrBitmap As Bitmap = GenerateQRCodeImage(req.QRText)

                    qrFile = Path.Combine(
                        ConfigurationManager.AppSettings("QRPath"),
                        "qr_" & invno.Trim() & ".png")

                    qrBitmap.Save(qrFile, Imaging.ImageFormat.Png)

                    rpt.SetParameterValue("Qrpath", qrFile)

                    qrBitmap.Dispose()

                End If


                'Dim log As New System.Text.StringBuilder()

                'For Each pf As CrystalDecisions.CrystalReports.Engine.ParameterFieldDefinition In rpt.DataDefinition.ParameterFields

                '    log.AppendLine("Parameter : " & pf.Name)

                '    If pf.CurrentValues Is Nothing OrElse pf.CurrentValues.Count = 0 Then
                '        log.AppendLine("Value : <EMPTY>")
                '    Else
                '        For Each v As CrystalDecisions.Shared.ParameterValue In pf.CurrentValues
                '            Dim dv = TryCast(v, CrystalDecisions.Shared.ParameterDiscreteValue)
                '            If dv IsNot Nothing Then
                '                log.AppendLine("Value : " & dv.Value.ToString())
                '            End If
                '        Next
                '    End If

                '    log.AppendLine("--------------------------------")

                'Next

                'System.IO.File.WriteAllText("D:\CrystalParameterValues.txt", log.ToString())


                'Refresh once again before export
                'rpt.Refresh()

                '=========================
                'PART 2 STARTS FROM HERE
                '=========================

                '------------------------------------------------------------
                ' 6. Export Report to PDF (Memory)
                '------------------------------------------------------------
                Dim stream As System.IO.Stream = Nothing
                Dim memoryStream As MemoryStream = Nothing

                Try

                    stream = rpt.ExportToStream(ExportFormatType.PortableDocFormat)

                    memoryStream = New MemoryStream()

                    stream.CopyTo(memoryStream)

                    Dim pdfBytes As Byte() = memoryStream.ToArray()

                    '------------------------------------------------------------
                    ' Build Response
                    '------------------------------------------------------------
                    Dim response As New HttpResponseMessage(HttpStatusCode.OK)

                    response.Content = New ByteArrayContent(pdfBytes)
                    response.Content.Headers.ContentType =
                New Headers.MediaTypeHeaderValue("application/pdf")

                    response.Headers.CacheControl =
                New Headers.CacheControlHeaderValue() With {
                    .NoCache = True,
                    .NoStore = True,
                    .MustRevalidate = True
                }

                    response.Content.Headers.Expires = DateTimeOffset.UtcNow.AddSeconds(-1)

                    Dim fileName As String = req.ReportName

                    If Not String.IsNullOrWhiteSpace(invno) Then
                        fileName &= "-" & invno
                    End If

                    fileName &= ".pdf"

                    If Not String.IsNullOrWhiteSpace(req.PrinterName) Then


                        'rpt.PrintOptions.PrinterName = req.PrinterName

                        'rpt.PrintToPrinter(1, False, 0, 0)

                        'Return New HttpResponseMessage(HttpStatusCode.OK) With {
                        '.Content = New StringContent("Printed Successfully")
                        ' }




                        response.Content.Headers.ContentDisposition =
                    New Headers.ContentDispositionHeaderValue("inline") With {
                        .FileName = fileName
                    }

                    Else

                        If req.DigitalSign Then

                            response.Content.Headers.ContentDisposition =
                        New Headers.ContentDispositionHeaderValue("attachment") With {
                            .FileName = fileName
                        }

                        Else

                            response.Content.Headers.ContentDisposition =
                        New Headers.ContentDispositionHeaderValue("inline") With {
                            .FileName = fileName
                        }

                        End If

                    End If

                    Return response

                Finally

                    '------------------------------------------------------------
                    ' Close Streams
                    '------------------------------------------------------------
                    If memoryStream IsNot Nothing Then
                        memoryStream.Close()
                        memoryStream.Dispose()
                    End If

                    If stream IsNot Nothing Then
                        stream.Close()
                        stream.Dispose()
                    End If

                    '------------------------------------------------------------
                    ' Delete Temporary QR File
                    '------------------------------------------------------------
                    If Not String.IsNullOrWhiteSpace(qrFile) AndAlso
               File.Exists(qrFile) Then

                        Try
                            File.Delete(qrFile)
                        Catch
                            ' Ignore delete failure
                        End Try

                    End If

                    '------------------------------------------------------------
                    ' Crystal Cleanup
                    '------------------------------------------------------------
                    If rpt IsNot Nothing Then

                        Try
                            rpt.Close()
                            rpt.Dispose()
                        Catch
                        End Try

                    End If

                    GC.Collect()
                    GC.WaitForPendingFinalizers()
                    GC.Collect()

                End Try

            Catch ex As Exception

                If rpt IsNot Nothing Then

                    Try
                        rpt.Close()
                        rpt.Dispose()
                    Catch
                    End Try

                End If

                Try

                    If Not String.IsNullOrWhiteSpace(qrFile) AndAlso
               File.Exists(qrFile) Then

                        File.Delete(qrFile)

                    End If

                Catch
                End Try

                Dim logFile As String = "D:\api_error.txt"

                File.AppendAllText(
            logFile,
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") &
            Environment.NewLine &
            ex.ToString() &
            Environment.NewLine &
            "--------------------------------------------------------" &
            Environment.NewLine)

                GC.Collect()
                GC.WaitForPendingFinalizers()
                GC.Collect()

                Return New HttpResponseMessage(HttpStatusCode.InternalServerError) With {
            .Content = New StringContent(ex.ToString())
        }

            End Try

        End Function

        Public Sub CrystalReportLogOnold(ByVal reportParameters As ReportDocument, ByVal serverName As String, ByVal databaseName As String, ByVal userName As String, ByVal password As String)
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
            For Each sect As CrystalDecisions.CrystalReports.Engine.Section In sects
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


        Public Sub CrystalReportLogOn(rpt As ReportDocument, serverName As String, databaseName As String, userName As String, password As String)

            Dim ci As New CrystalDecisions.Shared.ConnectionInfo()
            ci.ServerName = serverName
            ci.DatabaseName = databaseName
            ci.UserID = userName
            ci.Password = password
            ci.IntegratedSecurity = False   ' VERY IMPORTANT



            ' -------- MAIN REPORT TABLES --------
            For Each tbl As CrystalDecisions.CrystalReports.Engine.Table In rpt.Database.Tables
                Dim logonInfo As TableLogOnInfo = tbl.LogOnInfo
                logonInfo.ConnectionInfo = ci
                tbl.ApplyLogOnInfo(logonInfo)

                'Dim parts = tbl.Location.Split("."c)
                'Dim tableName = parts(parts.Length - 1)
                '' MUST reset location
                ''tbl.Location = databaseName & ".dbo." & tbl.Name
                'tbl.Location = databaseName & ".dbo." & tableName

                'new
                'Dim tableName As String = tbl.Location

                'If tableName.Contains(".") Then
                '    tableName = tableName.Split("."c).Last()
                'End If
                'tbl.Location = databaseName & ".dbo." & tableName

                If tbl.TestConnectivity() Then

                    If Not tbl.Location.Trim().StartsWith("Command", StringComparison.OrdinalIgnoreCase) Then

                        'If this is a normal table
                        If tbl.Location.Contains(".") Then
                            Dim tableName As String = tbl.Location.Split("."c).Last()
                            tbl.Location = databaseName & ".dbo." & tableName
                        End If

                    End If

                End If


            Next

            ' -------- SUBREPORTS --------
            For Each sec As CrystalDecisions.CrystalReports.Engine.Section In rpt.ReportDefinition.Sections
                For Each obj As ReportObject In sec.ReportObjects
                    If obj.Kind = ReportObjectKind.SubreportObject Then
                        Dim subObj As SubreportObject = CType(obj, SubreportObject)
                        Dim subRpt As ReportDocument = subObj.OpenSubreport(subObj.SubreportName)

                        For Each tbl As CrystalDecisions.CrystalReports.Engine.Table In subRpt.Database.Tables
                            Dim logonInfo As TableLogOnInfo = tbl.LogOnInfo
                            logonInfo.ConnectionInfo = ci
                            tbl.ApplyLogOnInfo(logonInfo)

                            'Dim parts = tbl.Location.Split("."c)
                            'Dim tableName = parts(parts.Length - 1)
                            ''tbl.Location = databaseName & ".dbo." & tbl.Name
                            'tbl.Location = databaseName & ".dbo." & tableName
                            'tbl.Location = tbl.Location.Substring(tbl.Location.LastIndexOf(".") + 1)

                            'new
                            'Dim tableName As String = tbl.Location

                            'If tableName.Contains(".") Then
                            '    tableName = tableName.Split("."c).Last()
                            'End If
                            'tbl.Location = databaseName & ".dbo." & tableName


                            If tbl.TestConnectivity() Then

                                If Not tbl.Location.Trim().StartsWith("Command", StringComparison.OrdinalIgnoreCase) Then

                                    'If this is a normal table
                                    If tbl.Location.Contains(".") Then
                                        Dim tableName As String = tbl.Location.Split("."c).Last()
                                        tbl.Location = databaseName & ".dbo." & tableName
                                    End If

                                End If

                            End If


                        Next
                    End If
                Next
            Next

            ' -------- FORCE LOGON --------
            rpt.SetDatabaseLogon(userName, password, serverName, databaseName)

            'rpt.VerifyDatabase()
        End Sub



        Private Sub ApplyConnection(tables As Tables,
                            ci As ConnectionInfo,
                            databaseName As String)

            For Each tbl As CrystalDecisions.CrystalReports.Engine.Table In tables

                Dim logonInfo As TableLogOnInfo = tbl.LogOnInfo
                logonInfo.ConnectionInfo = ci
                tbl.ApplyLogOnInfo(logonInfo)

                Dim loc As String = tbl.Location.ToLower()

                'Don't change location for Stored Procedures or Commands
                If loc.Contains("command") _
                        OrElse loc.Contains("procedure") _
                        OrElse tbl.Name.StartsWith("@") Then

                    Continue For

                End If

                'Change location only for normal tables/views
                If tbl.Location.Contains(".") Then

                    Dim tableName As String = tbl.Location.Split("."c).Last()

                    tbl.Location = databaseName & ".dbo." & tableName

                End If

            Next

        End Sub


        Public Function decodefile(ByVal srcfile As String) As String

            Dim decodedBytes As Byte()
            decodedBytes = Convert.FromBase64String(Decode(srcfile))

            Dim decodedText As String
            decodedText = Encoding.UTF8.GetString(decodedBytes)
            decodefile = decodedText
        End Function

        'Sub EncodeFile(ByVal srcFile As String, ByVal destfile As String)
        Public Function encodefile(ByVal srcfile As String) As String

            Dim bytesToEncode As Byte()
            bytesToEncode = Encoding.UTF8.GetBytes(srcfile)

            Dim encodedText As String
            encodedText = Convert.ToBase64String(bytesToEncode)
            encodefile = Encript(encodedText)
        End Function

        Public Function Decode(ByVal Password As String) As String
            'Dim I As Integer
            Dim TMP As Long
            tmpp = ""
            For I = 1 To Len(Password)
                TMP = Asc(Mid(Password, I, 1))
                TMP = TMP - I
                tmpp = Trim(tmpp) & Chr(TMP)
                'Decode = Decode & Chr(TMP)
            Next I
            Decode = tmpp
            Return Decode
        End Function

        Public Function Encript(ByVal Password As String) As String
            ' Dim I As Integer
            'Dim tmpp As String

            Dim TMP As Long
            tmpp = ""
            For I = 1 To Len(Password)
                TMP = Asc(Mid(Password, I, 1))
                TMP = TMP + I
                tmpp = Trim(tmpp) + Chr(TMP)

                'Encript = Encript & Chr(TMP)
            Next I
            Encript = tmpp
            Return Encript
        End Function



        '**signedpdf

        'Public Function SignInvoicePdf(sourcePdf As String, targetPdf As String, invtype As String, invno As Integer, finyr As String) As String

        '    Try

        '        Dim certificate As X509Certificate2 = loadCertificate(mepasskey)

        '        If certificate Is Nothing Then
        '            Return "Certificate not found"
        '        End If

        '        If System.IO.File.Exists(targetPdf) Then
        '            System.IO.File.Delete(targetPdf)
        '        End If

        '        Dim status As String =
        '    SignWithThisCert(certificate, sourcePdf, targetPdf, invtype)

        '        If status <> "True" Then
        '            Return status
        '        End If

        '        ' Read signed PDF
        '        Dim filebyte As Byte() = System.IO.File.ReadAllBytes(targetPdf)

        '        ' Save in DB
        '        If chkexists(invno, finyr) = False Then

        '            Dim msql As String = "INSERT INTO Digital_Pdf(invno,invtype,createddate,finyr,isdeleted,data)    VALUES(@invno,@invtype,@createddate,@finyr,'N',@data)"

        '            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("constr").ConnectionString)

        '                Using cmd As New SqlCommand(msql, con)

        '                    cmd.Parameters.AddWithValue("@invno", invno)
        '                    cmd.Parameters.AddWithValue("@invtype", invtype)
        '                    cmd.Parameters.AddWithValue("@createddate", DateTime.Now)
        '                    cmd.Parameters.AddWithValue("@finyr", finyr)
        '                    cmd.Parameters.AddWithValue("@data", filebyte)

        '                    con.Open()
        '                    cmd.ExecuteNonQuery()

        '                End Using

        '            End Using

        '        End If

        '        If System.IO.File.Exists(sourcePdf) Then
        '            System.IO.File.Delete(sourcePdf)
        '        End If

        '        Return "Success"

        '    Catch ex As Exception
        '        Return ex.Message
        '    End Try

        'End Function

        'Private Function SignWithThisCert(cert As X509Certificate2, sourcePdf As String, destPdf As String, invtype As String) As String

        '    Try

        '        If cert Is Nothing Then
        '            Return "Certificate Failed"
        '        End If

        '        Dim certParser As New Org.BouncyCastle.X509.X509CertificateParser()

        '        Dim chain() As Org.BouncyCastle.X509.X509Certificate =
        '            {certParser.ReadCertificate(cert.RawData)}

        '        Dim externalSignature As IExternalSignature =
        '            New X509Certificate2Signature(cert, DigestAlgorithms.SHA256)

        '        Dim reader As New PdfReader(sourcePdf)

        '        Using signedPdf As New FileStream(destPdf, FileMode.Create)

        '            Dim stamper = PdfStamper.CreateSignature(reader, signedPdf, vbNullChar)

        '            Dim appearance = stamper.SignatureAppearance

        '            appearance.SignDate = DateTime.Now
        '            appearance.Acro6Layers = False
        '            appearance.Layer4Text = PdfSignatureAppearance.questionMark

        '            If invtype.ToUpper() = "CRN" Or invtype.ToUpper() = "CRNOTE" Then

        '                appearance.SetVisibleSignature(New iTextSharp.text.Rectangle(440, 80, 590, 55), reader.NumberOfPages, Nothing)

        '            Else

        '                appearance.SetVisibleSignature(New iTextSharp.text.Rectangle(440, 64, 575, 24), reader.NumberOfPages, "Signature")

        '            End If

        '            appearance.SignatureRenderingMode = PdfSignatureAppearance.RenderingMode.DESCRIPTION

        '            MakeSignature.SignDetached(appearance, externalSignature, chain, Nothing, Nothing, Nothing, 0, CryptoStandard.CMS)

        '        End Using

        '        reader.Close()

        '        Return "True"

        '    Catch ex As Exception
        '        If System.IO.File.Exists(destPdf) Then
        '            System.IO.File.Delete(destPdf)
        '        End If

        '        Return ex.Message
        '    End Try

        'End Function

        'Private Function loadCertificate(serialNo As String) As X509Certificate2

        '    Dim store As New X509Store(StoreLocation.CurrentUser)

        '    store.Open(OpenFlags.ReadOnly)

        '    Dim certCollection =
        '        store.Certificates.Find(
        '            X509FindType.FindBySerialNumber,
        '            serialNo,
        '            False)

        '    If certCollection.Count > 0 Then
        '        Return certCollection(0)
        '    End If

        '    store.Close()

        '    Return Nothing

        'End Function








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


    '    {
    '  "ReportName": "SalesReport",
    '  "PrinterName": "HP LaserJet",
    '  "UseDB": true,
    '  "ServerName": "SQLSERVER01",
    '  "DatabaseName": "SalesDB",
    '  "DBUser": "sa",
    '  "DBPassword": "password",
    '  "Parameters": {
    '    "Dockey@": 1,
    '    "finyr@": "2025-26",
    '    "Qrpath": 1001
    '  }
    '}

    'pdf
    '    {
    '  "ReportName": "SalesReport",
    '  "PrinterName": "",
    '  "UseDB": false,
    '  "Parameters": {
    '    "InvoiceNo": "INV-10025"
    '  }
    '}

    'usage
    'http://localhost/crystalprintservice/api/Print/PrintReport

    '    {
    '  "ReportName": "TharunReports",
    '  "PrinterName": "",
    '  "UseDB": false,
    '  "Parameters": {
    '    "@fromdate": "2025-04-01",
    '    "@todate": "2026-01-16"
    '  }
    '}


    '*****************
    '    {
    ' "ReportName":"Invoice",
    ' "PrinterName":"TSC Printer",
    ' "QRText":"SU5WOjEwMDI1fERBVEU6MTItMDMtMjAyNnxBTVQ6MTIwMDA=",
    ' "UseDB":true,
    ' "ServerName":"SQLSERVER01",
    ' "DatabaseName":"SalesDB",
    ' "DBUser":"sa",
    ' "DBPassword":"password",
    ' "Parameters":{
    '  "FromDate":1,
    '  "ToDate":"2025-26"
    ' }
    '}

    '{
    '     "ReportName":"EINVOICETR",
    '     "PrinterName":"",
    '     "UseDB":true,
    '     "DatabaseName":"Tarinvent25",
    '     "QRText":"",
    '     "DigitalSign":false,
    '     "Parameters":{
    '       "Dockey@":1,
    '       "finyr@":"2025-26"
    '     }
    '}




End Namespace