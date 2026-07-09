<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Idiomas.aspx.vb" Inherits="Idiomas" MasterPageFile="~/Site.master" Title="Idiomas" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h2>IDIOMAS</h2>
    <div class="toolbar">
        <div class="search-box">
            <asp:TextBox ID="txtSearch" runat="server" placeholder="Buscar..." CssClass="search-input" />
            <asp:Button ID="btnSearch" runat="server" Text="Buscar" CssClass="btn btn-search" OnClick="btnSearch_Click" />
            <asp:Button ID="btnClear" runat="server" Text="Limpiar" CssClass="btn btn-clear" OnClick="btnClear_Click" />
        </div>
        <asp:Button ID="btnAdd" runat="server" Text="+ Agregar" CssClass="btn btn-add" OnClick="btnAdd_Click" />
    </div>
    <div id="pnlAdd" runat="server" visible="false" class="modal-overlay show">
        <div class="modal-dialog">
            <h3>Nuevo Idioma</h3>
            <asp:PlaceHolder ID="phAdd" runat="server" />
            <div class="form-buttons">
                <asp:Button ID="btnSave" runat="server" Text="Guardar" CssClass="btn btn-save" OnClick="btnSave_Click" />
                <asp:Button ID="btnCancel" runat="server" Text="Cancelar" CssClass="btn btn-cancel" OnClick="btnCancel_Click" />
            </div>
            <asp:Label ID="lblAddError" runat="server" CssClass="msg-error" />
        </div>
    </div>
    <div id="pnlEdit" runat="server" visible="false" class="modal-overlay show">
        <div class="modal-dialog">
            <h3>Editar Idioma</h3>
            <asp:PlaceHolder ID="phEdit" runat="server" />
            <div class="form-buttons">
                <asp:Button ID="btnEditSave" runat="server" Text="Guardar cambios" CssClass="btn btn-save" OnClick="btnEditSave_Click" />
                <asp:Button ID="btnEditCancel" runat="server" Text="Cancelar" CssClass="btn btn-cancel" OnClick="btnEditCancel_Click" />
            </div>
            <asp:Label ID="lblEditError" runat="server" CssClass="msg-error" />
        </div>
    </div>
    <div class="grid-container">
        <asp:GridView ID="gv" runat="server" CssClass="gridview" AutoGenerateColumns="False"
            AllowPaging="True" PageSize="10" AllowSorting="True"
            OnRowEditing="gv_RowEditing" OnRowUpdating="gv_RowUpdating"
            OnRowDeleting="gv_RowDeleting" OnRowCancelingEdit="gv_RowCancelingEdit"
            OnRowDataBound="gv_RowDataBound" OnPageIndexChanging="gv_PageIndexChanging"
            OnSorting="gv_Sorting"
            EmptyDataText="No se encontraron registros."
            CellPadding="8" CellSpacing="0" BorderWidth="0" GridLines="Both"
            HeaderStyle-CssClass="grid-header" AlternatingRowStyle-CssClass="grid-alt">
            <PagerSettings Mode="NumericFirstLast" PageButtonCount="10" />
            <PagerStyle CssClass="grid-pager" />
        </asp:GridView>
    </div>
    <asp:Label ID="lblMsg" runat="server" CssClass="msg-info" />
</asp:Content>
