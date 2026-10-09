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
                                <asp:ListItem Value="ClientName">Client</asp:ListItem>
                                <asp:ListItem Value="SectorName">Market Sector</asp:ListItem>
                                <asp:ListItem Value="Project_Owner">Project Owner</asp:ListItem>
                                <asp:ListItem Value="Status">Status</asp:ListItem>
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
                            <asp:Button ID="BtnExportCsv" runat="server" OnClick="BtnExportCsv_Click" Text="Export CSV" CssClass="ipsos-btn ipsos-btn-secondary" />
                        </div>
                    </div>
                </div>

                <!-- Search Results Grid -->
                <div class="ipsos-table-container" style="margin-top: 1rem;">
                    <asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="False" AllowPaging="True" DataKeyNames="ID"
                        OnPageIndexChanging="GridView2_PageIndexChanging" OnSelectedIndexChanging="GridView2_SelectedIndexChanging"
                        CssClass="ipsos-table">
                        <Columns>
                            <asp:CommandField ButtonType="Button" SelectText="View" ShowSelectButton="True" />
                            <asp:BoundField DataField="ID" HeaderText="ID" />
                            <asp:BoundField DataField="Symphony_Number" HeaderText="Symphony Number" />
                            <asp:BoundField DataField="Project_Name" HeaderText="Project Name" />
                            <asp:BoundField DataField="Project_Type" HeaderText="Type" />
                            <asp:BoundField DataField="SubType" HeaderText="SubType" />
                            <asp:TemplateField HeaderText="Start Date">
                                <ItemTemplate><%#: FormatDate(Eval("Project_StartDate")) %></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="End Date">
                                <ItemTemplate><%#: FormatDate(Eval("Project_EndDate")) %></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Status">
                                <ItemTemplate>
                                    <span class='status-badge <%#: StatusCssClass(Eval("Status")) %>'>
                                        <%#: Eval("Status") %>
                                    </span>
                                </ItemTemplate>
                            </asp:TemplateField>
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
                    <asp:Button ID="BtnDelete" runat="server" Text="Delete" Visible="false" OnClick="BtnDelete_Click" CssClass="ipsos-btn ipsos-btn-danger ipsos-btn-sm"
                        OnClientClick="return confirm('ยืนยันการลบโปรเจกต์นี้? (Delete this project?)');" />
                </div>
            </div>
            <div class="ipsos-card-body">
                <asp:Label ID="LblFormMessage" runat="server" EnableViewState="false"></asp:Label>
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

                <!-- Row 5: Project Owner Email & Status -->
                <div class="ipsos-row">
                    <div class="ipsos-col-6">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label">Project Owner Email <span class="ipsos-required">*</span></label>
                            <asp:TextBox ID="TextBox22" runat="server" CssClass="ipsos-form-control" TextMode="Email" Enabled="false"
                                placeholder="example@ipsos.com"></asp:TextBox>
                        </div>
                    </div>
                    <div class="ipsos-col-6">
                        <div class="ipsos-form-group">
                            <label class="ipsos-form-label"><i class="fas fa-info-circle"></i> Status</label>
                            <div class="status-display">
                                <asp:Label ID="LblProjectStatus" runat="server" CssClass="status-badge-lg"></asp:Label>
                            </div>
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
                                        <asp:Button ID="BtnPreview" runat="server" OnClick="BtnPreview_Click" Text="Preview" CssClass="ipsos-btn ipsos-btn-secondary"
                                            ToolTip="ตรวจสอบไฟล์โดยไม่บันทึกข้อมูล (nothing is saved)" />
                                    </div>
                                </div>
                            </div>
                            <div class="ipsos-row">
                                <div class="ipsos-col-12">
                                    <label class="ipsos-form-label"><i class="fas fa-history"></i> Last 10 uploads</label>
                                    <div class="ipsos-table-container upload-history">
                                        <asp:GridView ID="GridViewUploadHistory" runat="server" AutoGenerateColumns="False" CssClass="ipsos-table"
                                            EmptyDataText="No uploads yet.">
                                            <Columns>
                                                <asp:BoundField DataField="Uploaded_Time" HeaderText="Date/Time" />
                                                <asp:BoundField DataField="File_Name" HeaderText="File" />
                                                <asp:BoundField DataField="Uploaded_By" HeaderText="By" />
                                                <asp:BoundField DataField="Run_Type" HeaderText="Type" />
                                                <asp:BoundField DataField="Total_Rows" HeaderText="Rows" />
                                                <asp:BoundField DataField="Inserted_Count" HeaderText="Inserted" />
                                                <asp:BoundField DataField="Updated_Count" HeaderText="Updated" />
                                                <asp:BoundField DataField="Skipped_Count" HeaderText="Skipped" />
                                                <asp:BoundField DataField="Error_Count" HeaderText="Errors" />
                                            </Columns>
                                        </asp:GridView>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </asp:Panel>

    <!-- Project History Panel (View mode) -->
    <asp:Panel ID="PanelHistory" runat="server" Visible="false">
        <div class="ipsos-card" style="margin-top: 1.5rem;">
            <div class="ipsos-card-header">
                <i class="fas fa-history"></i> Project History
            </div>
            <div class="ipsos-card-body">
                <div class="ipsos-table-container project-history">
                    <asp:GridView ID="GridViewHistory" runat="server" AutoGenerateColumns="False" CssClass="ipsos-table"
                        EmptyDataText="No history for this project.">
                        <Columns>
                            <asp:BoundField DataField="Action_Time" HeaderText="Date/Time" />
                            <asp:BoundField DataField="Action_By" HeaderText="By" />
                            <asp:BoundField DataField="Task" HeaderText="Task" />
                            <asp:BoundField DataField="Data" HeaderText="Detail" />
                        </Columns>
                    </asp:GridView>
                </div>
            </div>
        </div>
    </asp:Panel>

    <style>
        .ipsos-required {
            color: #dc3545;
            font-weight: bold;
        }
        .ipsos-checkbox {
            font-size: 1.05rem;
            color: var(--ipsos-prussian-blue);
        }
        .ipsos-checkbox input[type="checkbox"] {
            width: 20px;
            height: 20px;
            margin-right: 0.5rem;
            accent-color: var(--ipsos-sapphire-blue);
        }
        /* Status Badge Styles */
        .status-badge {
            display: inline-block;
            padding: 0.35rem 0.85rem;
            font-size: 0.85rem;
            font-weight: 600;
            border-radius: 50px;
            text-transform: uppercase;
        }
        .status-badge-lg {
            display: inline-block;
            padding: 0.6rem 1.5rem;
            font-size: 1rem;
            font-weight: 600;
            border-radius: 50px;
            text-transform: uppercase;
        }
        .status-display {
            padding: 0.5rem 0;
        }
        /* Status Colors */
        .status-new, .status-badge-new {
            background-color: #e3f2fd;
            color: #1565c0;
        }
        .status-active, .status-badge-active {
            background-color: #e8f5e9;
            color: #2e7d32;
        }
        .status-open, .status-badge-open {
            background-color: #e8f5e9;
            color: #2e7d32;
        }
        .status-in-progress, .status-badge-in-progress {
            background-color: #fff3e0;
            color: #ef6c00;
        }
        .status-completed, .status-badge-completed {
            background-color: #e0f2f1;
            color: #00695c;
        }
        .status-closed, .status-badge-closed {
            background-color: #eceff1;
            color: #546e7a;
        }
        .status-cancelled, .status-badge-cancelled {
            background-color: #ffebee;
            color: #c62828;
        }
        .status-on-hold, .status-badge-on-hold {
            background-color: #fce4ec;
            color: #ad1457;
        }
        .status-pending, .status-badge-pending {
            background-color: #fff8e1;
            color: #ff8f00;
        }
        .status-unknown, .status-badge-unknown {
            background-color: #f5f5f5;
            color: #757575;
        }
        /* Upload Result Message */
        .upload-result {
            margin-top: 1rem;
            padding: 1rem;
            border-radius: 8px;
        }
        .upload-result.success {
            background-color: #e8f5e9;
            border: 1px solid #a5d6a7;
            color: #2e7d32;
        }
        .upload-result.error {
            background-color: #ffebee;
            border: 1px solid #ef9a9a;
            color: #c62828;
        }
        .upload-result.warning {
            background-color: #fff3e0;
            border: 1px solid #ffcc80;
            color: #ef6c00;
        }
    </style>
</asp:Content>
