<%@ Page Title="Upload History" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="UploadHistory.aspx.cs" Inherits="Project.Admin.UploadHistory" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-topbar">
        <a runat="server" href="~/Admin/Default.aspx" class="ipsos-btn ipsos-btn-secondary ipsos-btn-sm"><i class="fas fa-arrow-left"></i> Back to Admin</a>
    </div>

    <asp:Label ID="LblMessage" runat="server" Visible="false" EnableViewState="false"></asp:Label>

    <asp:Panel ID="PnlDetail" runat="server" Visible="false" EnableViewState="false" CssClass="ipsos-card admin-detail-card">
        <div class="ipsos-card-header">
            <i class="fas fa-exclamation-triangle"></i>
            <asp:Label ID="LblDetailTitle" runat="server"></asp:Label>
        </div>
        <div class="ipsos-card-body">
            <pre class="admin-error-detail"><asp:Literal ID="LitDetail" runat="server" Mode="Encode"></asp:Literal></pre>
            <asp:Button ID="BtnCloseDetail" runat="server" Text="Close" CssClass="ipsos-btn ipsos-btn-secondary ipsos-btn-sm" OnClick="BtnCloseDetail_Click" />
        </div>
    </asp:Panel>

    <div class="ipsos-card">
        <div class="ipsos-card-header">
            <i class="fas fa-file-upload"></i> Upload History
            <span class="admin-summary">latest 200 uploads</span>
        </div>
        <div class="ipsos-card-body">
            <div class="ipsos-table-container">
                <asp:GridView ID="GridUploads" runat="server" CssClass="ipsos-table" AutoGenerateColumns="False"
                    DataKeyNames="ID" AllowPaging="True" PageSize="50" EmptyDataText="No uploads recorded yet."
                    OnPageIndexChanging="GridUploads_PageIndexChanging"
                    OnRowCommand="GridUploads_RowCommand">
                    <Columns>
                        <asp:BoundField DataField="Uploaded_DateTime" HeaderText="Date/Time" DataFormatString="{0:yyyy-MM-dd HH:mm:ss}" />
                        <asp:BoundField DataField="File_Name" HeaderText="File" />
                        <asp:BoundField DataField="Uploaded_By" HeaderText="Uploaded By" />
                        <asp:TemplateField HeaderText="Mode">
                            <ItemTemplate>
                                <span class='<%# Convert.ToBoolean(Eval("Is_Preview")) ? "admin-badge admin-badge-preview" : "admin-badge admin-badge-import" %>'>
                                    <%# Convert.ToBoolean(Eval("Is_Preview")) ? "Preview" : "Import" %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="Total_Rows" HeaderText="Total" />
                        <asp:BoundField DataField="Inserted_Count" HeaderText="Inserted" />
                        <asp:BoundField DataField="Updated_Count" HeaderText="Updated" />
                        <asp:BoundField DataField="Skipped_Count" HeaderText="Skipped" />
                        <asp:BoundField DataField="Error_Count" HeaderText="Errors" />
                        <asp:TemplateField HeaderText="Details">
                            <ItemTemplate>
                                <asp:Button ID="BtnView" runat="server" CommandName="ViewDetail" CommandArgument='<%# Eval("ID") %>'
                                    Text="View errors" CssClass="ipsos-btn ipsos-btn-outline ipsos-btn-sm"
                                    Visible='<%# Convert.ToBoolean(Eval("HasDetail")) %>' />
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
        .admin-detail-card {
            margin-bottom: 1.5rem;
        }
        .admin-error-detail {
            white-space: pre-wrap;
            word-break: break-word;
            max-height: 420px;
            overflow: auto;
            background: #f8f9fa;
            border: 1px solid #e9ecef;
            border-radius: 6px;
            padding: 1rem;
            font-size: 0.8rem;
        }
        .admin-badge {
            display: inline-block;
            padding: 0.2rem 0.6rem;
            border-radius: 999px;
            font-size: 0.75rem;
            font-weight: 600;
        }
        .admin-badge-preview {
            background: rgba(243, 156, 18, 0.15);
            color: #b7950b;
        }
        .admin-badge-import {
            background: rgba(39, 174, 96, 0.15);
            color: #1e8449;
        }
    </style>
</asp:Content>
