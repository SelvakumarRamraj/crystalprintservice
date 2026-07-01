Imports System.Security.Cryptography
Imports System.Text
Imports System.IO
Imports System.Configuration
Public Class CryptoHelper
    Private Shared Function GetKey() As Byte()
        Return Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings("EncKey"))
    End Function

    Private Shared Function GetIV() As Byte()
        Return Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings("EncIV"))
    End Function

    ' 🔒 ENCRYPT
    Public Shared Function Encrypt(plainText As String) As String

        Using aes As Aes = Aes.Create()
            aes.Key = GetKey()
            aes.IV = GetIV()

            Dim encryptor = aes.CreateEncryptor()

            Using ms As New MemoryStream()
                Using cs As New CryptoStream(ms, encryptor, CryptoStreamMode.Write)
                    Using sw As New StreamWriter(cs)
                        sw.Write(plainText)
                    End Using
                End Using
                Return Convert.ToBase64String(ms.ToArray())
            End Using
        End Using

    End Function

    ' 🔓 DECRYPT
    Public Shared Function Decrypt(cipherText As String) As String

        Using aes As Aes = Aes.Create()
            aes.Key = GetKey()
            aes.IV = GetIV()

            Dim decryptor = aes.CreateDecryptor()

            Dim buffer As Byte() = Convert.FromBase64String(cipherText)

            Using ms As New MemoryStream(buffer)
                Using cs As New CryptoStream(ms, decryptor, CryptoStreamMode.Read)
                    Using sr As New StreamReader(cs)
                        Return sr.ReadToEnd()
                    End Using
                End Using
            End Using
        End Using

    End Function
End Class
