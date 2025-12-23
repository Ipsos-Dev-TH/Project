<%@ Page Title="Home" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Project.Default" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <link rel="stylesheet" href="/Content/jquery-ui.css">
    <script src="/Scripts/jquery-1.12.4.js"></script>
    <script src="/Scripts/jquery-ui.js"></script>

    <script>
        function setHourglass() {
            document.getElementById("MainContent_Button2").disabled = true;
            document.body.style.cursor = 'Wait';
        }
        $(function () {
            $("#MainContent_datepicker1").datepicker();
            $("#MainContent_datepicker2").datepicker();
        });
    </script>

    <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>

    <!-- Search Panel -->
    <asp:Panel ID="Panel1" runat="server">
        <div class="ipsos-card">
            <div class="ipsos-card-header">
                <i class="fas fa-search"></i> Search Projects
            </div>
            <div class="ipsos-card-body">
                <div class="ipsos-row">
                    <div class="ipsos-col-4">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label">Search By</label>
                            <asp:DropDownList ID="DropDownList15" runat="server" CssClass="ipsos-form-control">
                                <asp:ListItem Value="Symphony_Number">Symphony Number</asp:ListItem>
                                <asp:ListItem Value="Project_Name">Project Name</asp:ListItem>
                                <asp:ListItem Value="Project_Type">Type</asp:ListItem>
                                <asp:ListItem Value="SubType">Sub Type</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                    </div>
                    <div class="ipsos-col-4">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label">Search Term</label>
                            <asp:TextBox ID="TextBox21" runat="server" CssClass="ipsos-form-control"
                                placeholder="Enter search term..." AutoPostBack="True" OnTextChanged="TextBox21_TextChanged"></asp:TextBox>
                        </div>
                    </div>
                    <div class="ipsos-col-4">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label">&nbsp;</label>
                            <asp:Button ID="Button6" runat="server" OnClick="Button6_Click" Text="Search" CssClass="ipsos-btn ipsos-btn-primary" />
                        </div>
                    </div>
                </div>

                <!-- Search Results Grid -->
                <div class="ipsos-table-container" style="margin-top: 1rem;">
                    <asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="False" AllowPaging="True"
                        OnPageIndexChanging="GridView2_PageIndexChanging" OnSelectedIndexChanging="GridView2_SelectedIndexChanging"
                        CssClass="ipsos-table">
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
                </div>
            </div>
        </div>
    </asp:Panel>

    <!-- Project Form Panel -->
    <asp:Panel ID="Panel2" runat="server">
        <div class="ipsos-card" style="margin-top: 1.5rem;">
            <div class="ipsos-card-header">
                <i class="fas fa-project-diagram"></i> Project Details
                <div style="float: right;">
                    <asp:Button ID="Button7" runat="server" Text="New" OnClick="Button7_Click" CssClass="ipsos-btn ipsos-btn-success ipsos-btn-sm" />
                    <asp:Button ID="Button8" runat="server" Enabled="False" Text="Edit" OnClick="Button8_Click" CssClass="ipsos-btn ipsos-btn-warning ipsos-btn-sm" />
                    <asp:Button ID="Button10" runat="server" Text="Upload File" OnClick="Button10_Click1" CssClass="ipsos-btn ipsos-btn-info ipsos-btn-sm" />
                </div>
            </div>
            <div class="ipsos-card-body">
                <!-- Row 1: Symphony Number & Project Name -->
                <div class="ipsos-row">
                    <div class="ipsos-col-6">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label">Symphony Number <span class="ipsos-required">*</span></label>
                            <asp:TextBox ID="TextBox1" runat="server" CssClass="ipsos-form-control" Enabled="False"
                                AutoPostBack="True" OnTextChanged="TextBox1_TextChanged"></asp:TextBox>
                            <asp:TextBox ID="TextBox2" runat="server" CssClass="ipsos-form-control" Enabled="False"
                                Text="N/A" Visible="false"></asp:TextBox>
                        </div>
                    </div>
                    <div class="ipsos-col-6">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label">Project Name <span class="ipsos-required">*</span></label>
                            <asp:TextBox ID="TextBox3" runat="server" CssClass="ipsos-form-control" Enabled="False"></asp:TextBox>
                        </div>
                    </div>
                </div>

                <!-- Row 2: Project Type, Sub Type, Product Type -->
                <div class="ipsos-row">
                    <div class="ipsos-col-4">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label">Project Type <span class="ipsos-required">*</span></label>
                            <asp:DropDownList ID="DropDownList1" runat="server" CssClass="ipsos-form-control" AutoPostBack="True"
                                AppendDataBoundItems="True" OnSelectedIndexChanged="DropDownList1_SelectedIndexChanged" Enabled="False"
                                DataTextField="Project_Type" DataValueField="ID">
                            </asp:DropDownList>
                        </div>
                    </div>
                    <div class="ipsos-col-4">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label">Project Sub Type <span class="ipsos-required">*</span></label>
                            <asp:DropDownList ID="DropDownList2" runat="server" CssClass="ipsos-form-control" Enabled="False">
                            </asp:DropDownList>
                        </div>
                    </div>
                    <div class="ipsos-col-4">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label">Product Type <span class="ipsos-required">*</span></label>
                            <asp:DropDownList ID="DropDownList16" runat="server" CssClass="ipsos-form-control" Enabled="False"
                                DataTextField="Product_Type" DataValueField="ID">
                            </asp:DropDownList>
                        </div>
                    </div>
                </div>

                <!-- Row 3: Start Date, End Date, Client DB -->
                <div class="ipsos-row">
                    <div class="ipsos-col-4">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label">Start Date <span class="ipsos-required">*</span></label>
                            <asp:TextBox ID="datepicker1" runat="server" CssClass="ipsos-form-control" Enabled="False" TextMode="Date"></asp:TextBox>
                        </div>
                    </div>
                    <div class="ipsos-col-4">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label">End Date <span class="ipsos-required">*</span></label>
                            <asp:TextBox ID="datepicker2" runat="server" CssClass="ipsos-form-control" Enabled="False" TextMode="Date"></asp:TextBox>
                        </div>
                    </div>
                    <div class="ipsos-col-4">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label">Client DB <span class="ipsos-required">*</span></label>
                            <asp:DropDownList ID="DropDownList17" runat="server" CssClass="ipsos-form-control" DataTextField="Product_Type"
                                DataValueField="ID" Enabled="False">
                            </asp:DropDownList>
                        </div>
                    </div>
                </div>

                <!-- Row 4: Client Name, Market Sector, Use iField -->
                <div class="ipsos-row">
                    <div class="ipsos-col-4">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label">Client Name <span class="ipsos-required">*</span></label>
                            <asp:DropDownList ID="DropDownList18" runat="server" CssClass="ipsos-form-control" Enabled="False">
                            </asp:DropDownList>
                        </div>
                    </div>
                    <div class="ipsos-col-4">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label">Market Sector <span class="ipsos-required">*</span></label>
                            <asp:DropDownList ID="DropDownList19" runat="server" CssClass="ipsos-form-control" Enabled="False">
                            </asp:DropDownList>
                        </div>
                    </div>
                    <div class="ipsos-col-4">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label">&nbsp;</label>
                            <div style="padding-top: 0.5rem;">
                                <asp:CheckBox ID="CheckBox1" runat="server" Text=" Use iField" CssClass="ipsos-checkbox" />
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Row 5: Project Owner Email -->
                <div class="ipsos-row">
                    <div class="ipsos-col-6">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label">Project Owner Email <span class="ipsos-required">*</span></label>
                            <asp:TextBox ID="TextBox22" runat="server" CssClass="ipsos-form-control" TextMode="Email" Enabled="false"
                                placeholder="example@ipsos.com"></asp:TextBox>
                        </div>
                    </div>
                </div>

                <!-- Submit Button -->
                <div class="ipsos-row">
                    <div class="ipsos-col-12" style="text-align: center; margin-top: 1rem;">
                        <asp:Button ID="Button2" runat="server" Enabled="False" OnClick="Button2_Click" Text="Submit"
                            CssClass="ipsos-btn ipsos-btn-primary ipsos-btn-lg" />
                    </div>
                </div>

                <!-- File Upload Panel -->
                <asp:Panel ID="Panel3" runat="server" Visible="false">
                    <div class="ipsos-card" style="margin-top: 1rem; background: #f8f9fa;">
                        <div class="ipsos-card-body">
                            <div class="ipsos-row">
                                <div class="ipsos-col-4">
                                    <div class="ipsos-form-group">
                                        <label class="ipsos-form-label">Upload File</label>
                                        <asp:FileUpload ID="FileUpload1" runat="server" CssClass="ipsos-form-control" />
                                        <asp:Label ID="LblStatus" runat="server" ForeColor="Green" />
                                    </div>
                                </div>
                                <div class="ipsos-col-4">
                                    <div class="ipsos-form-group">
                                        <label class="ipsos-form-label">&nbsp;</label>
                                        <asp:Button ID="Button9" runat="server" OnClick="Button9_Click1" Text="Sync Data" CssClass="ipsos-btn ipsos-btn-info" />
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </asp:Panel>

    <style>
        .ipsos-required {
            color: #dc3545;
            font-weight: bold;
        }
        .ipsos-checkbox {
            font-size: 0.95rem;
            color: var(--ipsos-prussian-blue);
        }
        .ipsos-checkbox input[type="checkbox"] {
            width: 18px;
            height: 18px;
            margin-right: 0.5rem;
            accent-color: var(--ipsos-sapphire-blue);
        }
    </style>
</asp:Content>
