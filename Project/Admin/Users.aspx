<%@ Page Title="Users" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Users.aspx.cs" Inherits="Project.Admin.Users" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-topbar">
        <a runat="server" href="~/Admin/Default.aspx" class="ipsos-btn ipsos-btn-secondary ipsos-btn-sm"><i class="fas fa-arrow-left"></i> Back to Admin</a>
    </div>

    <asp:Label ID="LblMessage" runat="server" Visible="false" EnableViewState="false"></asp:Label>

    <div class="ipsos-alert ipsos-alert-info admin-message">
        <i class="fas fa-info-circle"></i>
        <span>Role changes and disabling take effect at the user's next login. Users not listed here sign in with the default role.</span>
    </div>

    <div class="ipsos-card">
        <div class="ipsos-card-header">
            <i class="fas fa-user-plus"></i> Add User
        </div>
        <div class="ipsos-card-body">
            <div class="admin-form-row">
                <div class="admin-form-field admin-form-field-wide">
                    <label class="ipsos-form-label ipsos-form-label-required">Email (@ipsos.com)</label>
                    <asp:TextBox ID="TxtEmail" runat="server" CssClass="ipsos-form-control" MaxLength="255" placeholder="firstname.lastname@ipsos.com"></asp:TextBox>
                </div>
                <div class="admin-form-field">
                    <label class="ipsos-form-label ipsos-form-label-required">Role</label>
                    <asp:DropDownList ID="DdlRole" runat="server" CssClass="ipsos-form-control"></asp:DropDownList>
                </div>
                <div class="admin-form-field">
                    <asp:Button ID="BtnAddUser" runat="server" Text="Add User" CssClass="ipsos-btn ipsos-btn-success" OnClick="BtnAddUser_Click" />
                </div>
            </div>
        </div>
    </div>

    <div class="ipsos-card" style="margin-top: 1.5rem;">
        <div class="ipsos-card-header">
            <i class="fas fa-users"></i> Users
        </div>
        <div class="ipsos-card-body">
            <div class="admin-form-row">
                <div class="admin-form-field admin-form-field-wide">
                    <label class="ipsos-form-label">Email contains</label>
                    <asp:TextBox ID="TxtSearch" runat="server" CssClass="ipsos-form-control" MaxLength="255"></asp:TextBox>
                </div>
                <div class="admin-form-field">
                    <asp:Button ID="BtnSearch" runat="server" Text="Search" CssClass="ipsos-btn ipsos-btn-primary" OnClick="BtnSearch_Click" />
                </div>
            </div>

            <div class="ipsos-table-container">
                <asp:GridView ID="GridUsers" runat="server" CssClass="ipsos-table" AutoGenerateColumns="False"
                    DataKeyNames="ID" AllowPaging="True" PageSize="50" EmptyDataText="No users found."
                    OnPageIndexChanging="GridUsers_PageIndexChanging"
                    OnRowCommand="GridUsers_RowCommand"
                    OnRowDataBound="GridUsers_RowDataBound">
                    <Columns>
                        <asp:BoundField DataField="Email" HeaderText="Email" />
                        <asp:TemplateField HeaderText="Role">
                            <ItemTemplate>
                                <div class="admin-inline">
                                    <asp:DropDownList ID="DdlRowRole" runat="server" CssClass="admin-select-sm"></asp:DropDownList>
                                    <asp:Button ID="BtnChangeRole" runat="server" CommandName="ChangeRole" CommandArgument='<%# Eval("ID") %>'
                                        Text="Save" CssClass="ipsos-btn ipsos-btn-primary ipsos-btn-sm" />
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Status">
                            <ItemTemplate>
                                <span class='<%# Convert.ToBoolean(Eval("IsActive")) ? "admin-badge admin-badge-active" : "admin-badge admin-badge-inactive" %>'>
                                    <%# Convert.ToBoolean(Eval("IsActive")) ? "Enabled" : "Disabled" %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Created">
                            <ItemTemplate>
                                <%#: Eval("Created_DateTime", "{0:yyyy-MM-dd HH:mm}") %><br />
                                <small class="admin-muted"><%#: Eval("Created_By") %></small>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Modified">
                            <ItemTemplate>
                                <%#: Eval("Modified_DateTime", "{0:yyyy-MM-dd HH:mm}") %><br />
                                <small class="admin-muted"><%#: Eval("Modified_By") %></small>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Actions">
                            <ItemTemplate>
                                <asp:Button ID="BtnDisable" runat="server" CommandName="DisableUser" CommandArgument='<%# Eval("ID") %>'
                                    Text="Disable" CssClass="ipsos-btn ipsos-btn-danger ipsos-btn-sm"
                                    Visible='<%# Convert.ToBoolean(Eval("IsActive")) %>'
                                    OnClientClick="return confirm('Disable this user?');" />
                                <asp:Button ID="BtnEnable" runat="server" CommandName="EnableUser" CommandArgument='<%# Eval("ID") %>'
                                    Text="Enable" CssClass="ipsos-btn ipsos-btn-teal ipsos-btn-sm"
                                    Visible='<%# !Convert.ToBoolean(Eval("IsActive")) %>' />
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
        .admin-inline {
            display: flex;
            gap: 0.4rem;
            align-items: center;
        }
        .admin-select-sm {
            padding: 0.3rem 0.5rem;
            border: 1px solid #ced4da;
            border-radius: 4px;
            font-size: 0.85rem;
        }
        .admin-muted {
            color: #6c757d;
        }
        .admin-badge {
            display: inline-block;
            padding: 0.2rem 0.6rem;
            border-radius: 999px;
            font-size: 0.75rem;
            font-weight: 600;
        }
        .admin-badge-active {
            background: rgba(39, 174, 96, 0.15);
            color: #1e8449;
        }
        .admin-badge-inactive {
            background: rgba(108, 117, 125, 0.15);
            color: #495057;
        }
    </style>
</asp:Content>
