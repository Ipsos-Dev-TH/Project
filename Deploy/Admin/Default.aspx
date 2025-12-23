<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Project.Admin.Default" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
      <p>
          <br />
    </p>
    <asp:Panel ID="Panel1" runat="server">
        <style>
            th, td {
  padding: 5px;
}
        </style>
        <table style="width:100%;" border ="1">
            <tr>
                <td colspan="2"><strong>Project</strong></td>
            </tr>
            <tr>
                <td style="width: 20%">Project Type:</td>
                <td>
                    <asp:Button ID="Button10" runat="server" Text="Edit" Width="120px" OnClick="Button10_Click" />
                </td>
            </tr>
            <tr>
                <td style="width: 20%">Project Sub Type:</td>
                <td>
                    <asp:DropDownList ID="DropDownList4" runat="server" DataSourceID="SqlDataSource1" DataTextField="Project_Type" DataValueField="ID">
                    </asp:DropDownList>
                    <asp:SqlDataSource ID="SqlDataSource1" runat="server" ConnectionString="<%$ ConnectionStrings:ProjectConnectionString %>" SelectCommand="SELECT * FROM [Project_Type] WHERE ([Status] = @Status) ORDER BY [Project_Type]">
                        <SelectParameters>
                            <asp:Parameter DefaultValue="True" Name="Status" Type="Boolean" />
                        </SelectParameters>
                    </asp:SqlDataSource>
                    <asp:Button ID="Button11" runat="server" Text="Edit" Width="120px" OnClick="Button11_Click" />
                </td>
            </tr>
            <tr>
                <td style="width: 20%"><strong>Product</strong></td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td style="width: 20%">Product Type:</td>
                <td>
                    <asp:Button ID="Button12" runat="server"  Text="Edit" Width="120px" OnClick="Button12_Click1" />
                </td>
            </tr>
        </table>


    </asp:Panel>
</asp:Content>
