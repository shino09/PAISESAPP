' Master page: establece el titulo de la pestana del navegador
Public Class SiteMaster
    Inherits System.Web.UI.MasterPage

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        litTitle.Text = "PaisesAPP - " & Page.Title
        Dim asm = System.Reflection.Assembly.GetExecutingAssembly()
        If asm IsNot Nothing AndAlso Not String.IsNullOrEmpty(asm.Location) Then
            Dim compileTime = System.IO.File.GetLastWriteTime(asm.Location)
            lblVersion.Text = compileTime.ToString("yyyy-MM-dd HH:mm:ss")
        End If
    End Sub
End Class
