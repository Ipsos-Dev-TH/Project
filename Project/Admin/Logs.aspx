<%@ Page Title="Audit Logs" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Logs.aspx.cs" Inherits="Project.Admin.Logs" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-topbar">
        <a runat="server" href="~/Admin/Default.aspx" class="ipsos-btn ipsos-btn-secondary ipsos-btn-sm"><i class="fas fa-arrow-left"></i> Back to Admin</a>
    </div>

    <asp:Label ID="LblMessage" runat="server" Visible="false" EnableViewState="false"></asp:Label>

    <div class="ipsos-card">
        <div class="ipsos-card-header">
            <i class="fas fa-history"></i> Audit Logs
            <asp:Label ID="LblSummary" runat="server" CssClass="admin-summary"></asp:Label>
        </div>
        <div class="ipsos-card-body">
            <div class="admin-form-row">
                <div class="admin-form-field">
                    <label class="ipsos-form-label">Table</label>
                    <asp:DropDownList ID="DdlTable" runat="server" CssClass="ipsos-form-control"></asp:DropDownList>
                </div>
                <div class="admin-form-field">
                    <label class="ipsos-form-label">Task</label>
                    <asp:DropDownList ID="DdlTask" runat="server" CssClass="ipsos-form-control"></asp:DropDownList>
                </div>
                <div class="admin-form-field">
                    <label class="ipsos-form-label">User contains</label>
                    <asp:TextBox ID="TxtUser" runat="server" CssClass="ipsos-form-control" MaxLength="255"></asp:TextBox>
                </div>
                <div class="admin-form-field admin-form-field-wide">
                    <label class="ipsos-form-label">Data contains</label>
                    <asp:TextBox ID="TxtText" runat="server" CssClass="ipsos-form-control" MaxLength="500" placeholder="e.g. ProjectID=123;"></asp:TextBox>
                </div>
                <div class="admin-form-field">
                    <label class="ipsos-form-label">From</label>
                    <asp:TextBox ID="TxtFrom" runat="server" CssClass="ipsos-form-control" TextMode="Date"></asp:TextBox>
                </div>
                <div class="admin-form-field">
                    <label class="ipsos-form-label">To</label>
                    <asp:TextBox ID="TxtTo" runat="server" CssClass="ipsos-form-control" TextMode="Date"></asp:TextBox>
                </div>
                <div class="admin-form-field">
                    <asp:Button ID="BtnSearch" runat="server" Text="Search" CssClass="ipsos-btn ipsos-btn-primary" OnClick="BtnSearch_Click" />
                    <asp:Button ID="BtnReset" runat="server" Text="Reset" CssClass="ipsos-btn ipsos-btn-secondary" OnClick="BtnReset_Click" />
                </div>
            </div>

            <div class="ipsos-table-container">
                <asp:GridView ID="GridLogs" runat="server" CssClass="ipsos-table admin-log-table" AutoGenerateColumns="False"
                    AllowPaging="True" PageSize="100" EmptyDataText="No log entries found."
                    OnPageIndexChanging="GridLogs_PageIndexChanging">
                    <Columns>
                        <asp:BoundField DataField="Action_DateTime" HeaderText="Date/Time" DataFormatString="{0:yyyy-MM-dd HH:mm:ss}" />
                        <asp:BoundField DataField="Action_on_Table" HeaderText="Table" />
                        <asp:BoundField DataField="Task" HeaderText="Task" />
                        <asp:BoundField DataField="Action_By" HeaderText="User" />
                        <asp:BoundField DataField="Data" HeaderText="Data" ItemStyle-CssClass="admin-log-data" />
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </div>

    <style>
        .admin-topbar {
            margin-bottom: 1rem;
        }
        span.admin-message {
            display: block;
        }
        .admin-summary {
            margin-left: 0.75rem;
            font-size: 0.85rem;
            font-weight: normal;
            opacity: 0.85;
        }
        .admin-form-row {
            display: flex;
            flex-wrap: wrap;
            align-items: flex-end;
            gap: 1rem;
            margin-bottom: 1rem;
        }
        .admin-form-field {
            flex: 0 1 auto;
            min-width: 150px;
        }
        .admin-form-field-wide {
            flex: 1 1 240px;
        }
        .admin-log-table td {
            vertical-align: top;
            white-space: nowrap;
        }
        .admin-log-table td.admin-log-data {
            white-space: pre-wrap;
            word-break: break-word;
            font-family: Consolas, 'Courier New', monospace;
            font-size: 0.8rem;
            min-width: 320px;
        }
    </style>
</asp:Content>
