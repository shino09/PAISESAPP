Imports System.Data
Imports Oracle.ManagedDataAccess.Client
Imports System.Configuration

' Metadatos de una columna de tabla
Public Class ColumnInfo
    Public Property ColumnName As String
    Public Property DataType As String
    Public Property DataLength As Integer
    Public Property IsNullable As Boolean
    Public Property IsIdentity As Boolean
    Public Property IsVirtual As Boolean
End Class

' Metodos estaticos para acceso a base de datos Oracle
Public Class DatabaseHelper

    ' Obtiene la cadena de conexion desde Web.config
    Public Shared Function GetConnectionString() As String
        Return ConfigurationManager.ConnectionStrings("OracleConnection").ConnectionString
    End Function

    ' Ejecuta un SELECT y devuelve un DataTable
    Public Shared Function GetDataTable(sql As String, Optional params As Dictionary(Of String, Object) = Nothing) As DataTable
        Dim dt As New DataTable()
        Using conn As New OracleConnection(GetConnectionString())
            conn.Open()
            Using cmd As New OracleCommand(sql, conn)
                cmd.BindByName = True
                If params IsNot Nothing Then
                    For Each kvp In params
                        cmd.Parameters.Add(New OracleParameter(kvp.Key, kvp.Value))
                    Next
                End If
                Using da As New OracleDataAdapter(cmd)
                    da.Fill(dt)
                End Using
            End Using
        End Using
        Return dt
    End Function

    ' Ejecuta INSERT / UPDATE / DELETE y devuelve filas afectadas
    Public Shared Function ExecuteNonQuery(sql As String, Optional params As Dictionary(Of String, Object) = Nothing) As Integer
        Using conn As New OracleConnection(GetConnectionString())
            conn.Open()
            Using cmd As New OracleCommand(sql, conn)
                cmd.BindByName = True
                If params IsNot Nothing Then
                    For Each kvp In params
                        cmd.Parameters.Add(New OracleParameter(kvp.Key, kvp.Value))
                    Next
                End If
                Return cmd.ExecuteNonQuery()
            End Using
        End Using
    End Function

    ' Devuelve lista de tablas del usuario actual en Oracle
    Public Shared Function GetTableNames() As List(Of String)
        Dim sql As String = "SELECT table_name FROM user_tables ORDER BY table_name"
        Dim dt As DataTable = GetDataTable(sql)
        Dim result As New List(Of String)()
        For Each row As DataRow In dt.Rows
            result.Add(row("table_name").ToString())
        Next
        Return result
    End Function

    ' Obtiene metadatos de columnas con deteccion directa de virtuales via ALL_TAB_COLS
    ' Nota: USER_TAB_COLUMNS no expone VIRTUAL_COLUMN en Oracle 21c,
    ' pero ALL_TAB_COLS si lo hace.
    Public Shared Function GetTableColumns(tableName As String) As List(Of ColumnInfo)
        Dim sql As String = "SELECT column_name, data_type, data_length, nullable, " & _
                            "identity_column, virtual_column " & _
                            "FROM all_tab_cols WHERE table_name = :p_table AND owner = USER " & _
                            "ORDER BY column_id"
        Dim p As New Dictionary(Of String, Object)()
        p.Add("p_table", tableName)
        Dim dt As DataTable = GetDataTable(sql, p)
        Dim result As New List(Of ColumnInfo)()
        For Each row As DataRow In dt.Rows
            Dim ci As New ColumnInfo()
            ci.ColumnName = row("column_name").ToString()
            ci.DataType = row("data_type").ToString()
            ci.DataLength = Convert.ToInt32(row("data_length"))
            ci.IsNullable = (row("nullable").ToString() = "Y")
            ci.IsIdentity = (row("identity_column").ToString() = "YES")
            ci.IsVirtual = (row("virtual_column").ToString() = "YES")
            result.Add(ci)
        Next
        Return result
    End Function

    ' Obtiene nombres de columnas que forman la clave primaria
    Public Shared Function GetPrimaryKeyColumns(tableName As String) As List(Of String)
        Dim sql As String = "SELECT cols.column_name " & _
                            "FROM user_constraints cons, user_cons_columns cols " & _
                            "WHERE cons.constraint_type = 'P' " & _
                            "AND cons.constraint_name = cols.constraint_name " & _
                            "AND cons.table_name = :p_table " & _
                            "ORDER BY cols.position"
        Dim p As New Dictionary(Of String, Object)()
        p.Add("p_table", tableName)
        Dim dt As DataTable = GetDataTable(sql, p)
        Dim result As New List(Of String)()
        For Each row As DataRow In dt.Rows
            result.Add(row("column_name").ToString())
        Next
        Return result
    End Function

    ' Obtiene restricciones FK: columna, tabla referenciada, columna referenciada
    Public Shared Function GetForeignKeyInfo(tableName As String) As DataTable
        Dim sql As String = "SELECT a.constraint_name, a.column_name, " & _
                            "c_pk.table_name AS ref_table, " & _
                            "b.column_name AS ref_column " & _
                            "FROM user_cons_columns a " & _
                            "JOIN user_constraints cons ON a.constraint_name = cons.constraint_name " & _
                            "JOIN user_cons_columns b ON cons.r_constraint_name = b.constraint_name " & _
                            "JOIN user_constraints c_pk ON cons.r_constraint_name = c_pk.constraint_name " & _
                            "WHERE cons.constraint_type = 'R' " & _
                            "AND a.table_name = :p_table " & _
                            "ORDER BY a.constraint_name, a.position"
        Dim p As New Dictionary(Of String, Object)()
        p.Add("p_table", tableName)
        Return GetDataTable(sql, p)
    End Function

    ' Obtiene datos de tabla foranea para llenar DropDownList
    Public Shared Function GetForeignTableData(tableName As String, keyColumn As String, displayColumn As String) As DataTable
        Dim sql As String = String.Format("SELECT {0}, {1} FROM {2} ORDER BY {1}", keyColumn, displayColumn, tableName)
        Return GetDataTable(sql)
    End Function

End Class
