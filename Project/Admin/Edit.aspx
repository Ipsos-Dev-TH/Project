<%@ Page Title="Edit" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Edit.aspx.cs" Inherits="Project.Admin.Edit" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="ipsos-card">
        <div class="ipsos-card-header">
            <i class="fas fa-edit"></i>
            <asp:Label ID="Label1" runat="server" Text=""></asp:Label>
        </div>
        <div class="ipsos-card-body">
            <!-- Action Buttons -->
            <div class="edit-actions">
                <asp:Button ID="Button1" runat="server" Text="Add New" CssClass="ipsos-btn ipsos-btn-success" OnClick="Button1_Click" />
            </div>

            <!-- Data Grid -->
            <div class="ipsos-table-container">
                <asp:GridView ID="GridView1" runat="server" CssClass="ipsos-table"
                    OnRowCancelingEdit="GridView1_RowCancelingEdit"
                    OnRowDeleting="GridView1_RowDeleting"
                    OnRowEditing="GridView1_RowEditing"
                    OnRowUpdating="GridView1_RowUpdating"
                    AllowPaging="True"
                    OnPageIndexChanging="GridView1_PageIndexChanging"
                    PageSize="100">
                    <Columns>
                        <asp:CommandField ButtonType="Button" ShowEditButton="True" />
                        <asp:CommandField ShowDeleteButton="True" ButtonType="Button" />
                    </Columns>
                </asp:GridView>
            </div>

            <!-- Back Button -->
            <div class="edit-footer">
                <asp:Button ID="Button2" runat="server" Text="Back to Configuration" CssClass="ipsos-btn ipsos-btn-secondary" OnClick="Button2_Click" />
            </div>
        </div>
    </div>

    <style>
        .edit-actions {
            margin-bottom: 1.5rem;
            padding-bottom: 1rem;
            border-bottom: 1px solid #e9ecef;
        }
        .edit-footer {
            margin-top: 1.5rem;
            padding-top: 1rem;
            border-top: 1px solid #e9ecef;
            text-align: center;
        }
        .ipsos-table input[type="text"] {
            padding: 0.4rem 0.75rem;
            border: 1px solid #ced4da;
            border-radius: 4px;
            font-size: 0.9rem;
        }
        .ipsos-table input[type="text"]:focus {
            border-color: var(--ipsos-sapphire-blue);
            outline: none;
            box-shadow: 0 0 0 2px rgba(55, 78, 161, 0.15);
        }
        .ipsos-table input[type="checkbox"] {
            width: 18px;
            height: 18px;
            accent-color: var(--ipsos-sapphire-blue);
        }
    </style>
</asp:Content>
