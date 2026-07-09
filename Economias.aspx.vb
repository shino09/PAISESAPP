Imports System.Data

' Pagina CRUD para la tabla ECONOMIAS
Public Class Economias
    Inherits System.Web.UI.Page

    Private Const TABLA As String = "ECONOMIAS"
    Private columnInfo As List(Of ColumnInfo)
    Private pkColumns As List(Of String)
    Private fkInfo As DataTable
    Private search As String = ""
    Private currentEditPK As Dictionary(Of String, Object)

    ' Carga metadatos de columnas, PK y FK al iniciar la pagina
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        CargarMetaDatos()
        If Not IsPostBack Then CargarDatos()
    End Sub

    ' Obtiene columnas, primary key y foreign keys de la tabla
    Private Sub CargarMetaDatos()
        columnInfo = DatabaseHelper.GetTableColumns(TABLA)
        pkColumns = DatabaseHelper.GetPrimaryKeyColumns(TABLA)
        fkInfo = DatabaseHelper.GetForeignKeyInfo(TABLA)
    End Sub

    ' Llena el GridView con datos y oculta los modales
    Private Sub CargarDatos(Optional buscar As String = "")
        pnlAdd.Visible = False
        pnlEdit.Visible = False
        search = buscar
        ViewState("search") = buscar
        Dim sCol As String = If(ViewState("sortCol") IsNot Nothing, ViewState("sortCol").ToString(), "")
        Dim sDir As String = If(ViewState("sortDir") IsNot Nothing, ViewState("sortDir").ToString(), "")
        TableCrud.CargarGrid(gv, TABLA, columnInfo, pkColumns, buscar, sCol, sDir)
    End Sub

    ' Ejecuta busqueda por texto en columnas de tipo texto
    Protected Sub btnSearch_Click(sender As Object, e As EventArgs)
        CargarDatos(txtSearch.Text.Trim())
    End Sub

    ' Limpia el filtro de busqueda y recarga el grid
    Protected Sub btnClear_Click(sender As Object, e As EventArgs)
        txtSearch.Text = ""
        gv.PageIndex = 0
        CargarDatos()
    End Sub

    ' Muestra el modal con el formulario para nuevo registro
    Protected Sub btnAdd_Click(sender As Object, e As EventArgs)
        pnlAdd.Visible = True
        lblAddError.Text = ""
        TableCrud.ConstruirFormularioAdd(phAdd, columnInfo, fkInfo)
    End Sub

    ' Inserta el nuevo registro en la base de datos
    Protected Sub btnSave_Click(sender As Object, e As EventArgs)
        Dim err As String = TableCrud.Insertar(phAdd, TABLA, columnInfo)
        If String.IsNullOrEmpty(err) Then
            pnlAdd.Visible = False
            lblMsg.Text = "Registro agregado."
            CargarDatos(search)
        Else
            lblAddError.Text = err
        End If
    End Sub

    ' Cierra el modal de alta sin guardar
    Protected Sub btnCancel_Click(sender As Object, e As EventArgs)
        pnlAdd.Visible = False
    End Sub

    ' Abre el modal de edicion con los datos de la fila seleccionada
    Protected Sub gv_RowEditing(sender As Object, e As GridViewEditEventArgs)
        e.Cancel = True
        currentEditPK = New Dictionary(Of String, Object)()
        For Each pk In pkColumns
            If pkColumns.Count > 1 Then
                currentEditPK(pk) = gv.DataKeys(e.NewEditIndex).Values(pk)
            Else
                currentEditPK(pk) = gv.DataKeys(e.NewEditIndex).Value
            End If
        Next
        Dim rowData As DataRow = TableCrud.GetRowData(TABLA, pkColumns, currentEditPK)
        If rowData IsNot Nothing Then
            lblEditError.Text = ""
            pnlEdit.Visible = True
            TableCrud.ConstruirFormularioEditar(phEdit, columnInfo, fkInfo, rowData)
        End If
    End Sub

    ' Actualiza el registro usando edicion inline del GridView
    Protected Sub gv_RowUpdating(sender As Object, e As GridViewUpdateEventArgs)
        Dim err As String = TableCrud.Actualizar(gv, e, TABLA, columnInfo, pkColumns)
        If String.IsNullOrEmpty(err) Then
            gv.EditIndex = -1
            lblMsg.Text = "Registro actualizado."
            CargarDatos(search)
        Else
            lblMsg.Text = "Error: " & err
        End If
    End Sub

    ' Elimina el registro seleccionado
    Protected Sub gv_RowDeleting(sender As Object, e As GridViewDeleteEventArgs)
        Dim err As String = TableCrud.Eliminar(e, TABLA, pkColumns)
        If String.IsNullOrEmpty(err) Then
            lblMsg.Text = "Registro eliminado."
            CargarDatos(search)
        Else
            lblMsg.Text = "Error: " & err
        End If
    End Sub

    ' Cancela la edicion inline y restaura el grid
    Protected Sub gv_RowCancelingEdit(sender As Object, e As GridViewCancelEditEventArgs)
        gv.EditIndex = -1
        CargarDatos(search)
    End Sub

    ' Aplica formato a botones y celdas en cada fila del grid
    Protected Sub gv_RowDataBound(sender As Object, e As GridViewRowEventArgs)
        TableCrud.FormatearFilaEdicion(gv, e, columnInfo)
    End Sub

    ' Maneja el cambio de pagina del GridView
    Protected Sub gv_PageIndexChanging(sender As Object, e As GridViewPageEventArgs)
        gv.EditIndex = -1
        gv.PageIndex = e.NewPageIndex
        CargarDatos(search)
    End Sub

    ' Ordena los datos por la columna seleccionada
    Protected Sub gv_Sorting(sender As Object, e As GridViewSortEventArgs)
        Dim col As String = e.SortExpression
        Dim dir As String = "ASC"
        If ViewState("sortCol") IsNot Nothing AndAlso ViewState("sortCol").ToString() = col Then
            dir = If(ViewState("sortDir").ToString() = "ASC", "DESC", "ASC")
        End If
        ViewState("sortCol") = col
        ViewState("sortDir") = dir
        gv.PageIndex = 0
        CargarDatos(search)
    End Sub

    ' Guarda los cambios realizados en el modal de edicion
    Protected Sub btnEditSave_Click(sender As Object, e As EventArgs)
        Dim err As String = TableCrud.ActualizarDesdeModal(phEdit, TABLA, columnInfo, pkColumns, currentEditPK)
        If String.IsNullOrEmpty(err) Then
            pnlEdit.Visible = False
            lblMsg.Text = "Registro actualizado."
            CargarDatos(search)
        Else
            lblEditError.Text = err
        End If
    End Sub

    ' Cierra el modal de edicion sin guardar
    Protected Sub btnEditCancel_Click(sender As Object, e As EventArgs)
        pnlEdit.Visible = False
    End Sub
End Class
