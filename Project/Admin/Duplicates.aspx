<%@ Page Title="Duplicate Projects" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Duplicates.aspx.cs" Inherits="Project.Admin.Duplicates" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-topbar">
        <a runat="server" href="~/Admin/Default.aspx" class="ipsos-btn ipsos-btn-secondary ipsos-btn-sm"><i class="fas fa-arrow-left"></i> Back to Admin</a>
    </div>

    <asp:Label ID="LblMessage" runat="server" Visible="false" EnableViewState="false"></asp:Label>

    <asp:Panel ID="PnlNoDuplicates" runat="server" Visible="false" CssClass="ipsos-alert ipsos-alert-success admin-message">
        <i class="fas fa-check-circle"></i>
        <span>No duplicate active Symphony Numbers remain. A DBA can now run <strong>Database/002_unique_symphony_number.sql</strong> to enforce uniqueness in the database.</span>
    </asp:Panel>

    <div class="ipsos-card">
        <div class="ipsos-card-header">
            <i class="fas fa-clone"></i> Duplicate Projects
            <asp:Label ID="LblSummary" runat="server" CssClass="admin-summary"></asp:Label>
        </div>
        <div class="ipsos-card-body">
            <p class="admin-note">
                Active projects that share the same Symphony Number (ignoring dashes) are grouped below.
                Click <strong>Keep this</strong> on the row to keep: its empty fields are filled from the other rows in the group
                (most recently modified first), and the other rows are soft-deleted (they can be restored from Deleted Projects).
                When no duplicates remain, run <strong>Database/002_unique_symphony_number.sql</strong>.
            </p>

            <div class="ipsos-table-container">
                <asp:GridView ID="GridDuplicates" runat="server" CssClass="ipsos-table" AutoGenerateColumns="False"
                    DataKeyNames="ID" EmptyDataText="No duplicate projects found."
                    OnRowCommand="GridDuplicates_RowCommand"
                    OnRowDataBound="GridDuplicates_RowDataBound">
                    <Columns>
                        <asp:BoundField DataField="Symphony_Key" HeaderText="Group (Symphony Key)" />
                        <asp:BoundField DataField="ID" HeaderText="ID" />
                        <asp:BoundField DataField="Symphony_Number" HeaderText="Symphony Number" />
                        <asp:BoundField DataField="Project_Name" HeaderText="Project Name" />
                        <asp:BoundField DataField="Project_Type" HeaderText="Type" />
                        <asp:BoundField DataField="SubType" HeaderText="SubType" />
                        <asp:BoundField DataField="ClientName" HeaderText="Client" />
                        <asp:BoundField DataField="Project_StartDate" HeaderText="Start" DataFormatString="{0:yyyy-MM-dd}" />
                        <asp:BoundField DataField="Project_EndDate" HeaderText="End" DataFormatString="{0:yyyy-MM-dd}" />
                        <asp:BoundField DataField="Status" HeaderText="Status" />
                        <asp:BoundField DataField="Creator" HeaderText="Creator" />
                        <asp:BoundField DataField="Created_DateTime" HeaderText="Created" DataFormatString="{0:yyyy-MM-dd HH:mm}" />
                        <asp:TemplateField HeaderText="Action">
                            <ItemTemplate>
                                <asp:Button ID="BtnKeep" runat="server" CommandName="Keep" CommandArgument='<%# Eval("ID") %>'
                                    Text="Keep this" CssClass="ipsos-btn ipsos-btn-success ipsos-btn-sm"
                                    OnClientClick="return confirm('Keep this project and soft-delete the other projects in this group?');" />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </div>

    <style>
        .admin-topbar {
            margin-bottom: 1rem;
        }
        .admin-message {
            display: flex;
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
        .admin-note {
            color: #6c757d;
            font-size: 0.9rem;
        }
        .ipsos-table tr.dup-group-a td {
            background: #ffffff;
        }
        .ipsos-table tr.dup-group-b td {
            background: #eef3fb;
        }
        .ipsos-table tr.dup-group-start td {
            border-top: 2px solid var(--ipsos-teal);
        }
    </style>
</asp:Content>
