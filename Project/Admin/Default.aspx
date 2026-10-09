<%@ Page Title="Admin" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Project.Admin.Default" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="ipsos-page-header">
        <h1><i class="fas fa-cogs"></i> Admin</h1>
        <p>Manage master data, users, data quality and audit history</p>
    </div>

    <asp:Label ID="LblMessage" runat="server" Visible="false" EnableViewState="false"></asp:Label>

    <!-- Quick counts -->
    <div class="admin-stats">
        <div class="admin-stat">
            <i class="fas fa-folder-open"></i>
            <div>
                <asp:Label ID="LblActiveProjects" runat="server" CssClass="admin-stat-value" Text="-"></asp:Label>
                <span class="admin-stat-label">Active projects</span>
            </div>
        </div>
        <div class="admin-stat">
            <i class="fas fa-clone"></i>
            <div>
                <asp:Label ID="LblDuplicateGroups" runat="server" CssClass="admin-stat-value" Text="-"></asp:Label>
                <span class="admin-stat-label">Duplicate Symphony groups</span>
            </div>
        </div>
        <div class="admin-stat">
            <i class="fas fa-user-check"></i>
            <div>
                <asp:Label ID="LblActiveUsers" runat="server" CssClass="admin-stat-value" Text="-"></asp:Label>
                <span class="admin-stat-label">Active users</span>
            </div>
        </div>
        <div class="admin-stat">
            <i class="fas fa-trash-restore"></i>
            <div>
                <asp:Label ID="LblDeletedProjects" runat="server" CssClass="admin-stat-value" Text="-"></asp:Label>
                <span class="admin-stat-label">Deleted projects</span>
            </div>
        </div>
    </div>

    <div class="ipsos-row">
        <!-- Master data -->
        <div class="ipsos-col-6">
            <div class="ipsos-card">
                <div class="ipsos-card-header">
                    <i class="fas fa-database"></i> Master Data
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
                        <a runat="server" href="~/Admin/Edit.aspx?data=Project_Type" class="ipsos-btn ipsos-btn-primary ipsos-btn-sm">Manage</a>
                    </div>
                    <div class="config-item">
                        <div class="config-info">
                            <i class="fas fa-layer-group config-icon"></i>
                            <div>
                                <strong>Project SubType</strong>
                                <small>Manage sub types for each project type</small>
                            </div>
                        </div>
                        <a runat="server" href="~/Admin/Edit.aspx?data=Project_SubType" class="ipsos-btn ipsos-btn-primary ipsos-btn-sm">Manage</a>
                    </div>
                    <div class="config-item">
                        <div class="config-info">
                            <i class="fas fa-cubes config-icon"></i>
                            <div>
                                <strong>Product Type</strong>
                                <small>Manage product types</small>
                            </div>
                        </div>
                        <a runat="server" href="~/Admin/Edit.aspx?data=Product_Type" class="ipsos-btn ipsos-btn-primary ipsos-btn-sm">Manage</a>
                    </div>
                    <div class="config-item">
                        <div class="config-info">
                            <i class="fas fa-building config-icon"></i>
                            <div>
                                <strong>Client Name</strong>
                                <small>Manage client names</small>
                            </div>
                        </div>
                        <a runat="server" href="~/Admin/Edit.aspx?data=ClientName" class="ipsos-btn ipsos-btn-primary ipsos-btn-sm">Manage</a>
                    </div>
                    <div class="config-item">
                        <div class="config-info">
                            <i class="fas fa-industry config-icon"></i>
                            <div>
                                <strong>Market Sector</strong>
                                <small>Manage market sectors</small>
                            </div>
                        </div>
                        <a runat="server" href="~/Admin/Edit.aspx?data=MarketSector" class="ipsos-btn ipsos-btn-primary ipsos-btn-sm">Manage</a>
                    </div>
                    <div class="config-item">
                        <div class="config-info">
                            <i class="fas fa-server config-icon"></i>
                            <div>
                                <strong>Client DB</strong>
                                <small>Manage client databases</small>
                            </div>
                        </div>
                        <a runat="server" href="~/Admin/Edit.aspx?data=ClientDB" class="ipsos-btn ipsos-btn-primary ipsos-btn-sm">Manage</a>
                    </div>
                </div>
            </div>
        </div>

        <div class="ipsos-col-6">
            <!-- Data quality -->
            <div class="ipsos-card">
                <div class="ipsos-card-header">
                    <i class="fas fa-broom"></i> Data Quality
                </div>
                <div class="ipsos-card-body">
                    <div class="config-item">
                        <div class="config-info">
                            <i class="fas fa-clone config-icon"></i>
                            <div>
                                <strong>Duplicate Projects</strong>
                                <small>Merge active projects that share a Symphony Number</small>
                            </div>
                        </div>
                        <a runat="server" href="~/Admin/Duplicates.aspx" class="ipsos-btn ipsos-btn-primary ipsos-btn-sm">Open</a>
                    </div>
                    <div class="config-item">
                        <div class="config-info">
                            <i class="fas fa-trash-restore config-icon"></i>
                            <div>
                                <strong>Deleted Projects</strong>
                                <small>Review and restore soft-deleted projects</small>
                            </div>
                        </div>
                        <a runat="server" href="~/Admin/DeletedProjects.aspx" class="ipsos-btn ipsos-btn-primary ipsos-btn-sm">Open</a>
                    </div>
                </div>
            </div>

            <!-- Users & audit -->
            <div class="ipsos-card" style="margin-top: 1.5rem;">
                <div class="ipsos-card-header">
                    <i class="fas fa-user-shield"></i> Users &amp; Audit
                </div>
                <div class="ipsos-card-body">
                    <div class="config-item">
                        <div class="config-info">
                            <i class="fas fa-users config-icon"></i>
                            <div>
                                <strong>Users</strong>
                                <small>Grant roles (Admin / Editor / Viewer) and disable accounts</small>
                            </div>
                        </div>
                        <a runat="server" href="~/Admin/Users.aspx" class="ipsos-btn ipsos-btn-primary ipsos-btn-sm">Open</a>
                    </div>
                    <div class="config-item">
                        <div class="config-info">
                            <i class="fas fa-history config-icon"></i>
                            <div>
                                <strong>Audit Logs</strong>
                                <small>Search the change history</small>
                            </div>
                        </div>
                        <a runat="server" href="~/Admin/Logs.aspx" class="ipsos-btn ipsos-btn-primary ipsos-btn-sm">Open</a>
                    </div>
                    <div class="config-item">
                        <div class="config-info">
                            <i class="fas fa-file-upload config-icon"></i>
                            <div>
                                <strong>Upload History</strong>
                                <small>Results and errors of Excel uploads</small>
                            </div>
                        </div>
                        <a runat="server" href="~/Admin/UploadHistory.aspx" class="ipsos-btn ipsos-btn-primary ipsos-btn-sm">Open</a>
                    </div>
                </div>
            </div>
        </div>
    </div>

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
        .admin-message {
            display: block;
        }
        .admin-stats {
            display: flex;
            flex-wrap: wrap;
            gap: 1rem;
            margin-bottom: 1.5rem;
        }
        .admin-stat {
            flex: 1 1 200px;
            display: flex;
            align-items: center;
            gap: 1rem;
            padding: 1rem 1.25rem;
            background: #fff;
            border-radius: 8px;
            border-left: 4px solid var(--ipsos-teal);
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
        }
        .admin-stat i {
            font-size: 1.75rem;
            color: var(--ipsos-sapphire-blue);
        }
        .admin-stat-value {
            display: block;
            font-size: 1.6rem;
            font-weight: 700;
            color: var(--ipsos-prussian-blue);
        }
        .admin-stat-value.admin-stat-alert {
            color: #c0392b;
        }
        .admin-stat-label {
            color: #6c757d;
            font-size: 0.85rem;
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
    </style>
</asp:Content>
