Imports System.Data
Imports System.Text
Imports System.Web.UI.WebControls

' Metodos compartidos para CRUD generico sobre cualquier tabla
Public Class TableCrud

    ' Llena el GridView: consulta SELECT, configura columnas, paginacion, botones
    Public Shared Sub CargarGrid(grid As GridView, tableName As String,
                                 ByRef columnInfo As List(Of ColumnInfo),
                                 ByRef pkColumns As List(Of String),
                                 Optional search As String = "",
                                 Optional sortCol As String = "",
                                 Optional sortDir As String = "")

        Dim sql As New StringBuilder()
        sql.Append("SELECT * FROM " & tableName)

        If Not String.IsNullOrEmpty(search) Then
            Dim textCols = columnInfo.Where(Function(c) c.DataType.ToUpper().Contains("CHAR") OrElse _
                                               c.DataType.ToUpper().Contains("VARCHAR") OrElse _
                                               c.DataType.ToUpper().Contains("CLOB")).ToList()
            If textCols.Count > 0 Then
                sql.Append(" WHERE ")
                For i As Integer = 0 To textCols.Count - 1
                    If i > 0 Then sql.Append(" OR ")
                    sql.AppendFormat("UPPER({0}) LIKE :p_search", textCols(i).ColumnName)
                Next
            End If
        End If

        If Not String.IsNullOrEmpty(sortCol) Then
            sql.AppendFormat(" ORDER BY {0} {1}", sortCol, sortDir)
        Else
            sql.Append(" ORDER BY 1")
        End If

        Dim dt As DataTable
        If Not String.IsNullOrEmpty(search) Then
            Dim p As New Dictionary(Of String, Object)()
            p.Add("p_search", "%" & search.ToUpper() & "%")
            dt = DatabaseHelper.GetDataTable(sql.ToString(), p)
        Else
            dt = DatabaseHelper.GetDataTable(sql.ToString())
        End If

        If pkColumns.Count > 0 Then
            grid.DataKeyNames = pkColumns.ToArray()
        End If

        grid.Columns.Clear()

        For Each col As DataColumn In dt.Columns
            Dim ci = columnInfo.FirstOrDefault(Function(c) c.ColumnName = col.ColumnName)
            If ci IsNot Nothing AndAlso ci.IsIdentity Then Continue For

            Dim bf As New BoundField()
            bf.DataField = col.ColumnName
            bf.HeaderText = col.ColumnName
            bf.SortExpression = col.ColumnName

            If ci IsNot Nothing Then
                Dim editable As Boolean = Not (pkColumns.Contains(ci.ColumnName) OrElse ci.IsIdentity OrElse ci.IsVirtual)
                bf.ReadOnly = Not editable

                If ci.DataType.ToUpper().Contains("DATE") Then
                    bf.DataFormatString = "{0:yyyy-MM-dd}"
                    bf.HtmlEncode = False
                End If

                If ci.DataType.ToUpper().Contains("NUMBER") AndAlso Not ci.DataType.ToUpper().Contains("CHAR") Then
                    Dim n = ci.ColumnName.ToUpper()
                    If n.Contains("POBLACION") OrElse n.Contains("SUPERFICIE") OrElse n.Contains("PIB") OrElse n.Contains("DEUDA") Then
                        bf.DataFormatString = "{0:N0}"
                        bf.HtmlEncode = False
                    ElseIf n.Contains("PORCENTAJE") OrElse n.Contains("INFLACION") OrElse n.Contains("DESEMPLEO") OrElse n.Contains("DENSIDAD") Then
                        bf.DataFormatString = "{0:N2}"
                        bf.HtmlEncode = False
                    End If
                End If
            End If

            grid.Columns.Add(bf)
        Next

        If pkColumns.Count > 0 Then
            Dim cf As New CommandField()
            cf.ShowEditButton = True
            cf.ShowDeleteButton = True
            cf.ButtonType = ButtonType.Button
            cf.HeaderText = "Acc."
            cf.ItemStyle.CssClass = "col-actions"
            cf.ItemStyle.HorizontalAlign = HorizontalAlign.Right
            grid.Columns.Add(cf)
        End If

        grid.DataSource = dt
        grid.DataBind()
    End Sub

    ' Ejecuta UPDATE en edicion inline del GridView
    Public Shared Function Actualizar(grid As GridView, e As GridViewUpdateEventArgs,
                                      tableName As String, columnInfo As List(Of ColumnInfo),
                                      pkColumns As List(Of String)) As String
        Try
            Dim clauses As New List(Of String)()
            Dim p As New Dictionary(Of String, Object)()
            Dim row As GridViewRow = grid.Rows(e.RowIndex)

            For i As Integer = 0 To grid.Columns.Count - 1
                Dim bf As BoundField = TryCast(grid.Columns(i), BoundField)
                If bf Is Nothing OrElse bf.ReadOnly Then Continue For

                Dim colName As String = bf.DataField
                Dim cell As TableCell = row.Cells(i)
                Dim tb As TextBox = TryCast(cell.Controls(0), TextBox)
                If tb Is Nothing Then Continue For

                Dim rawValue As String = tb.Text
                Dim ci = columnInfo.FirstOrDefault(Function(c) c.ColumnName = colName)
                If ci Is Nothing Then Continue For

                Dim pn As String = "p_" & colName
                If ci.DataType.ToUpper().Contains("CHAR") OrElse ci.DataType.ToUpper().Contains("VARCHAR") OrElse ci.DataType.ToUpper().Contains("CLOB") Then
                    clauses.Add(String.Format("{0} = :{1}", colName, pn))
                    p.Add(pn, rawValue)
                ElseIf ci.DataType.ToUpper().Contains("NUMBER") OrElse ci.DataType.ToUpper().Contains("FLOAT") OrElse ci.DataType.ToUpper().Contains("INTEGER") Then
                    If String.IsNullOrWhiteSpace(rawValue) Then
                        clauses.Add(String.Format("{0} = NULL", colName))
                    Else
                        clauses.Add(String.Format("{0} = :{1}", colName, pn))
                        Dim nv As Decimal = Convert.ToDecimal(rawValue.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture)
                        p.Add(pn, nv)
                    End If
                ElseIf ci.DataType.ToUpper().Contains("DATE") Then
                    If String.IsNullOrWhiteSpace(rawValue) Then
                        clauses.Add(String.Format("{0} = NULL", colName))
                    Else
                        clauses.Add(String.Format("{0} = TO_DATE(:{1}, 'YYYY-MM-DD')", colName, pn))
                        p.Add(pn, rawValue)
                    End If
                Else
                    clauses.Add(String.Format("{0} = :{1}", colName, pn))
                    p.Add(pn, rawValue)
                End If
            Next

            If clauses.Count = 0 Then Return "No hay campos editables."

            Dim sql As New StringBuilder()
            sql.AppendFormat("UPDATE {0} SET ", tableName)
            sql.Append(String.Join(", ", clauses))
            sql.Append(" WHERE ")

            Dim wc As New List(Of String)()
            For Each pk In pkColumns
                wc.Add(String.Format("{0} = :pk_{0}", pk))
                p.Add("pk_" & pk, e.Keys(pk))
            Next
            sql.Append(String.Join(" AND ", wc))

            DatabaseHelper.ExecuteNonQuery(sql.ToString(), p)
            Return ""
        Catch ex As Exception
            Return ex.Message
        End Try
    End Function

    ' Ejecuta DELETE recibiendo las PK del GridView
    Public Shared Function Eliminar(e As GridViewDeleteEventArgs,
                                    tableName As String,
                                    pkColumns As List(Of String)) As String
        Try
            Dim p As New Dictionary(Of String, Object)()
            Dim wc As New List(Of String)()
            For Each pk In pkColumns
                wc.Add(String.Format("{0} = :pk_{0}", pk))
                p.Add("pk_" & pk, e.Keys(pk))
            Next
            Dim sql As String = String.Format("DELETE FROM {0} WHERE {1}", tableName, String.Join(" AND ", wc))
            DatabaseHelper.ExecuteNonQuery(sql, p)
            Return ""
        Catch ex As Exception
            Return ex.Message
        End Try
    End Function

    ' Ejecuta INSERT desde los controles del modal de alta
    Public Shared Function Insertar(ph As System.Web.UI.WebControls.PlaceHolder,
                                    tableName As String,
                                    columnInfo As List(Of ColumnInfo)) As String
        Try
            Dim cols As New List(Of String)()
            Dim p As New Dictionary(Of String, Object)()
            Dim vals As New List(Of String)()

            For Each ci In columnInfo
                If ci.IsIdentity OrElse ci.IsVirtual Then Continue For

                Dim ctrl As System.Web.UI.Control = FindControlInPage(ph, "add_" & ci.ColumnName)
                If ctrl Is Nothing Then Continue For

                Dim rawValue As String = ""
                If TypeOf ctrl Is TextBox Then
                    rawValue = CType(ctrl, TextBox).Text.Trim()
                ElseIf TypeOf ctrl Is DropDownList Then
                    rawValue = CType(ctrl, DropDownList).SelectedValue.Trim()
                End If

                cols.Add(ci.ColumnName)

                If String.IsNullOrEmpty(rawValue) Then
                    vals.Add("NULL")
                    Continue For
                End If

                Dim pn As String = "p_" & ci.ColumnName
                If ci.DataType.ToUpper().Contains("CHAR") OrElse ci.DataType.ToUpper().Contains("VARCHAR") OrElse ci.DataType.ToUpper().Contains("CLOB") Then
                    vals.Add(":" & pn)
                    p.Add(pn, rawValue)
                ElseIf ci.DataType.ToUpper().Contains("NUMBER") OrElse ci.DataType.ToUpper().Contains("FLOAT") OrElse ci.DataType.ToUpper().Contains("INTEGER") Then
                    Dim nv As Decimal
                    If Decimal.TryParse(rawValue.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, nv) Then
                        vals.Add(":" & pn)
                        p.Add(pn, nv)
                    Else
                        vals.Add("NULL")
                    End If
                ElseIf ci.DataType.ToUpper().Contains("DATE") Then
                    vals.Add("TO_DATE(:" & pn & ", 'YYYY-MM-DD')")
                    p.Add(pn, rawValue)
                Else
                    vals.Add(":" & pn)
                    p.Add(pn, rawValue)
                End If
            Next

            If cols.Count = 0 Then Return "No hay columnas para insertar."

            Dim sql As String = String.Format("INSERT INTO {0} ({1}) VALUES ({2})",
                                               tableName,
                                               String.Join(", ", cols),
                                               String.Join(", ", vals))
            DatabaseHelper.ExecuteNonQuery(sql, p)
            Return ""
        Catch ex As Exception
            Return ex.Message
        End Try
    End Function

    ' Genera dinamicamente controles (TextBox, DropDownList, Date) para el modal de alta
    Public Shared Sub ConstruirFormularioAdd(ph As System.Web.UI.WebControls.PlaceHolder,
                                             columnInfo As List(Of ColumnInfo),
                                             fkInfo As DataTable)
        ph.Controls.Clear()
        Dim container As New HtmlGenericControl("div")
        container.Attributes.Add("class", "add-form-grid")

        For Each ci In columnInfo
            If ci.IsIdentity OrElse ci.IsVirtual Then Continue For

            Dim div As New HtmlGenericControl("div")
            div.Attributes.Add("class", "add-field")

            Dim lbl As New Label()
            lbl.Text = ci.ColumnName
            lbl.ToolTip = String.Format("{0} ({1})", ci.ColumnName, ci.DataType)
            lbl.CssClass = "add-label"
            div.Controls.Add(lbl)

            Dim fkRows() As DataRow = Nothing
            If fkInfo IsNot Nothing Then
                fkRows = fkInfo.Select("column_name = '" & ci.ColumnName & "'")
            End If

            If fkRows IsNot Nothing AndAlso fkRows.Length > 0 Then
                Dim refTable As String = fkRows(0)("ref_table").ToString()
                Dim refCol As String = fkRows(0)("ref_column").ToString()
                Dim displayCol As String = GetDisplayColumn(refTable)
                Dim ddl As New DropDownList()
                ddl.ID = "add_" & ci.ColumnName
                ddl.CssClass = "add-select"
                ddl.Items.Add(New ListItem("-- Seleccionar --", ""))
                Try
                    Dim refData As DataTable = DatabaseHelper.GetForeignTableData(refTable, refCol, displayCol)
                    For Each r As DataRow In refData.Rows
                        ddl.Items.Add(New ListItem(r(displayCol).ToString() & " (" & r(refCol).ToString() & ")", r(refCol).ToString()))
                    Next
                Catch
                    Dim txt As New TextBox()
                    txt.ID = "add_" & ci.ColumnName
                    txt.CssClass = "add-input"
                    div.Controls.Add(txt)
                End Try
                div.Controls.Add(ddl)
            ElseIf ci.DataType.ToUpper().Contains("CHAR") AndAlso ci.DataLength <= 2 Then
                Dim ddl As New DropDownList()
                ddl.ID = "add_" & ci.ColumnName
                ddl.CssClass = "add-select"
                ddl.Items.Add(New ListItem("--", ""))
                If ci.ColumnName.ToUpper().Contains("ES_") Then
                    ddl.Items.Add(New ListItem("Si (S)", "S"))
                    ddl.Items.Add(New ListItem("No (N)", "N"))
                Else
                    ddl.Items.Add(New ListItem("S", "S"))
                    ddl.Items.Add(New ListItem("N", "N"))
                End If
                div.Controls.Add(ddl)
            ElseIf ci.DataType.ToUpper().Contains("DATE") Then
                Dim txt As New TextBox()
                txt.ID = "add_" & ci.ColumnName
                txt.CssClass = "add-input input-date"
                txt.Attributes.Add("placeholder", "YYYY-MM-DD")
                div.Controls.Add(txt)
            ElseIf ci.DataType.ToUpper().Contains("NUMBER") OrElse ci.DataType.ToUpper().Contains("FLOAT") OrElse ci.DataType.ToUpper().Contains("INTEGER") Then
                Dim txt As New TextBox()
                txt.ID = "add_" & ci.ColumnName
                txt.CssClass = "add-input input-number"
                div.Controls.Add(txt)
            Else
                Dim txt As New TextBox()
                txt.ID = "add_" & ci.ColumnName
                txt.CssClass = "add-input"
                If ci.DataLength > 0 AndAlso ci.DataLength < 100 Then
                    txt.MaxLength = ci.DataLength
                End If
                div.Controls.Add(txt)
            End If

            container.Controls.Add(div)
        Next

        ph.Controls.Add(container)
    End Sub

    ' Aplica estilos a botones Editar/Eliminar y formato a controles de edicion inline
    Public Shared Sub FormatearFilaEdicion(grid As GridView, e As GridViewRowEventArgs,
                                           columnInfo As List(Of ColumnInfo))
        If e.Row.RowType = DataControlRowType.DataRow Then
            For i As Integer = 0 To grid.Columns.Count - 1
                If TypeOf grid.Columns(i) Is CommandField Then
                    For Each ctrl As Control In e.Row.Cells(i).Controls
                        Dim btn As Button = TryCast(ctrl, Button)
                        If btn IsNot Nothing Then
                            If btn.CommandName = "Edit" Then
                                btn.Attributes("class") = "btn btn-edit-grid"
                                btn.Text = ChrW(&H270F) & " Editar"
                            ElseIf btn.CommandName = "Delete" Then
                                btn.Attributes("class") = "btn btn-delete-grid"
                                btn.Text = ChrW(&H2716) & " Eliminar"
                                btn.OnClientClick = "return confirm('" & MensajeConfirmacionEliminar() & "');"
                            ElseIf btn.CommandName = "Update" Then
                                btn.Attributes("class") = "btn btn-save"
                                btn.Text = "Guardar"
                            ElseIf btn.CommandName = "Cancel" Then
                                btn.Attributes("class") = "btn btn-cancel"
                                btn.Text = "Cancelar"
                            End If
                        End If
                    Next
                    Exit For
                End If
            Next
        End If
        If e.Row.RowType = DataControlRowType.DataRow AndAlso e.Row.RowState.HasFlag(DataControlRowState.Edit) Then
            For i As Integer = 0 To grid.Columns.Count - 1
                Dim bf As BoundField = TryCast(grid.Columns(i), BoundField)
                If bf Is Nothing OrElse bf.ReadOnly Then Continue For
                Dim cell As TableCell = e.Row.Cells(i)
                Dim tb As TextBox = TryCast(cell.Controls(0), TextBox)
                If tb IsNot Nothing Then
                    Dim ci = columnInfo.FirstOrDefault(Function(c) c.ColumnName = bf.DataField)
                    If ci IsNot Nothing Then
                        If ci.DataType.ToUpper().Contains("DATE") Then
                            tb.Attributes("placeholder") = "YYYY-MM-DD"
                            tb.CssClass = "input-date"
                        ElseIf ci.DataType.ToUpper().Contains("NUMBER") Then
                            tb.CssClass = "input-number"
                        End If
                    End If
                End If
            Next
        End If

        If e.Row.RowType = DataControlRowType.DataRow Then
            For i As Integer = 0 To grid.Columns.Count - 1
                Dim bf As BoundField = TryCast(grid.Columns(i), BoundField)
                If bf IsNot Nothing Then
                    Dim ci = columnInfo.FirstOrDefault(Function(c) c.ColumnName = bf.DataField)
                    If ci IsNot Nothing AndAlso ci.DataType.ToUpper().Contains("DATE") AndAlso e.Row.Cells(i).Text <> "&nbsp;" Then
                        Dim val As Object = DataBinder.Eval(e.Row.DataItem, bf.DataField)
                        If val IsNot Nothing AndAlso val IsNot DBNull.Value Then
                            e.Row.Cells(i).Text = Convert.ToDateTime(val).ToString("yyyy-MM-dd")
                        End If
                    End If
                End If
            Next
        End If
    End Sub

    ' Texto de confirmacion para eliminar registros
    Public Shared Function MensajeConfirmacionEliminar() As String
        Return "Esta seguro de eliminar este registro?"
    End Function

    ' Obtiene una fila completa por su clave primaria
    Public Shared Function GetRowData(tableName As String, pkColumns As List(Of String), pkValues As Dictionary(Of String, Object)) As DataRow
        Dim sql As New StringBuilder()
        sql.AppendFormat("SELECT * FROM {0} WHERE ", tableName)
        Dim wc As New List(Of String)()
        Dim p As New Dictionary(Of String, Object)()
        For Each pk In pkColumns
            wc.Add(String.Format("{0} = :pk_{0}", pk))
            p.Add("pk_" & pk, pkValues(pk))
        Next
        sql.Append(String.Join(" AND ", wc))
        Dim dt As DataTable = DatabaseHelper.GetDataTable(sql.ToString(), p)
        If dt.Rows.Count > 0 Then Return dt.Rows(0)
        Return Nothing
    End Function

    ' Genera controles para el modal de edicion precargados con datos existentes
    Public Shared Sub ConstruirFormularioEditar(phEdit As System.Web.UI.WebControls.PlaceHolder,
                                                columnInfo As List(Of ColumnInfo),
                                                fkInfo As DataTable,
                                                rowData As DataRow)
        phEdit.Controls.Clear()
        Dim container As New HtmlGenericControl("div")
        container.Attributes.Add("class", "add-form-grid")

        For Each ci In columnInfo
            If ci.IsIdentity OrElse ci.IsVirtual Then Continue For

            Dim div As New HtmlGenericControl("div")
            div.Attributes.Add("class", "add-field")

            Dim lbl As New Label()
            lbl.Text = ci.ColumnName
            lbl.ToolTip = String.Format("{0} ({1})", ci.ColumnName, ci.DataType)
            lbl.CssClass = "add-label"
            div.Controls.Add(lbl)

            Dim rawValue As String = ""
            If rowData.Table.Columns.Contains(ci.ColumnName) AndAlso rowData(ci.ColumnName) IsNot DBNull.Value Then
                rawValue = rowData(ci.ColumnName).ToString()
            End If

            Dim fkRows() As DataRow = Nothing
            If fkInfo IsNot Nothing Then
                fkRows = fkInfo.Select("column_name = '" & ci.ColumnName & "'")
            End If

            If fkRows IsNot Nothing AndAlso fkRows.Length > 0 Then
                Dim refTable As String = fkRows(0)("ref_table").ToString()
                Dim refCol As String = fkRows(0)("ref_column").ToString()
                Dim displayCol As String = GetDisplayColumn(refTable)
                Dim ddl As New DropDownList()
                ddl.ID = "edit_" & ci.ColumnName
                ddl.CssClass = "add-select"
                ddl.Items.Add(New ListItem("-- Seleccionar --", ""))
                Try
                    Dim refData As DataTable = DatabaseHelper.GetForeignTableData(refTable, refCol, displayCol)
                    For Each r As DataRow In refData.Rows
                        Dim val As String = r(refCol).ToString()
                        ddl.Items.Add(New ListItem(r(displayCol).ToString() & " (" & val & ")", val))
                    Next
                Catch
                    Dim txt As New TextBox()
                    txt.ID = "edit_" & ci.ColumnName
                    txt.CssClass = "add-input"
                    txt.Text = rawValue
                    div.Controls.Add(txt)
                End Try
                If ddl.Items.FindByValue(rawValue) IsNot Nothing Then
                    ddl.SelectedValue = rawValue
                End If
                div.Controls.Add(ddl)
            ElseIf ci.DataType.ToUpper().Contains("CHAR") AndAlso ci.DataLength <= 2 Then
                Dim ddl As New DropDownList()
                ddl.ID = "edit_" & ci.ColumnName
                ddl.CssClass = "add-select"
                ddl.Items.Add(New ListItem("--", ""))
                If ci.ColumnName.ToUpper().Contains("ES_") Then
                    ddl.Items.Add(New ListItem("Si (S)", "S"))
                    ddl.Items.Add(New ListItem("No (N)", "N"))
                Else
                    ddl.Items.Add(New ListItem("S", "S"))
                    ddl.Items.Add(New ListItem("N", "N"))
                End If
                If ddl.Items.FindByValue(rawValue) IsNot Nothing Then
                    ddl.SelectedValue = rawValue
                End If
                div.Controls.Add(ddl)
            ElseIf ci.DataType.ToUpper().Contains("DATE") Then
                Dim txt As New TextBox()
                txt.ID = "edit_" & ci.ColumnName
                txt.CssClass = "add-input input-date"
                txt.Attributes.Add("placeholder", "YYYY-MM-DD")
                If Not String.IsNullOrEmpty(rawValue) Then
                    Dim dtVal As DateTime
                    If DateTime.TryParse(rawValue, dtVal) Then
                        txt.Text = dtVal.ToString("yyyy-MM-dd")
                    Else
                        txt.Text = rawValue
                    End If
                End If
                div.Controls.Add(txt)
            ElseIf ci.DataType.ToUpper().Contains("NUMBER") OrElse ci.DataType.ToUpper().Contains("FLOAT") OrElse ci.DataType.ToUpper().Contains("INTEGER") Then
                Dim txt As New TextBox()
                txt.ID = "edit_" & ci.ColumnName
                txt.CssClass = "add-input input-number"
                txt.Text = rawValue
                div.Controls.Add(txt)
            Else
                Dim txt As New TextBox()
                txt.ID = "edit_" & ci.ColumnName
                txt.CssClass = "add-input"
                txt.Text = rawValue
                If ci.DataLength > 0 AndAlso ci.DataLength < 100 Then
                    txt.MaxLength = ci.DataLength
                End If
                div.Controls.Add(txt)
            End If

            container.Controls.Add(div)
        Next

        phEdit.Controls.Add(container)
    End Sub

    ' Ejecuta UPDATE tomando valores desde los controles del modal de edicion
    Public Shared Function ActualizarDesdeModal(phEdit As System.Web.UI.WebControls.PlaceHolder,
                                                tableName As String,
                                                columnInfo As List(Of ColumnInfo),
                                                pkColumns As List(Of String),
                                                pkValues As Dictionary(Of String, Object)) As String
        Try
            Dim clauses As New List(Of String)()
            Dim p As New Dictionary(Of String, Object)()

            For Each ci In columnInfo
                If ci.IsIdentity OrElse ci.IsVirtual Then Continue For

                Dim ctrl As System.Web.UI.Control = FindControlInPage(phEdit, "edit_" & ci.ColumnName)
                If ctrl Is Nothing Then Continue For

                Dim rawValue As String = ""
                If TypeOf ctrl Is TextBox Then
                    rawValue = CType(ctrl, TextBox).Text.Trim()
                ElseIf TypeOf ctrl Is DropDownList Then
                    rawValue = CType(ctrl, DropDownList).SelectedValue.Trim()
                End If

                Dim pn As String = "p_" & ci.ColumnName
                If ci.DataType.ToUpper().Contains("CHAR") OrElse ci.DataType.ToUpper().Contains("VARCHAR") OrElse ci.DataType.ToUpper().Contains("CLOB") Then
                    clauses.Add(String.Format("{0} = :{1}", ci.ColumnName, pn))
                    p.Add(pn, rawValue)
                ElseIf ci.DataType.ToUpper().Contains("NUMBER") OrElse ci.DataType.ToUpper().Contains("FLOAT") OrElse ci.DataType.ToUpper().Contains("INTEGER") Then
                    If String.IsNullOrWhiteSpace(rawValue) Then
                        clauses.Add(String.Format("{0} = NULL", ci.ColumnName))
                    Else
                        clauses.Add(String.Format("{0} = :{1}", ci.ColumnName, pn))
                        Dim nv As Decimal = Convert.ToDecimal(rawValue.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture)
                        p.Add(pn, nv)
                    End If
                ElseIf ci.DataType.ToUpper().Contains("DATE") Then
                    If String.IsNullOrWhiteSpace(rawValue) Then
                        clauses.Add(String.Format("{0} = NULL", ci.ColumnName))
                    Else
                        clauses.Add(String.Format("{0} = TO_DATE(:{1}, 'YYYY-MM-DD')", ci.ColumnName, pn))
                        p.Add(pn, rawValue)
                    End If
                Else
                    clauses.Add(String.Format("{0} = :{1}", ci.ColumnName, pn))
                    p.Add(pn, rawValue)
                End If
            Next

            If clauses.Count = 0 Then Return "No hay campos editables."

            Dim sql As New StringBuilder()
            sql.AppendFormat("UPDATE {0} SET ", tableName)
            sql.Append(String.Join(", ", clauses))
            sql.Append(" WHERE ")

            Dim wc As New List(Of String)()
            For Each pk In pkColumns
                wc.Add(String.Format("{0} = :pk_{0}", pk))
                p.Add("pk_" & pk, pkValues(pk))
            Next
            sql.Append(String.Join(" AND ", wc))

            DatabaseHelper.ExecuteNonQuery(sql.ToString(), p)
            Return ""
        Catch ex As Exception
            Return ex.Message
        End Try
    End Function

    ' Ejecuta DELETE desde el modal de edicion usando valores de PK
    Public Shared Function EliminarDesdeModal(tableName As String, pkColumns As List(Of String), pkValues As Dictionary(Of String, Object)) As String
        Try
            Dim p As New Dictionary(Of String, Object)()
            Dim wc As New List(Of String)()
            For Each pk In pkColumns
                wc.Add(String.Format("{0} = :pk_{0}", pk))
                p.Add("pk_" & pk, pkValues(pk))
            Next
            Dim sql As String = String.Format("DELETE FROM {0} WHERE {1}", tableName, String.Join(" AND ", wc))
            DatabaseHelper.ExecuteNonQuery(sql, p)
            Return ""
        Catch ex As Exception
            Return ex.Message
        End Try
    End Function

    ' Determina que columna mostrar en un DropDownList de FK (NOMBRE, TIPO, etc.)
    Private Shared Function GetDisplayColumn(tabla As String) As String
        Select Case tabla.ToUpper()
            Case "PAISES" : Return "NOMBRE"
            Case "CONTINENTES" : Return "NOMBRE"
            Case "MONEDAS" : Return "NOMBRE"
            Case "IDIOMAS" : Return "NOMBRE"
            Case "GOBIERNOS" : Return "TIPO"
            Case Else
                Dim cols = DatabaseHelper.GetTableColumns(tabla)
                For Each c In cols
                    Dim n = c.ColumnName.ToUpper()
                    If n = "NOMBRE" OrElse n = "TIPO" OrElse n = "DESCRIPCION" Then Return c.ColumnName
                Next
                Dim pkCols = DatabaseHelper.GetPrimaryKeyColumns(tabla)
                Dim first = cols.FirstOrDefault(Function(c) Not pkCols.Contains(c.ColumnName))
                If first IsNot Nothing Then Return first.ColumnName
                Return cols(0).ColumnName
        End Select
    End Function

    ' Busca un control por ID recursivamente dentro de un contenedor
    Private Shared Function FindControlInPage(parent As Control, id As String) As Control
        For Each c As Control In parent.Controls
            If c.ID = id Then Return c
            Dim found As Control = FindControlInPage(c, id)
            If found IsNot Nothing Then Return found
        Next
        Return Nothing
    End Function

End Class
