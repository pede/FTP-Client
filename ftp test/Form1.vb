Imports System.Runtime.InteropServices
Public Class frmMain


    Private Const PASSWORD_TITLE As String = "Password:"
    Private Const ASTERISK_KEYCODE = 42
    Private Const GW_CHILD = 5
    Private Const WM_CREATE = &H1
    Private Const EM_SETPASSWORDCHAR = &HCC
    Private WindowHandle As Integer
    Private Client As New FTP

    <DllImport("shell32.dll", CallingConvention:=CallingConvention.Cdecl)> _
    Private Shared Function ExtractIcon(ByVal hIcon As IntPtr, _
    ByVal lpszExeFileName As String, ByVal nIconIndex As Integer) As IntPtr
    End Function

    Private Declare Function FindWindow Lib "user32" Alias "FindWindowA" _
    (ByVal lpClassName As String, ByVal lpWindowName As String) As Integer

    Private Declare Function GetWindow Lib "user32" _
    (ByVal hwnd As Integer, ByVal wCmd As Integer) As Integer

    Public Declare Function SendMessage Lib "user32" Alias "SendMessageA" _
    (ByVal hwnd As Integer, ByVal wMsg As Integer, ByVal wParam As Integer, _
     ByVal lParam As Integer) As Integer

    Private Sub Password()
        Dim InputWindow As Integer
        InputWindow = GetWindow(WindowHandle, GW_CHILD)
        SendMessage(InputWindow, EM_SETPASSWORDCHAR, ASTERISK_KEYCODE, 0)
    End Sub

    Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
        MyBase.WndProc(m)
        If m.Result.ToInt32 = WM_CREATE Then
            WindowHandle = FindWindow(vbNullString, PASSWORD_TITLE)
        ElseIf WindowHandle > 0 Then
            Password()
            WindowHandle = 0
        End If
    End Sub

    Private Function GetParent(ByVal Path As String)
        Dim strPath As String
        If Path.EndsWith("/") Then
            strPath = Mid(Path, 1, Path.LastIndexOf("/"))
        Else
            strPath = Path
        End If
        strPath = strPath.Substring(0, strPath.LastIndexOf("/"))
        If strPath = "" Then
            strPath = "/"
        End If
        Return strPath
    End Function

    Private Sub Directory(Optional ByVal Current As String = "/")
        Dim Path As String
        Dim Result As List(Of String)
        Result = Client.DirectoryList(Current)
        ExplorerIcons.Images.Clear()
        Explorer.Items.Clear()
        If Current <> "/" Then
            Dim Up As New ListViewItem
            Up.Tag = GetParent(Client.CurrentDirectory)
            Up.Text = "..."

            Explorer.Items.Add(Up)
        End If
        For Each Line As String In Result
            If Line <> "" Then
                Dim Item As New ListViewItem
                Item.Tag = Line ' Full Name
                Path = Current.Replace("/", "") & "/"
                If Line.Contains(Path) Then
                    Line = Replace(Line, Path, "")
                End If
                Item.Text = Line ' Partial Name
                Explorer.Items.Add(Item)
            End If
        Next
    End Sub

    Private Sub Download(ByVal Location As String)
        Dim Download As New SaveFileDialog
        Download.AddExtension = True
        Download.DefaultExt = IO.Path.GetExtension(Location)
        Download.CheckPathExists = True
        Download.Filter = UCase(Download.DefaultExt) & _
        " File (*." & Download.DefaultExt & ")|*." & Download.DefaultExt
        Download.FileName = Location
        If Download.ShowDialog(Me) = Windows.Forms.DialogResult.OK Then
            Try
                Client.Download(Location, Download.FileName)
            Catch ex As Exception
                ' Do nothing on Exception
            End Try
        End If
    End Sub


    Private Sub ConnectToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ConnectToolStripMenuItem.Click
        Client.Hostname = InputBox("Hostname:", "Host Name")
        Client.Username = InputBox("Username:", "User Name")
        Client.Password = InputBox("Password:", PASSWORD_TITLE)
        Directory()

    End Sub

    Private Sub UploadToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles UploadToolStripMenuItem.Click
        If Client.Connection Then
            Dim Upload As New OpenFileDialog()
            Upload.Filter = "All files (*.*)|*.*"
            Upload.CheckFileExists = True
            If Upload.ShowDialog(Me) = Windows.Forms.DialogResult.OK Then
                Try
                    Client.Upload(Upload.FileName)
                    Directory(Client.CurrentDirectory)
                Catch ex As Exception
                    ' Do nothing on Exception
                End Try
            End If
        Else
            MsgBox("Please connect to FTP Server", _
            MsgBoxStyle.Exclamation, "FTP Client")
        End If

    End Sub

    Private Sub DownloadToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DownloadToolStripMenuItem.Click
        If Client.Connection Then
            Dim Selected As ListViewItem
            If Explorer.SelectedItems.Count > 0 Then
                Selected = Explorer.Items(Explorer.SelectedItems(0).Index)
                If Selected.Tag.Contains(".") Then ' File
                    Download(Selected.Tag)
                End If
            End If
        Else
            MsgBox("Please connect to FTP Server", _
            MsgBoxStyle.Exclamation, "FTP Client")
        End If

    End Sub

    Private Sub ExitToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ExitToolStripMenuItem.Click
        Dim Response As MsgBoxResult
        Response = MsgBox("Are you sure you want to Exit FTP Client?", _
                          MsgBoxStyle.Question + MsgBoxStyle.YesNo, "FTP Client")
        If Response = MsgBoxResult.Yes Then
            End
        End If

    End Sub

    Private Sub NewFolderToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NewFolderToolStripMenuItem.Click
        If Client.Connection Then
            Dim Response As String = "x"
            If Response <> "" Then
                Client.DirectoryCreate("NewFolder")
                Directory(Client.CurrentDirectory)
            End If
        Else
            MsgBox("Please connect to FTP Server", _
            MsgBoxStyle.Exclamation, "FTP Client")
        End If
    End Sub

    Private Sub RenameToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RenameToolStripMenuItem.Click
        If Client.Connection Then
            Dim Response As String
            Dim Selected As ListViewItem
            If Explorer.SelectedItems.Count > 0 Then
                Selected = Explorer.Items(Explorer.SelectedItems(0).Index)
                Response = InputBox("Name:", "FTP Client", Selected.Text)
                If Response <> "" Then
                    Try
                        If Selected.Tag.Contains(".") Then ' File
                            Client.FileRename(Selected.Tag, Response)
                        Else ' Folder
                            Client.DirectoryRename(Selected.Tag, Response)
                        End If
                    Catch ex As Exception
                        ' Do nothing on Exception
                    End Try
                    Directory()
                End If
            End If
        Else
            MsgBox("Please connect to FTP Server", _
            MsgBoxStyle.Exclamation, "FTP Client")
        End If

    End Sub

    Private Sub DeleteToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DeleteToolStripMenuItem.Click
        If Client.Connection Then
            Dim Selected As ListViewItem
            If Explorer.SelectedItems.Count > 0 Then
                Selected = Explorer.Items(Explorer.SelectedItems(0).Index)
                Try
                    If Selected.Tag.Contains(".") Then ' File
                        Client.FileDelete(Selected.Tag)
                    Else ' Folder
                        Client.DirectoryDelete(Selected.Tag)
                    End If
                Catch ex As Exception
                    ' Do nothing on Exception
                End Try
                Directory()
            End If
        Else
            MsgBox("Please connect to FTP Server", _
            MsgBoxStyle.Exclamation, "FTP Client")
        End If

    End Sub

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Me.Text = "FTP Client"
    End Sub

    Private Sub Explorer_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles Explorer.DoubleClick
        If Client.Connection Then
            Dim Selected As ListViewItem
            If Explorer.SelectedItems.Count > 0 Then
                Selected = Explorer.Items(Explorer.SelectedItems(0).Index)
                If Selected.Tag.Contains(".") Then ' File
                    Download(Selected.Tag)
                Else ' Folder
                    If Selected.Tag.StartsWith("/") Then
                        Directory(Selected.Tag) ' List
                    Else
                        Directory("/" & Selected.Tag) ' List
                    End If
                End If
            End If
        Else
            MsgBox("Please connect to FTP Server", _
            MsgBoxStyle.Exclamation, "FTP Client")
        End If

    End Sub

End Class
