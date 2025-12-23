<%@ Page Title="Configuration" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Project.Admin.Default" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:Panel ID="Panel1" runat="server">
        <!-- Page Header -->
        <div class="ipsos-page-header">
            <h1><i class="fas fa-cogs"></i> Configuration</h1>
            <p>Manage system configuration and master data</p>
        </div>

        <!-- Configuration Cards -->
        <div class="ipsos-row">
            <!-- Project Configuration -->
            <div class="ipsos-col-6">
                <div class="ipsos-card">
                    <div class="ipsos-card-header">
                        <i class="fas fa-project-diagram"></i> Project Settings
                    </div>
                    <div class="ipsos-card-body">
                        <div class="config-item">
                            <div class="config-info">
                                <i class="fas fa-tags config-icon"></i>
                                <div>
                                    <strong>Project Type</strong>
                                    <small>Manage project types</small>
                                </div>
                            </div>
                            <asp:Button ID="Button10" runat="server" Text="Edit" CssClass="ipsos-btn ipsos-btn-primary ipsos-btn-sm" OnClick="Button10_Click" />
                        </div>

                        <div class="config-item">
                            <div class="config-info">
                                <i class="fas fa-layer-group config-icon"></i>
                                <div>
                                    <strong>Project Sub Type</strong>
                                    <small>Manage sub types for each project type</small>
                                </div>
                            </div>
                            <div class="config-actions">
                                <asp:DropDownList ID="DropDownList4" runat="server" DataSourceID="SqlDataSource1"
                                    DataTextField="Project_Type" DataValueField="ID" CssClass="ipsos-form-control-sm">
                                </asp:DropDownList>
                                <asp:SqlDataSource ID="SqlDataSource1" runat="server" ConnectionString="<%$ ConnectionStrings:ProjectConnectionString %>"
                                    SelectCommand="SELECT * FROM [Project_Type] WHERE ([Status] = @Status) ORDER BY [Project_Type]">
                                    <SelectParameters>
                                        <asp:Parameter DefaultValue="True" Name="Status" Type="Boolean" />
                                    </SelectParameters>
                                </asp:SqlDataSource>
                                <asp:Button ID="Button11" runat="server" Text="Edit" CssClass="ipsos-btn ipsos-btn-primary ipsos-btn-sm" OnClick="Button11_Click" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Product Configuration -->
            <div class="ipsos-col-6">
                <div class="ipsos-card">
                    <div class="ipsos-card-header">
                        <i class="fas fa-box"></i> Product Settings
                    </div>
                    <div class="ipsos-card-body">
                        <div class="config-item">
                            <div class="config-info">
                                <i class="fas fa-cubes config-icon"></i>
                                <div>
                                    <strong>Product Type</strong>
                                    <small>Manage product types</small>
                                </div>
                            </div>
                            <asp:Button ID="Button12" runat="server" Text="Edit" CssClass="ipsos-btn ipsos-btn-primary ipsos-btn-sm" OnClick="Button12_Click1" />
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div class="ipsos-row" style="margin-top: 1.5rem;">
            <!-- Client Configuration -->
            <div class="ipsos-col-6">
                <div class="ipsos-card">
                    <div class="ipsos-card-header">
                        <i class="fas fa-users"></i> Client Settings
                    </div>
                    <div class="ipsos-card-body">
                        <div class="config-item">
                            <div class="config-info">
                                <i class="fas fa-building config-icon"></i>
                                <div>
                                    <strong>Client Name</strong>
                                    <small>Manage client information</small>
                                </div>
                            </div>
                            <asp:Button ID="Button13" runat="server" Text="Edit" CssClass="ipsos-btn ipsos-btn-primary ipsos-btn-sm" OnClick="Button13_Click" />
                        </div>

                        <div class="config-item">
                            <div class="config-info">
                                <i class="fas fa-industry config-icon"></i>
                                <div>
                                    <strong>Market Sector</strong>
                                    <small>Manage market sectors</small>
                                </div>
                            </div>
                            <asp:Button ID="Button14" runat="server" Text="Edit" CssClass="ipsos-btn ipsos-btn-primary ipsos-btn-sm" OnClick="Button14_Click" />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </asp:Panel>

    <style>
        .ipsos-page-header {
            margin-bottom: 2rem;
            padding-bottom: 1rem;
            border-bottom: 2px solid var(--ipsos-teal);
        }
        .ipsos-page-header h1 {
            color: var(--ipsos-prussian-blue);
            font-family: 'Barlow', sans-serif;
            font-weight: 700;
            margin: 0;
        }
        .ipsos-page-header h1 i {
            color: var(--ipsos-teal);
            margin-right: 0.75rem;
        }
        .ipsos-page-header p {
            color: #6c757d;
            margin: 0.5rem 0 0;
        }
        .config-item {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 1rem;
            background: #f8f9fa;
            border-radius: 8px;
            margin-bottom: 0.75rem;
            transition: all 0.2s ease;
        }
        .config-item:last-child {
            margin-bottom: 0;
        }
        .config-item:hover {
            background: #e9ecef;
        }
        .config-info {
            display: flex;
            align-items: center;
            gap: 1rem;
        }
        .config-icon {
            font-size: 1.5rem;
            color: var(--ipsos-sapphire-blue);
            width: 40px;
            text-align: center;
        }
        .config-info strong {
            display: block;
            color: var(--ipsos-prussian-blue);
        }
        .config-info small {
            color: #6c757d;
            font-size: 0.85rem;
        }
        .config-actions {
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }
        .ipsos-form-control-sm {
            padding: 0.4rem 0.75rem;
            font-size: 0.875rem;
            border: 1px solid #ced4da;
            border-radius: 6px;
            background: #fff;
        }
        .ipsos-form-control-sm:focus {
            border-color: var(--ipsos-sapphire-blue);
            outline: none;
            box-shadow: 0 0 0 3px rgba(55, 78, 161, 0.1);
        }
    </style>
</asp:Content>
