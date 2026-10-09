<%@ Page Title="Edit Master Data" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Edit.aspx.cs" Inherits="Project.Admin.Edit" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-topbar">
        <a runat="server" href="~/Admin/Default.aspx" class="ipsos-btn ipsos-btn-secondary ipsos-btn-sm"><i class="fas fa-arrow-left"></i> Back to Admin</a>
    </div>

    <asp:Label ID="LblMessage" runat="server" Visible="false" EnableViewState="false"></asp:Label>

    <div class="ipsos-card">
        <div class="ipsos-card-header">
            <i class="fas fa-edit"></i>
            <asp:Label ID="LblTitle" runat="server" Text=""></asp:Label>
        </div>
        <div class="ipsos-card-body">
            <!-- Add new -->
            <h4 class="admin-section-title"><i class="fas fa-plus-circle"></i> Add New</h4>
            <div class="admin-form-row">
                <asp:Panel ID="PnlAddParent" runat="server" CssClass="admin-form-field">
                    <label class="ipsos-form-label ipsos-form-label-required">Project Type</label>
                    <asp:DropDownList ID="DdlAddParent" runat="server" CssClass="ipsos-form-control"></asp:DropDownList>
                </asp:Panel>
                <div class="admin-form-field admin-form-field-wide">
                    <label class="ipsos-form-label ipsos-form-label-required">Name</label>
                    <asp:TextBox ID="TxtAddName" runat="server" CssClass="ipsos-form-control" MaxLength="200" placeholder="Enter a name..."></asp:TextBox>
                </div>
                <div class="admin-form-field">
                    <asp:Button ID="BtnAdd" runat="server" Text="Add" CssClass="ipsos-btn ipsos-btn-success" OnClick="BtnAdd_Click" />
                </div>
            </div>

            <!-- Filter -->
            <h4 class="admin-section-title"><i class="fas fa-filter"></i> Filter</h4>
            <div class="admin-form-row">
                <div class="admin-form-field">
                    <label class="ipsos-form-label">Status</label>
                    <asp:DropDownList ID="DdlStatusFilter" runat="server" CssClass="ipsos-form-control">
                        <asp:ListItem Value="Active" Selected="True">Active</asp:ListItem>
                        <asp:ListItem Value="Inactive">Inactive</asp:ListItem>
                        <asp:ListItem Value="All">All</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <asp:Panel ID="PnlParentFilter" runat="server" CssClass="admin-form-field">
                    <label class="ipsos-form-label">Project Type</label>
                    <asp:DropDownList ID="DdlParentFilter" runat="server" CssClass="ipsos-form-control"></asp:DropDownList>
                </asp:Panel>
                <div class="admin-form-field admin-form-field-wide">
                    <label class="ipsos-form-label">Name contains</label>
                    <asp:TextBox ID="TxtSearch" runat="server" CssClass="ipsos-form-control" MaxLength="200"></asp:TextBox>
                </div>
                <div class="admin-form-field">
                    <asp:Button ID="BtnFilter" runat="server" Text="Search" CssClass="ipsos-btn ipsos-btn-primary" OnClick="BtnFilter_Click" />
                    <asp:Button ID="BtnReset" runat="server" Text="Reset" CssClass="ipsos-btn ipsos-btn-secondary" OnClick="BtnReset_Click" />
                </div>
            </div>

            <!-- Data grid -->
            <asp:Label ID="LblCount" runat="server" CssClass="admin-count"></asp:Label>
            <div class="ipsos-table-container">
                <asp:GridView ID="GridData" runat="server" CssClass="ipsos-table" AutoGenerateColumns="False"
                    DataKeyNames="ID" AllowPaging="True" PageSize="50" EmptyDataText="No records found."
                    OnPageIndexChanging="GridData_PageIndexChanging"
                    OnRowEditing="GridData_RowEditing"
                    OnRowCancelingEdit="GridData_RowCancelingEdit"
                    OnRowUpdating="GridData_RowUpdating"
                    OnRowCommand="GridData_RowCommand"
                    OnRowDataBound="GridData_RowDataBound">
                    <Columns>
                        <asp:BoundField DataField="ID" HeaderText="ID" ReadOnly="True" />
                        <asp:TemplateField HeaderText="Name">
                            <ItemTemplate><%#: Eval("Name") %></ItemTemplate>
                            <EditItemTemplate>
                                <asp:TextBox ID="TxtEditName" runat="server" CssClass="ipsos-form-control" MaxLength="200" Text='<%# Eval("Name") %>'></asp:TextBox>
                            </EditItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Project Type">
                            <ItemTemplate><%#: Eval("ParentName") %></ItemTemplate>
                            <EditItemTemplate>
                                <asp:DropDownList ID="DdlEditParent" runat="server" CssClass="ipsos-form-control"></asp:DropDownList>
                            </EditItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Status">
                            <ItemTemplate>
                                <span class='<%# Convert.ToBoolean(Eval("IsActive")) ? "admin-badge admin-badge-active" : "admin-badge admin-badge-inactive" %>'>
                                    <%# Convert.ToBoolean(Eval("IsActive")) ? "Active" : "Inactive" %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="ProjectCount" HeaderText="Active Projects" ReadOnly="True" />
                        <asp:TemplateField HeaderText="Actions">
                            <ItemTemplate>
                                <asp:Button ID="BtnEdit" runat="server" CommandName="Edit" Text="Edit" CssClass="ipsos-btn ipsos-btn-primary ipsos-btn-sm" />
                                <asp:Button ID="BtnDeactivate" runat="server" CommandName="Deactivate" CommandArgument='<%# Eval("ID") %>'
                                    Text="Deactivate" CssClass="ipsos-btn ipsos-btn-danger ipsos-btn-sm"
                                    Visible='<%# Convert.ToBoolean(Eval("IsActive")) %>'
                                    OnClientClick="return confirm('Deactivate this item? It will no longer appear in dropdowns.');" />
                                <asp:Button ID="BtnReactivate" runat="server" CommandName="Reactivate" CommandArgument='<%# Eval("ID") %>'
                                    Text="Reactivate" CssClass="ipsos-btn ipsos-btn-teal ipsos-btn-sm"
                                    Visible='<%# !Convert.ToBoolean(Eval("IsActive")) %>' />
                            </ItemTemplate>
                            <EditItemTemplate>
                                <asp:Button ID="BtnUpdate" runat="server" CommandName="Update" Text="Save" CssClass="ipsos-btn ipsos-btn-success ipsos-btn-sm" />
                                <asp:Button ID="BtnCancel" runat="server" CommandName="Cancel" Text="Cancel" CssClass="ipsos-btn ipsos-btn-secondary ipsos-btn-sm" />
                            </EditItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </div>

    <!-- Merge -->
    <div class="ipsos-card" style="margin-top: 1.5rem;">
        <div class="ipsos-card-header">
            <i class="fas fa-compress-arrows-alt"></i> Merge Duplicate Items
        </div>
        <div class="ipsos-card-body">
            <p class="admin-note">
                All projects that use the <strong>source</strong> item are moved to the <strong>target</strong> item, then the source is deactivated.
                <asp:Literal ID="LitMergeNote" runat="server"></asp:Literal>
                This cannot be undone automatically.
            </p>
            <div class="admin-form-row">
                <div class="admin-form-field admin-form-field-wide">
                    <label class="ipsos-form-label ipsos-form-label-required">Source (will be deactivated)</label>
                    <asp:DropDownList ID="DdlMergeSource" runat="server" CssClass="ipsos-form-control"></asp:DropDownList>
                </div>
                <div class="admin-form-field admin-form-field-wide">
                    <label class="ipsos-form-label ipsos-form-label-required">Target (kept)</label>
                    <asp:DropDownList ID="DdlMergeTarget" runat="server" CssClass="ipsos-form-control"></asp:DropDownList>
                </div>
                <div class="admin-form-field">
                    <asp:Button ID="BtnMerge" runat="server" Text="Merge" CssClass="ipsos-btn ipsos-btn-warning" OnClick="BtnMerge_Click"
                        OnClientClick="return confirm('Merge the source item into the target item? All projects will be moved to the target.');" />
                </div>
            </div>
        </div>
    </div>

    <style>
        .admin-topbar {
            margin-bottom: 1rem;
        }
        .admin-message {
            display: block;
        }
        .admin-section-title {
            color: var(--ipsos-prussian-blue);
            font-size: 1rem;
            font-weight: 600;
            margin: 0 0 0.75rem;
        }
        .admin-section-title i {
            color: var(--ipsos-teal);
            margin-right: 0.4rem;
        }
        .admin-form-row {
            display: flex;
            flex-wrap: wrap;
            align-items: flex-end;
            gap: 1rem;
            margin-bottom: 1.5rem;
            padding-bottom: 1rem;
            border-bottom: 1px solid #e9ecef;
        }
        .admin-form-field {
            flex: 0 1 auto;
            min-width: 160px;
        }
        .admin-form-field-wide {
            flex: 1 1 260px;
        }
        .admin-count {
            display: block;
            color: #6c757d;
            font-size: 0.85rem;
            margin-bottom: 0.5rem;
        }
        .admin-note {
            color: #6c757d;
            font-size: 0.9rem;
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
        .ipsos-table input[type="text"], .ipsos-table select {
            padding: 0.4rem 0.75rem;
            border: 1px solid #ced4da;
            border-radius: 4px;
            font-size: 0.9rem;
        }
    </style>
</asp:Content>
