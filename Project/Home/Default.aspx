<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Project.Default" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>    


<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
   
    <script>
        function setHourglass() {
            document.getElementById("MainContent_Button2").disabled = true;
            document.body.style.cursor = 'Wait';
        }
    </script>
    <body>

      <link rel="stylesheet" href="/Content/jquery-ui.css">
  <link rel="stylesheet" href="/Content/style.css">
  <script src="/Scripts/jquery-1.12.4.js"></script>
  <script src="/Scripts/jquery-ui.js"></script>

  <script>
  $( function() {
      $( "#MainContent_datepicker1" ).datepicker();
  });
      $(function () {
          $("#MainContent_datepicker2").datepicker();
      });
  </script>

 

    <style>
            th, td {
  padding: 5px;
}
            .tablerow {
  background-color: whitesmoke; 
}

            .auto-style1 {
                font-size: small;
                height: 20px;
                width: 11%;
            }
            .auto-style4 {
                font-size: small;
                height: 20px;
                text-decoration: underline;
            }
            .auto-style5 {
                width: 23%;
                height: 20px;
            }
            .auto-style7 {
                font-size: small;
                width: 10%;
                height: 20px;
            }
            .auto-style8 {
                font-size: small;
                height: 22px;
                width: 11%;
            }
            .auto-style9 {
                width: 23%;
                height: 22px;
            }
            .auto-style13 {
                font-size: small;
                height: 20px;
                text-align: center;
            }
            .auto-style15 {
            color: #FF0000;

        }
            .auto-style16 {
            font-size: small;
            width: 10%;
            height: 22px;
        }
            .auto-style17 {
            height: 20px;
        }
            .auto-style18 {
            font-size: small;
            height: 20px;
            text-align: right;
            width: 237px;
        }
        .auto-style19 {
            font-size: small;
            height: 20px;
            text-align: left;
        }
            .auto-style20 {
            text-align: right;
        }
            </style><br />
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
    <asp:Panel ID="Panel1" runat="server">
        <table style="width:100%;border-radius:10px;background-color:white" border="0">
            <tr>
                <td></td>
                <td></td>
                <td></td>
                <td></td>
            </tr>
            
            <tr>
                <td style="width:20%" class="auto-style4"></td>
                <td style="width:30%" class="auto-style4">
                    <asp:DropDownList ID="DropDownList15" runat="server" Width="80%">
                        <asp:ListItem Value="Symphony_Number">Symphony Number</asp:ListItem>
                        <asp:ListItem Value="Project_Name">Project Name</asp:ListItem>
                        <asp:ListItem Value="Project_Type">Type</asp:ListItem>
                        <asp:ListItem Value="SubType">Sub Type</asp:ListItem>
                    </asp:DropDownList>
                </td>
                <td style="width:20%" class="auto-style4">
                    <asp:TextBox ID="TextBox21" runat="server" Width="80%" AutoPostBack="True" OnTextChanged="TextBox21_TextChanged"></asp:TextBox>
                </td>
                <td style="width:30%" class="auto-style4">
                    <asp:Button ID="Button6" runat="server" OnClick="Button6_Click" Text="Search" />
                </td>
            </tr>
            <tr>
                <td colspan="4"><center>
                    <asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="False" AllowPaging="True" OnPageIndexChanging="GridView2_PageIndexChanging" OnSelectedIndexChanging="GridView2_SelectedIndexChanging" >
                        <Columns>
                            <asp:CommandField ButtonType="Button" SelectText="View" ShowSelectButton="True" />
                            <asp:BoundField DataField="ID" HeaderText="ID" />
                            <asp:BoundField DataField="Symphony_Number" HeaderText="Symphony Number" />
                            <asp:BoundField DataField="Project_Name" HeaderText="Project Name" />
                            <asp:BoundField DataField="Project_Type" HeaderText="Type" />
                            <asp:BoundField DataField="SubType" HeaderText="SubType" />
                            <asp:BoundField DataField="Project_StartDate" HeaderText="Start Date" />
                            <asp:BoundField DataField="Project_EndDate" HeaderText="End Date" />
                        </Columns>
                    </asp:GridView>
                    </center></td>
            </tr>
                </table><br /></asp:Panel>
    <asp:Panel ID="Panel2" runat="server">
    <table style="width:100%;border-radius:10px;background-color:white" border="0">
        
            <tr>
                <td class="auto-style1" colspan="6"><u><strong>Project (ข้อมูลโปรเจค)</strong></u> </span>&nbsp;<asp:Button ID="Button7" runat="server" Text="New" Width="20%" OnClick="Button7_Click" />&nbsp;
                    <asp:Button ID="Button8" runat="server" Enabled="False" Text="Edit" Width="20%" OnClick="Button8_Click" />&nbsp;&nbsp;
                    <asp:Button ID="Button10" runat="server" Text="Upload File" Width="20%" OnClick="Button10_Click1" />
                    </td>
            </tr>
            <tr>
                <td class="auto-style8"><strong>Symphony Number<span class="auto-style15">*</span>:</strong></td>
                <td width:10%; height: 20px;" class="auto-style9">
                    <asp:TextBox ID="TextBox1" runat="server" Width="80%" Enabled="False" AutoPostBack="True" OnTextChanged="TextBox1_TextChanged"></asp:TextBox>
                </td>
                <td class="auto-style16">&nbsp;</td>
                <td width:10%; height: 20px;" class="auto-style9">
                    <asp:TextBox ID="TextBox2" runat="server" Width="80%" Enabled="False" Text="N/A" Visible="false"></asp:TextBox>
                </td>
                <td class="auto-style16"><strong>Project Name<span class="auto-style15">*</span>:</strong></td>
                <td width:10%; height: 20px;" class="auto-style9">
                    <asp:TextBox ID="TextBox3" runat="server" Width="80%" Enabled="False"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td class="auto-style1"><strong>Project Type<span class="auto-style15">*</span>:</strong></td>
                <td width:10%; height: 20px;" class="auto-style5">
                    <asp:DropDownList ID="DropDownList1" runat="server" Width="80%" AutoPostBack="True" AppendDataBoundItems="True" OnSelectedIndexChanged="DropDownList1_SelectedIndexChanged" Enabled="False"  DataTextField="Project_Type" DataValueField="ID">
                   </asp:DropDownList>
                </td>
                <td class="auto-style7"><strong>Project Sub Type<span class="auto-style15">*</span>:</strong></td>
                <td width:10%; height: 20px;" class="auto-style5">
                    <asp:DropDownList ID="DropDownList2" runat="server" Width="80%" Enabled="False">
                    </asp:DropDownList>
                    </td>
                <td class="auto-style7"><strong>Product Type<span class="auto-style15">*</span>:</strong></td>
                <td width:10%; height: 20px;" class="auto-style5">
                    <asp:DropDownList ID="DropDownList16" runat="server" Width="80%" Enabled="False" DataTextField="Product_Type" DataValueField="ID">
                    </asp:DropDownList>
                    </td>
            </tr>
            <tr>
                <td class="auto-style1"><strong>Start Date<span class="auto-style15">*</span>:</strong></td>
                <td width:10%; height: 20px;" class="auto-style5">
                    <asp:TextBox ID="datepicker1" runat="server" Width="80%" Enabled="False" TextMode="Date"></asp:TextBox>
                </td>
                <td class="auto-style7"><strong>End Date<span class="auto-style15">*</span>:</strong></td>
                <td width:10%; height: 20px;" class="auto-style5">
                    <asp:TextBox ID="datepicker2" runat="server" Width="80%" Enabled="False" TextMode="Date"></asp:TextBox>
                </td>
                <td class="auto-style7"><strong>Client DB<span class="auto-style15">*</span>:</strong></td>
                <td width:10%; height: 20px;" class="auto-style5">
                    <asp:DropDownList ID="DropDownList17" runat="server" DataTextField="Product_Type" DataValueField="ID" Enabled="False" Width="80%">
                    </asp:DropDownList>
                </td>
            </tr>
            <tr>
                <td class="auto-style1"><strong>Client Name<span class="auto-style15">*</span>:</strong></td>
                <td 20px;"="" class="auto-style5" height:="" width:10%;="">


                    


                    <asp:DropDownList ID="DropDownList18" width="85%" runat="server"  Enabled="False">
                    </asp:DropDownList>
                  
                    &nbsp;</td>
                <td class="auto-style7"><strong>Market Sector<span class="auto-style15">*</span></strong>:</td>
                <td 20px;"="" class="auto-style5" height:="" width:10%;="">
                    <asp:DropDownList ID="DropDownList19" runat="server" Enabled="False" width="85%">
                    </asp:DropDownList>
                    &nbsp;</td>
                <td class="auto-style7">&nbsp;</td>
                <td 20px;"="" class="auto-style5" height:="" width:10%;="">
                    <asp:CheckBox ID="CheckBox1" runat="server" Text="Use iField" />
                </td>
            </tr>
            <tr>
                <td class="auto-style1"><strong>Project Owner Email</strong><span class="auto-style15">*</span>:</td>
                <td 20px;"="" class="auto-style17" colspan="3" height:="" width:10%;="">
                    <asp:TextBox ID="TextBox22" runat="server" Width="80%" TextMode="Email" Enabled="false"></asp:TextBox>
                </td>
                <td class="auto-style7">&nbsp;</td>
                <td 20px;"="" class="auto-style5" height:="" width:10%;="">&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style13" colspan="6">
                    <strong>
                    <asp:Button ID="Button2" runat="server" Enabled="False" Height="40px" OnClick="Button2_Click" Text="Submit" Width="229px" />
                    </strong>
                </td>
            </tr>
            <tr>
                <td class="auto-style13" colspan="6">
                    <asp:Panel ID="Panel3" runat="server" Visible="false">
                        <table style="width:100%;">
                            <tr>
                                <td class="auto-style20" style="width:20%">Upload File:</td>
                                <td style="width:40%">
                                    <asp:FileUpload ID="FileUpload1" runat="server" Width="80%" />
                                    <asp:Label ID="LblStatus" runat="server" ForeColor="Green" />
                                </td>
                                <td>
                                    <asp:Button ID="Button9" runat="server" OnClick="Button9_Click1" Text="Sync Data" Width="80%" />
                                </td>
                            </tr>
                        </table>
                    </asp:Panel>
                </td>
            </tr>
            </table>
    </asp:Panel>
</asp:Content>
