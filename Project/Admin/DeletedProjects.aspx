<%@ Page Title="Deleted Projects" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="DeletedProjects.aspx.cs" Inherits="Project.Admin.DeletedProjects" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-topbar">
        <a runat="server" href="~/Admin/Default.aspx" class="ipsos-btn ipsos-btn-secondary ipsos-btn-sm"><i class="fas fa-arrow-left"></i> Back to Admin</a>
    </div>

    <asp:Label ID="LblMessage" runat="server" Visible="false" EnableViewState="false"></asp:Label>

    <div class="ipsos-card">
        <div class="ipsos-card-header">
            <i class="fas fa-trash-restore"></i> Deleted Projects
            <asp:Label ID="LblSummary" runat="server" CssClass="admin-summary"></asp:Label>
        </div>
        <div class="ipsos-card-body">
            <div class="admin-form-row">
                <div class="admin-form-field admin-form-field-wide">
                    <label class="ipsos-form-label">Search (ID, Symphony Number, Project Name, Client, Deleted By)</label>
                    <asp:TextBox ID="TxtSearch" runat="server" CssClass="ipsos-form-control" MaxLength="200"></asp:TextBox>
                </div>
                <div class="admin-form-field">
                    <asp:Button ID="BtnSearch" runat="server" Text="Search" CssClass="ipsos-btn ipsos-btn-primary" OnClick="BtnSearch_Click" />
                </div>
            </div>

            <div class="ipsos-table-container">
                <asp:GridView ID="GridDeleted" runat="server" CssClass="ipsos-table" AutoGenerateColumns="False"
                    DataKeyNames="ID" AllowPaging="True" PageSize="50" EmptyDataText="No deleted projects found."
                    OnPageIndexChanging="GridDeleted_PageIndexChanging"
                    OnRowCommand="GridDeleted_RowCommand">
                    <Columns>
                        <asp:BoundField DataField="ID" HeaderText="ID" />
                        <asp:BoundField DataField="Symphony_Number" HeaderText="Symphony Number" />
                        <asp:BoundField DataField="Project_Name" HeaderText="Project Name" />
                        <asp:BoundField DataField="Project_Type" HeaderText="Type" />
                        <asp:BoundField DataField="ClientName" HeaderText="Client" />
                        <asp:BoundField DataField="Status" HeaderText="Status" />
                        <asp:BoundField DataField="Creator" HeaderText="Creator" />
                        <asp:BoundField DataField="Modified_By" HeaderText="Deleted By" />
                        <asp:BoundField DataField="Modified_DateTime" HeaderText="Deleted At" DataFormatString="{0:yyyy-MM-dd HH:mm}" />
                        <asp:TemplateField HeaderText="Action">
                            <ItemTemplate>
                                <asp:Button ID="BtnRestore" runat="server" CommandName="Restore" CommandArgument='<%# Eval("ID") %>'
                                    Text="Restore" CssClass="ipsos-btn ipsos-btn-teal ipsos-btn-sm"
                                    OnClientClick="return confirm('Restore this project?');" />
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
            min-width: 160px;
        }
        .admin-form-field-wide {
            flex: 1 1 260px;
        }
    </style>
</asp:Content>
