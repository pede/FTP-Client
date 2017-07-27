Imports System.Net
Imports System.IO

Public Class FTP
    Private fRequest As FtpWebRequest
    Private fHostname As String = ""
    Private fUsername As String = ""
    Private fPassword As String = ""
    Private fLastDir As String = ""
    Private fCurrentDir As String = ""

    ' Public Properties

    ''' <summary>Hostname</summary>
    ''' <remarks>ftp://ftp.example.com or ftp.example.com</remarks>
    Public Property Hostname() As String
        Get
            If fHostname.StartsWith("ftp://") Then
                Return fHostname
            Else
                Return "ftp://" & fHostname
            End If
        End Get
        Set(ByVal Value As String)
            fHostname = Value
        End Set
    End Property

    ''' <summary>Username</summary>
    ''' <remarks>If blank uses "anonymous" instead</remarks>
    Public Property Username() As String
        Get
            Return IIf(fUsername = "", "anonymous", fUsername)
        End Get
        Set(ByVal Value As String)
            fUsername = Value
        End Set
    End Property

    ''' <summary>Password</summary>
    Public Property Password() As String
        Get
            Return fPassword
        End Get
        Set(ByVal Value As String)
            fPassword = Value
        End Set
    End Property

    ''' <summary>Current Directory</summary>
    ''' <remarks>Return Directory ending with /</remarks>
    Public Property CurrentDirectory() As String
        Get
            Return fCurrentDir & CStr(IIf(fCurrentDir.EndsWith("/"), "", "/"))
        End Get
        Set(ByVal Value As String)
            If Not Value.StartsWith("/") Then _
            Throw New ApplicationException("Directory should start with /")
            fCurrentDir = Value
        End Set
    End Property

    ' Private Methods

    ''' <summary>Get FTP Login Credentials</summary>
    ''' <returns>Network Credential for FTP</returns>
    Private Function GetCredentials() As Net.ICredentials
        Return New NetworkCredential(fUsername, fPassword)
    End Function

    ''' <summary>Get FTP Request</summary>
    ''' <param name="URI">Host URI</param>
    ''' <returns>Result of FTP Request</returns>
    Private Function GetRequest(ByVal URI As String) As FtpWebRequest
        Dim Result As FtpWebRequest = CType(FtpWebRequest.Create(URI), FtpWebRequest)
        Result.Credentials = GetCredentials()
        Result.KeepAlive = False
        Return Result
    End Function

    ''' <summary>Get Response String</summary>
    ''' <param name="Request">Request for Response</param>
    ''' <returns>Get the result as string</returns>
    Private Function GetResponseString(ByVal Request As FtpWebRequest) As String
        Dim Result As String
        Using Response As FtpWebResponse = CType(Request.GetResponse, FtpWebResponse)
            Dim Size As Long = Response.ContentLength
            Using DataStream As Stream = Response.GetResponseStream
                Using sReader As New StreamReader(DataStream)
                    Result = sReader.ReadToEnd
                    sReader.Close()
                End Using
                DataStream.Close()
            End Using
            Response.Close()
        End Using
        Return Result
    End Function

    ''' <summary>Get the Size of an FTP Request</summary>
    ''' <param name="Request">FTP Request</param>
    ''' <returns>Request Data Length</returns>
    Private Function GetRequestSize(ByVal Request As FtpWebRequest) As Long
        Dim lSize As Long
        Using Response As FtpWebResponse = CType(Request.GetResponse, FtpWebResponse)
            lSize = Response.ContentLength
            Response.Close()
        End Using
        Return lSize
    End Function

    ''' <summary>Amend Path so it always starts with /</summary>
    ''' <param name="Path">Path to Amend</param>
    ''' <returns>Amended Path or orignal if not Amended</returns>
    Private Function AmendPath(ByVal Path As String) As String
        Return CStr(IIf(Path.StartsWith("/"), "", "/")) & Path
    End Function

    ''' <summary>Returns Full Path using Current Directory</summary>
    ''' <param name="File">File to Get Path From</param>
    ''' <returns>Full Path of File</returns>
    Private Function GetFullPath(ByVal File As String) As String
        If File.Contains("/") Then
            Return AmendPath(File)
        Else
            Return CurrentDirectory & File
        End If
    End Function

    ''' <summary>Get Directory</summary>
    ''' <param name="Directory">Directory to Get</param>
    ''' <returns>Directory Data</returns>
    Private Function GetDirectory(ByVal Directory As String) As String
        If Directory = "" Then
            fLastDir = CurrentDirectory
            Return Hostname & CurrentDirectory
        Else
            If Not Directory.StartsWith("/") Then _
            Throw New ApplicationException("Directory should start with /")
            fLastDir = Directory
            Return Hostname & Directory
        End If
    End Function
    Public Methods

    ''' <summary>Connection</summary>
    ''' <returns>Returns True if Connection established, False if not</returns>
    Public Function Connection() As Boolean
        If fHostname <> "" And (fUsername <> "" Or fPassword <> "") Then
            fRequest = GetRequest(Hostname)
            fRequest.Method = WebRequestMethods.Ftp.PrintWorkingDirectory
            Try
                GetResponseString(fRequest) ' Get but Ignore Response
                Return True
            Catch ex As Exception
                Return False
            End Try
        Else
            Return False
        End If
    End Function

    ''' <summary>List Directory</summary>
    ''' <param name="Directory">Directory to List (Optional)</param>
    ''' <returns>A list of Files and Foldoers as a List(of String)</returns>
    Public Function DirectoryList(Optional ByVal Directory As String = "") As List(Of String)
        Dim Result As String
        Dim lsResult As New List(Of String)
        fRequest = GetRequest(GetDirectory(Directory))
        fRequest.Method = WebRequestMethods.Ftp.ListDirectory ' Simple List
        Result = GetResponseString(fRequest) ' Get Result String
        Result = Result.Replace(vbCrLf, vbCr).TrimEnd(Chr(13))
        lsResult.AddRange(Result.Split(Chr(13))) ' Split String into List
        CurrentDirectory = Directory
        Return lsResult
    End Function

    ''' <summary>Rename Directory</summary>
    ''' <param name="SourcePath">Directory to Rename</param>
    ''' <param name="TargetDirectory">New Directory Name</param>
    ''' <returns>True if Success, False if Error/Fail</returns>
    Public Function DirectoryRename(ByVal SourcePath As String, _
                                  ByVal TargetDirectory As String) As Boolean
        Dim Source As String = Hostname & AmendPath(SourcePath)
        Dim Target As String = AmendPath(TargetDirectory)
        fRequest = GetRequest(Source)
        fRequest.Method = WebRequestMethods.Ftp.Rename
        fRequest.RenameTo = TargetDirectory
        Try
            GetResponseString(fRequest) ' Get but Ignore Response
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>Create Directory</summary>
    ''' <param name="Path">Directory to Create</param>
    ''' <returns>True if Success, False if Error/Fail</returns>
    Public Function DirectoryCreate(ByVal Path As String) As Boolean
        Dim URI As String = Hostname & CurrentDirectory & AmendPath(Path)
        fRequest = GetRequest(URI)
        fRequest.Method = WebRequestMethods.Ftp.MakeDirectory
        Try
            GetResponseString(fRequest) ' Get but Ignore Response
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>Delete Directory</summary>
    ''' <param name="Path">Directory to Delete</param>
    ''' <returns>True if Success, False if Error/Fail</returns>
    Public Function DirectoryDelete(ByVal Path As String) As Boolean
        Dim URI As String = Hostname & CurrentDirectory & AmendPath(Path)
        fRequest = GetRequest(URI)
        fRequest.Method = WebRequestMethods.Ftp.RemoveDirectory
        Try
            GetResponseString(fRequest) ' Get but Ignore Response
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>Determine Size of Remote File</summary>
    ''' <param name="Filename">Filename of File</param>
    ''' <returns>Size of File as Long</returns>
    Public Function FileSize(ByVal Filename As String) As Long
        Dim Path, URI As String
        If Filename.Contains("/") Then
            Path = AmendPath(Filename)
        Else
            Path = CurrentDirectory & Filename
        End If
        URI = Hostname & Path
        fRequest = GetRequest(URI)
        fRequest.Method = WebRequestMethods.Ftp.GetFileSize
        GetResponseString(fRequest) ' Get but Ignore Response
        Return GetRequestSize(fRequest)
    End Function

    ''' <summary>Determine of Remote File Exists</summary>
    ''' <param name="Filename">Filename of File</param>
    ''' <returns>True if Exists, False if Not</returns>
    Public Function FileExists(ByVal Filename As String) As Boolean
        Dim lSize As Long
        Try
            lSize = FileSize(Filename)
            Return True
        Catch ex As Exception ' Handle Request not Found Only
            If TypeOf (ex) Is System.Net.WebException Then
                If ex.Message.Contains("550") Then
                    Return False ' No File
                Else
                    Throw
                End If
            Else
                Throw
            End If
        End Try
    End Function

    ''' <summary>Rename Remote File</summary>
    ''' <param name="SourceFilename">File to Rename</param>
    ''' <param name="TargetFilename">New Name for File</param>
    ''' <returns>True if Success, False if Error/Fail</returns>
    Public Function FileRename(ByRef SourceFilename As String, _
                             ByVal TargetFilename As String) As Boolean
        Dim Source, Target, URI As String
        Source = GetFullPath(SourceFilename)
        If Not FileExists(Source) Then
            Throw New FileNotFoundException("File " & Source & " not found")
        End If
        Target = GetFullPath(TargetFilename)
        If Target = Source Then
            Throw New ApplicationException("Source and Target Filenames are the Same")
        ElseIf FileExists(Target) Then
            Throw New ApplicationException("Target File " & Target & " already exists")
        End If
        URI = Hostname & Source
        fRequest = GetRequest(URI)
        fRequest.Method = WebRequestMethods.Ftp.Rename
        fRequest.RenameTo = Target
        Try
            GetResponseString(fRequest) ' Get but Ignore Response
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>Delete Remote File</summary>
    ''' <param name="Filename">File do Delete</param>
    ''' <returns>True if Success, False if Error/Fail</returns>
    Public Function FileDelete(ByVal Filename As String) As Boolean
        Dim URI As String
        URI = Hostname & GetFullPath(Filename)
        fRequest = GetRequest(URI)
        fRequest.Method = WebRequestMethods.Ftp.DeleteFile
        Try
            GetResponseString(fRequest) ' Get but Ignore Response
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>Upload Local File to FTP Server</summary>
    ''' <param name="LocalFileInfo">Local File Info</param>
    ''' <param name="RemoteFilename">Target Filename (Optional)</param>
    ''' <returns>True if Success, False if Error/Fail</returns>
    Public Function Upload(ByVal LocalFileInfo As FileInfo, _
                         Optional ByVal RemoteFilename As String = "") As Boolean
        Dim Remote, URI As String
        Dim DataRead As Integer
        If RemoteFilename.Trim = "" Then ' If No RemoteFileInfo Use LocalFileInfo
            Remote = CurrentDirectory & LocalFileInfo.Name
        ElseIf RemoteFilename.Contains("/") Then
            Remote = AmendPath(RemoteFilename) ' If has / then is Full Path
        Else ' Otherwise is filename only use the Current Directory for Path
            Remote = CurrentDirectory & RemoteFilename
        End If
        URI = Hostname & Remote
        fRequest = GetRequest(URI)
        fRequest.Method = Net.WebRequestMethods.Ftp.UploadFile ' File Upload
        fRequest.UseBinary = True ' Binary Mode
        fRequest.ContentLength = LocalFileInfo.Length ' Notify FTP of Expected Size
        Dim Content(2047) As Byte ' Byte Buffer Store, at least 1 Byte
        Using LocalData As FileStream = LocalFileInfo.OpenRead() ' Read File
            Try ' Open File Stream, Upload Data
                Using UploadData As Stream = fRequest.GetRequestStream
                    Do
                        DataRead = LocalData.Read(Content, 0, 2048)
                        UploadData.Write(Content, 0, DataRead)
                    Loop Until DataRead < 2048
                    UploadData.Close()
                End Using
            Catch ex As Exception
                ' Do Nothing on Exception
            Finally
                LocalData.Close() ' Close File Stream
            End Try
        End Using
        Return True
    End Function

    ''' <summary>Upload Local File to FTP Server</summary>
    ''' <param name="LocalFilename">Local File</param>
    ''' <param name="RemoteFilename">Target Filename (Optional)</param>
    ''' <returns>True if Success, False if Error/Fail</returns>
    Public Function Upload(ByVal LocalFilename As String, _
                         Optional ByVal RemoteFilename As String = "") As Boolean
        If Not File.Exists(LocalFilename) Then ' Check Local File Exists
            Throw New ApplicationException("File " & LocalFilename & " not found")
        End If
        Return Upload(New FileInfo(LocalFilename), RemoteFilename)
    End Function

    ''' <summary>Download Remote File to Local</summary>
    ''' <param name="RemoteFilename">Remote Filename, if Required</param>
    ''' <param name="LocalFileInfo">Local File Information</param>
    ''' <param name="PermitOverwrite">Overwrite Local File if it Exists</param>
    ''' <returns>True if Success, False if Error/Fail</returns>
    Public Function Download(ByVal RemoteFilename As String, _
                           ByVal LocalFileInfo As FileInfo, _
                           Optional ByVal PermitOverwrite As Boolean = False)
        If LocalFileInfo.Exists And Not PermitOverwrite Then _
        Throw New ApplicationException("Local file already exists")
        Dim Remote, URI As String
        Dim DataRead As Integer
        If RemoteFilename.Trim = "" Then
            Throw New ApplicationException("File not specified")
        ElseIf RemoteFilename.Contains("/") Then
            Remote = AmendPath(RemoteFilename)
        Else
            Remote = CurrentDirectory & RemoteFilename
        End If
        URI = Hostname & Remote
        fRequest = GetRequest(URI)
        fRequest.Method = WebRequestMethods.Ftp.DownloadFile
        fRequest.UseBinary = True
        Using Response As FtpWebResponse = CType(fRequest.GetResponse, FtpWebResponse)
            Using ResponseStream As Stream = Response.GetResponseStream
                Using DownloadData As FileStream = LocalFileInfo.OpenWrite
                    Try
                        Dim Buffer(2048) As Byte
                        Do
                            DataRead = ResponseStream.Read(Buffer, 0, Buffer.Length)
                            DownloadData.Write(Buffer, 0, DataRead)
                        Loop Until DataRead = 0
                        ResponseStream.Close()
                        DownloadData.Flush()
                        DownloadData.Close()
                    Catch ex As Exception
                        DownloadData.Close()
                        LocalFileInfo.Delete() ' Delete Partial Data Downloads
                        Throw
                    End Try
                End Using
                ResponseStream.Close()
            End Using
            Response.Close()
        End Using
        Return True
    End Function

    ''' <summary>Download Remote File to Local</summary>
    ''' <param name="RemoteFilename">Remote Filename, if Required</param>
    ''' <param name="LocalFilename">Full Path of Local File</param>
    ''' <param name="PermitOverwrite">Overwrite Local File if it Exists</param>
    ''' <returns>True if Success, False if Error/Fail</returns>
    Public Function Download(ByVal RemoteFilename As String, _
                           ByVal LocalFilename As String, _
                           Optional ByVal PermitOverwrite As Boolean = False) As Boolean
        Return Me.Download(RemoteFilename, New FileInfo(LocalFilename), PermitOverwrite)
    End Function


End Class
