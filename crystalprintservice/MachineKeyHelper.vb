Imports System.Text
Imports System.Web.Security
Imports System.IO
Public Class MachineKeyHelper
    Dim tmpp As String
    Private Shared ReadOnly purpose As String() = {"DBCredentials"}

    ' 🔒 Encrypt string → Base64
    Public Shared Function EncryptString(plainText As String) As String

        Dim bytes As Byte() = Encoding.UTF8.GetBytes(plainText)

        Dim protectedBytes As Byte() = MachineKey.Protect(bytes, purpose)

        Return Convert.ToBase64String(protectedBytes)

    End Function

    ' 🔓 Decrypt Base64 → string
    Public Shared Function DecryptString(cipherText As String) As String

        Dim protectedBytes As Byte() = Convert.FromBase64String(cipherText)

        Dim bytes As Byte() = MachineKey.Unprotect(protectedBytes, purpose)

        Return Encoding.UTF8.GetString(bytes)

    End Function




End Class
