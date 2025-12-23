using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Configuration;
using System.IO;
using System.Data.SqlClient;
using System.Web.Services;
using System.Web.Script.Services;
using Project;
using System.Data.OleDb;
using ExcelDataReader;
using HtmlAgilityPack;
using System.Globalization;

namespace Project
{
    public partial class Default : System.Web.UI.Page
    {
        SQL sql = new SQL();
        Core Core = new Core();
        string conn = ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString;
        DataTable dtCost = new DataTable();
        string Email = "";
        protected void Page_Load(object sender, EventArgs e)
        {


            string Command = Request.QueryString["Command"];
            string ID = Request.QueryString["ID"];

            if (!IsPostBack)
            {
                DataTable dtnull = null;
                DropDownList1.DataSource = dtnull;
                DropDownList1.DataBind();
                DataTable dttype = Core.DatabaseQuery(conn, sql.Select_Project_Type_all);
                DropDownList1.DataSource = dttype;
                DataRow dr = dttype.NewRow();
                dr["Project_Type"] = "---Please Choose---";
                dr["ID"] = 0;
                dttype.Rows.InsertAt(dr, 0);
                DropDownList1.DataTextField = "Project_Type";
                DropDownList1.DataValueField = "ID";
                DropDownList1.DataBind();

                DropDownList16.DataSource = dtnull;
                DropDownList16.DataBind();
                DataTable dtproducttype = Core.DatabaseQuery(conn, sql.Select_Product_Type_all);
                DropDownList16.DataSource = dtproducttype;
                DataRow dr2 = dtproducttype.NewRow();
                dr2["Product_Type"] = "---Please Choose---";
                dr2["ID"] = 0;
                dtproducttype.Rows.InsertAt(dr2, 0);
                DropDownList16.DataTextField = "Product_Type";
                DropDownList16.DataValueField = "ID";
                DropDownList16.DataBind();

                DropDownList17.DataSource = dtnull;
                DropDownList17.DataBind();
                DataTable dtclientdb = Core.DatabaseQuery(conn, "select * from ClientDB Where Status = 'True'");
                DropDownList17.DataSource = dtclientdb;
                DataRow drx = dtclientdb.NewRow();
                drx["Name"] = "---Please Choose---";
                drx["ID"] = 0;
                dtclientdb.Rows.InsertAt(drx, 0);
                DropDownList17.DataTextField = "Name";
                DropDownList17.DataValueField = "ID";
                DropDownList17.DataBind();

                DropDownList18.DataSource = dtnull;
                DropDownList18.DataBind();
                DataTable dtclientname = Core.DatabaseQuery(conn, "SELECT * FROM [ClientName] WHERE [Status] = 1 order by ClientName ASC");
                DropDownList18.DataSource = dtclientname;
                DataRow dry = dtclientname.NewRow();
                dry["ClientName"] = "---Please Choose---";
                dry["ID"] = 0;
                dtclientname.Rows.InsertAt(dry, 0);
                DropDownList18.DataTextField = "ClientName";
                DropDownList18.DataValueField = "ID";
                DropDownList18.DataBind();

                DropDownList19.DataSource = dtnull;
                DropDownList19.DataBind();
                DataTable dtmarketsector = Core.DatabaseQuery(conn, "SELECT * FROM [MarketSector] WHERE [Status] = 1 order by SectorName ASC");
                DropDownList19.DataSource = dtmarketsector;
                DataRow drz = dtmarketsector.NewRow();
                drz["SectorName"] = "---Please Choose---";
                drz["ID"] = 0;
                dtmarketsector.Rows.InsertAt(drz, 0);
                DropDownList19.DataTextField = "SectorName";
                DropDownList19.DataValueField = "ID";
                DropDownList19.DataBind();


                if (Command == "View" || Command == "Edit")
                {

                    ID = Request.QueryString["ID"];
                    Button7.Enabled = false;
                    Button8.Enabled = true;

                    DataTable dtData = Core.DatabaseQuery(conn, sql.Select_Project_byIDSelected.Replace("@IDSelected", ID));
                    Session["dtData"] = dtData;
                    TextBox1.Text = dtData.Rows[0]["Symphony_Number"].ToString();
                    TextBox2.Text = dtData.Rows[0]["Project_Number"].ToString();
                    TextBox3.Text = dtData.Rows[0]["Project_Name"].ToString();
                    TextBox22.Text = dtData.Rows[0]["Project_Owner"].ToString();
                    //TextBox22.Text = dtData.Rows[0]["ClientName"].ToString();
                    //TextBox23.Text = dtData.Rows[0]["SectorName"].ToString();
                    try
                    {
                        DateTime stDate = (DateTime)dtData.Rows[0]["Project_StartDate"];
                        datepicker1.Text = stDate.ToString("yyyy-MM-dd");
                    }
                    catch
                    {
                        datepicker1.Text = "";
                    }
                    try
                    {
                        DateTime edDate = (DateTime)dtData.Rows[0]["Project_EndDate"];
                        datepicker2.Text = edDate.ToString("yyyy-MM-dd");
                    }
                    catch
                    {
                        datepicker2.Text = "";
                    }
                    try
                    {
                        DropDownList1.ClearSelection();
                        DropDownList1.Items.FindByValue(dtData.Rows[0]["Project_Type_ID"].ToString()).Selected = true;
                        DropDownList1.SelectedIndex = DropDownList1.Items.IndexOf(DropDownList1.Items.FindByValue(dtData.Rows[0]["Project_Type_ID"].ToString()));

                        DropDownList16.ClearSelection();
                        DropDownList16.Items.FindByValue(dtData.Rows[0]["Product_Type_ID"].ToString()).Selected = true;
                        DropDownList16.SelectedIndex = DropDownList16.Items.IndexOf(DropDownList16.Items.FindByValue(dtData.Rows[0]["Product_Type_ID"].ToString()));

                        DropDownList18.ClearSelection();
                        DropDownList18.Items.FindByText(dtData.Rows[0]["ClientName"].ToString()).Selected = true;
                        DropDownList18.SelectedIndex = DropDownList18.Items.IndexOf(DropDownList18.Items.FindByText(dtData.Rows[0]["ClientName"].ToString()));

                        DropDownList19.ClearSelection();
                        DropDownList19.Items.FindByText(dtData.Rows[0]["SectorName"].ToString()).Selected = true;
                        DropDownList19.SelectedIndex = DropDownList19.Items.IndexOf(DropDownList19.Items.FindByText(dtData.Rows[0]["SectorName"].ToString()));

                        DropDownList17.ClearSelection();
                        DropDownList17.Items.FindByValue(dtData.Rows[0]["ClientDBID"].ToString()).Selected = true;
                        DropDownList17.SelectedIndex = DropDownList17.Items.IndexOf(DropDownList17.Items.FindByValue(dtData.Rows[0]["ClientDBID"].ToString()));

                        CheckBox1.Checked = Convert.ToBoolean(dtData.Rows[0]["Used_iField"].ToString());
                        CheckBox1.DataBind();

                    }
                    catch { }
                    try
                    {
                        DropDownList2.DataSource = dtnull;
                        DropDownList2.DataBind();
                        DataTable dtsub = Core.DatabaseQuery(conn, sql.Select_Project_SubType_byddl1.Replace("@ddl1", DropDownList1.SelectedItem.Value));
                        DropDownList2.DataSource = dtsub;
                        DataRow dr3 = dtsub.NewRow();
                        dr3["SubType"] = "---Please Choose---";
                        dr3["ID"] = 0;
                        dtsub.Rows.InsertAt(dr3, 0);
                        DropDownList2.DataTextField = "SubType";
                        DropDownList2.DataValueField = "ID";
                        DropDownList2.DataBind();
                        DropDownList2.ClearSelection();
                        DropDownList2.Items.FindByValue(dtData.Rows[0]["Project_SubType_ID"].ToString()).Selected = true;
                        DropDownList2.SelectedIndex = DropDownList2.Items.IndexOf(DropDownList2.Items.FindByValue(dtData.Rows[0]["Project_SubType_ID"].ToString()));
                    }
                    catch { }
                    if (Command == "Edit")
                    {
                        EnableControl();
                    }
                }
            }
        }

        public void EnableControl()
        {
            TextBox1.Enabled = true;
            //TextBox2.Enabled = true;
            TextBox3.Enabled = true;
            TextBox21.Enabled = true;
            TextBox22.Enabled = true;
            //TextBox23.Enabled = true;
            datepicker1.Enabled = true;
            datepicker2.Enabled = true;
            DropDownList1.Enabled = true;
            DropDownList2.Enabled = true;
            DropDownList16.Enabled = true;
            DropDownList15.Enabled = true;
            DropDownList17.Enabled = true;
            DropDownList18.Enabled = true;
            DropDownList19.Enabled = true;
            Button2.Enabled = true;


        }

        [System.Web.Script.Services.ScriptMethod()]
        [System.Web.Services.WebMethod]
        public List<string> GetCompletionList(string prefixText, int count)
        {
            Core Core = new Core();
            string conn = ConfigurationManager.ConnectionStrings["ProjectConnectionString"].ConnectionString;

            DataTable dt = Core.DatabaseQuery(conn, "Select [ClientName] from ClientName Where Status = 1 And ClientName like '%" + prefixText + "%'");
            List<string> names = new List<string>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                names.Add(dt.Rows[i][0].ToString());
            }
            return names;
        }

        protected void Button2_Click(object sender, EventArgs e)
        {
            string Command = Request.QueryString["Command"];
            string ID = Request.QueryString["ID"];

            string query = "";
            if (TextBox1.Text != string.Empty && DropDownList1.SelectedIndex > 0 && DropDownList2.SelectedIndex > 0 && DropDownList17.SelectedIndex > 0 && DropDownList18.SelectedIndex > 0 && DropDownList19.SelectedIndex > 0 && TextBox22.Text.Length > 6)
            {
                if (Command == "Edit")
                {
                    query = sql.Update_Project_byID.Replace("@IDSelected", ID);
                }
                else
                {
                    DataTable dtcheck = Core.DatabaseQuery(conn, sql.Select_Project_CheckExist.Replace("@Symphony_Number", TextBox1.Text));
                    if (dtcheck.Rows.Count > 0)
                    {
                        ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('เลขโปรเจกต์ซ้ำหรือไม่ถูกต้อง');", true);
                    }
                    else
                    {
                        query = sql.Insert_Project_new;

                    }
                }
                try
                {
                    using (SqlConnection con = new SqlConnection(conn))
                    {
                        using (SqlCommand cmd = new SqlCommand(query))
                        {
                            cmd.Connection = con;
                            cmd.Parameters.AddWithValue("@Symphony_Number", TextBox1.Text);
                            cmd.Parameters.AddWithValue("@Project_Number", TextBox2.Text);
                            cmd.Parameters.AddWithValue("@Project_Name", TextBox3.Text);
                            cmd.Parameters.AddWithValue("@Project_Type_ID", DropDownList1.SelectedItem.Value);
                            cmd.Parameters.AddWithValue("@Project_SubType_ID", DropDownList2.SelectedItem.Value);
                            cmd.Parameters.AddWithValue("@Product_Type_ID", DropDownList16.SelectedItem.Value);
                            cmd.Parameters.AddWithValue("@Project_StartDate", string.IsNullOrWhiteSpace(datepicker1.Text) ? DBNull.Value : (object)datepicker1.Text);
                            cmd.Parameters.AddWithValue("@Project_EndDate", string.IsNullOrWhiteSpace(datepicker2.Text) ? DBNull.Value : (object)datepicker2.Text);
                            cmd.Parameters.AddWithValue("@ClientDBID", DropDownList17.SelectedValue);
                            cmd.Parameters.AddWithValue("@ClientName_ID", DropDownList18.SelectedValue);
                            cmd.Parameters.AddWithValue("@MarketSector_ID", DropDownList19.SelectedValue);
                            cmd.Parameters.AddWithValue("@Used_iField", CheckBox1.Checked);
                            cmd.Parameters.AddWithValue("@Project_Owner", TextBox22.Text);

                            cmd.Parameters.AddWithValue("@Status", "NEW");
                            if (Command == "Edit")
                            {

                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@Creator", Email);
                                cmd.Parameters.AddWithValue("@Created_DateTime", DateTime.Now);
                            }
                            con.Open();
                            cmd.ExecuteNonQuery();
                            con.Close();


                            if (Command == "Edit")
                            {
                                DataTable dtData = (DataTable)Session["dtData"];
                                string DataChanged = "ID-" + dtData.Rows[0]["ID"].ToString() + "{";
                                if (dtData.Rows[0]["Symphony_Number"].ToString() != TextBox1.Text)
                                {
                                    DataChanged += "Symphony_Number of ID[" + dtData.Rows[0]["Symphony_Number"].ToString() + "," + TextBox1.Text + "];";
                                }
                                if (dtData.Rows[0]["Project_Number"].ToString() != TextBox2.Text)
                                {
                                    DataChanged += "Project_Number[" + dtData.Rows[0]["Project_Number"].ToString() + "," + TextBox2.Text + "];";
                                }
                                if (dtData.Rows[0]["Project_Name"].ToString() != TextBox3.Text)
                                {
                                    DataChanged += "Project_Name[" + dtData.Rows[0]["Project_Name"].ToString() + "," + TextBox3.Text + "];";
                                }
                                if (dtData.Rows[0]["Project_Type_ID"].ToString() != DropDownList1.SelectedItem.Value)
                                {
                                    DataChanged += "Project_Type_ID[" + dtData.Rows[0]["Project_Type_ID"].ToString() + "," + DropDownList1.SelectedItem.Value + "];";
                                }
                                if (dtData.Rows[0]["Project_SubType_ID"].ToString() != DropDownList2.SelectedItem.Value)
                                {
                                    DataChanged += "Project_SubType_ID[" + dtData.Rows[0]["Project_SubType_ID"].ToString() + "," + DropDownList2.SelectedItem.Value + "];";
                                }
                                if (dtData.Rows[0]["Product_Type_ID"].ToString() != DropDownList16.SelectedItem.Value)
                                {
                                    DataChanged += "Product_Type_ID[" + dtData.Rows[0]["Product_Type_ID"].ToString() + "," + DropDownList16.SelectedItem.Value + "];";
                                }
                                if (((DateTime)dtData.Rows[0]["Project_StartDate"]).ToString("yyyy-MM-dd") != datepicker1.Text)
                                {
                                    DataChanged += "Project_StartDate[" + dtData.Rows[0]["Project_StartDate"].ToString() + "," + datepicker1.Text + "];";
                                }
                                if (((DateTime)dtData.Rows[0]["Project_EndDate"]).ToString("yyyy-MM-dd") != datepicker2.Text)
                                {
                                    DataChanged += "Project_EndDate[" + dtData.Rows[0]["Project_EndDate"].ToString() + "," + datepicker2.Text + "];";
                                }
                                DataChanged += "}";
                                Core.Logs(conn, "Project", "Update", DataChanged, Session["Email"].ToString());
                            }
                            else
                            {
                                Core.Logs(conn, "Project", "Insert", "Insert Symphony_Number = " + TextBox1.Text, Session["Email"].ToString());
                            }
                        }
                    }





                }
                catch { ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('คุณกรอกข้อมูลไม่ครบถ้วนตามที่ระบบต้องการ');", true); }
                string mainpath = "";
                if (System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString() == "" || System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString() == string.Empty)
                {
                }
                else
                {
                    mainpath = System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString();
                }
                Response.Redirect("/Home");
            }
            else
            {
                ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('คุณกรอกข้อมูลไม่ครบถ้วนตามที่ระบบต้องการ');", true);
            }
        }

        protected void DropDownList1_SelectedIndexChanged(object sender, EventArgs e)
        {
            DataTable dt = Core.DatabaseQuery(conn, "SELECT * FROM [Project].[dbo].[Project_SubType] Where [Status] = 1 AND Project_Type_ID = " + DropDownList1.SelectedItem.Value + " order by SubType ASC");
            DataRow dr = dt.NewRow();
            dr["SubType"] = "---Please Choose---";
            dr["ID"] = 0;
            dt.Rows.InsertAt(dr, 0);
            DropDownList2.DataSource = dt;
            DropDownList2.DataTextField = "SubType";
            DropDownList2.DataValueField = "ID";
            DropDownList2.DataBind();
        }

        protected void GridView1_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            dtCost = (DataTable)Session["dtCost"];
            DataTable dtSearch = Core.DatabaseQuery(conn, "SELECT * FROM [Payment].[dbo].[Payment] Where Project_Cost_ID = " + dtCost.Rows[e.RowIndex]["ID"].ToString() + " AND (Status = 'NEW' OR Status = 'EDITED' OR Status = 'APPROVED')");
            if (dtSearch.Rows.Count > 0)
            {
                ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('Project Cost นี้มีการทำจ่ายแล้วไม่สามารถลบได้');", true);
            }
            else
            {
                Core.DatabaseInsert(conn, "DELETE FROM [dbo].[Project_Cost] WHERE ID = " + dtCost.Rows[e.RowIndex]["ID"].ToString());
                dtCost.Rows[e.RowIndex].Delete();
                dtCost.AcceptChanges();

                Session["dtCost"] = dtCost;
            }

        }

        protected void Button6_Click(object sender, EventArgs e)
        {
            DataTable dtSearch = Core.DatabaseQuery(conn, sql.Select_Project_byColumn.Replace("@Column", DropDownList15.SelectedValue).Replace("@Value", TextBox21.Text));
            GridView2.DataSource = dtSearch;
            GridView2.DataBind();
        }

        protected void GridView2_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            DataTable dtSearch = Core.DatabaseQuery(conn, "SELECT * FROM [Project].[dbo].[Project] inner join Project_Type on Project_Type.ID = Project_Type_ID inner join Project_SubType on Project_SubType.ID = Project_SubType_ID WHERE " + DropDownList15.SelectedItem.Value + " LIKE N'%" + TextBox21.Text + "%'");
            GridView2.DataSource = dtSearch;
            GridView2.PageIndex = e.NewPageIndex;
            GridView2.DataBind();
        }

        protected void Button7_Click(object sender, EventArgs e)
        {
            EnableControl();
            Panel1.Visible = false;
        }

        protected void Button8_Click(object sender, EventArgs e)
        {
            string mainpath = "";
            if (System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString() == "" || System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString() == string.Empty)
            {
            }
            else
            {
                mainpath = System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString();
            }
            Panel1.Visible = false;
            string ID = Request.QueryString["ID"];
            Response.Redirect("/Home/Default.aspx?Command=Edit&ID=" + ID);
        }

        protected void GridView2_SelectedIndexChanging(object sender, GridViewSelectEventArgs e)
        {
            string mainpath = "";
            if (System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString() == "" || System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString() == string.Empty)
            {
            }
            else
            {
                mainpath = System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString();
            }
            Response.Redirect("/Home/Default.aspx?Command=View&ID=" + GridView2.Rows[e.NewSelectedIndex].Cells[1].Text);
        }



        protected void TextBox1_TextChanged(object sender, EventArgs e)
        {
            TextBox1.Text = ChangeSymNumber(TextBox1.Text);
        }

        public string ChangeSymNumber(string SymNumber)
        {
            if (SymNumber.Length > 0)
            {
                SymNumber = SymNumber.Replace("-", "");
                if (SymNumber.Length >= 8)
                {
                    if (SymNumber.Length == 8)
                    {
                        SymNumber = SymNumber.Insert(2, "-");
                    }
                    else if (SymNumber.Length == 10)
                    {
                        SymNumber = SymNumber.Insert(2, "-");
                        SymNumber = SymNumber.Insert(9, "-");
                    }
                    else if (SymNumber.Length == 12)
                    {
                        SymNumber = SymNumber.Insert(2, "-");
                        SymNumber = SymNumber.Insert(9, "-");
                        SymNumber = SymNumber.Insert(12, "-");
                    }
                    else if (SymNumber.Length == 14)
                    {
                        SymNumber = SymNumber.Insert(2, "-");
                        SymNumber = SymNumber.Insert(9, "-");
                        SymNumber = SymNumber.Insert(12, "-");
                        SymNumber = SymNumber.Insert(15, "-");
                    }
                    else if (SymNumber.Length == 16)
                    {
                        SymNumber = SymNumber.Insert(2, "-");
                        SymNumber = SymNumber.Insert(9, "-");
                        SymNumber = SymNumber.Insert(12, "-");
                        SymNumber = SymNumber.Insert(15, "-");
                        SymNumber = SymNumber.Insert(18, "-");
                    }
                    else
                    {
                        SymNumber = string.Empty;
                    }
                }
                else
                {
                    SymNumber = string.Empty;
                }

            }
            return SymNumber;
        }

        public DataTable FindChangedFrom2Datatable(DataTable OldData, DataTable NewData)
        {

            DataTable ChangedTable = new DataTable();
            ChangedTable.Columns.Add("ColumnName");
            ChangedTable.Columns.Add("OldValue");
            ChangedTable.Columns.Add("NewValue");
            for (int j = 0; j < OldData.Columns.Count; j++)
            {
                for (int k = 0; k < NewData.Columns.Count; k++)
                {
                    if (OldData.Columns[j].ColumnName == NewData.Columns[k].ColumnName)
                    {
                        if (OldData.Rows[0][j].ToString() == NewData.Rows[0][k].ToString())
                        {

                        }
                        else
                        {
                            ChangedTable.Rows.Add(OldData.Columns[j].ColumnName, OldData.Rows[0][j], NewData.Rows[0][k]);
                        }
                    }
                    else
                    {

                    }

                }
            }



            return ChangedTable;
        }

        protected void Button9_Click(object sender, EventArgs e)
        {

        }

        protected void Button10_Click(object sender, EventArgs e)
        {

        }

        protected void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        protected void Button11_Click(object sender, EventArgs e)
        {
            string mainpath = "";
            if (System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString() == "" || System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString() == string.Empty)
            {
            }
            else
            {
                mainpath = System.Configuration.ConfigurationSettings.AppSettings["DefaultURL"].ToString();
            }
            Response.Redirect("/Project/Import-FW");
        }

        protected void TextBox21_TextChanged(object sender, EventArgs e)
        {
            Button6_Click(null, null);
        }

        
        protected void Button9_Click1(object sender, EventArgs e)
        {
            if (FileUpload1.HasFile)
            {
                try
                {

                    string fileName = Path.GetFileName(FileUpload1.FileName);
                    string filePath = Server.MapPath("~/Upload/") + fileName;
                    FileUpload1.SaveAs(filePath);

                    // Read Excel
                    DataTable dt = new DataTable();
                    try
                    {
                        dt = ReadExcelFile(filePath);
                    }
                    catch
                    {
                        string htmlContent = System.IO.File.ReadAllText(filePath);

                        dt = HtmlToDataTable(htmlContent);

                    }
                    // Insert to DB

                    InsertDataToDatabase(dt);

                    LblStatus.Text = "Upload and import successful!";


                }
                catch (Exception ex)
                {

                    LblStatus.ForeColor = System.Drawing.Color.Red;
                    LblStatus.Text = "Error: " + ex.Message;
                }
                
            }
            else
            {
                LblStatus.ForeColor = System.Drawing.Color.Red;
                LblStatus.Text = "Please select a file to upload.";
            }
        }

        private DataTable ReadExcelFile(string filePath)
        {
                using (var stream = FileUpload1.FileContent)
                {
                    DataTable dt = ReadExcelFile(stream);
                    InsertDataToDatabase(dt); // your method
                    return dt;
                }
            
           

        }

        public DataTable ReadExcelFile(Stream stream)
        {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet(new ExcelDataReader.ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataReader.ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true,
                            ReadHeaderRow = rowReader =>
                            {
                                // Example: skip first 4 rows so headers come from row 5
                                for (int i = 0; i < 6; i++)
                                    rowReader.Read();
                            }
                        }
                    });

                    DataTable dt = result.Tables[0];
                    InsertDataToDatabase(dt);
                    return dt;
                }
           
        }

        private void InsertDataToDatabase(DataTable dataTable)
        {
            string connectionString = conn.ToString();

            using (SqlConnection conm = new SqlConnection(connectionString))
            {
                conm.Open();

                foreach (DataRow row in dataTable.Rows)
                {
                    try
                    {
                        // Adjust column names to match your Excel and DB schema
                        string Symphony_Number = row["Job ID"].ToString();
                        string Project_Name = row["Record Detail"].ToString().Replace(Symphony_Number + " - ", "");
                        string Status = row["JobBook Status"].ToString();
                        string ClientID = "0";
                        string ClientName = row["Billing Client Name"].ToString();
                        string Project_Owner = "";
                        try {
                        Project_Owner = row["Job Owner"].ToString().Split(',')[1].Replace(" ", "") + "." + row["Job Owner"].ToString().Split(',')[0].Replace(" ", "") + "@ipsos.com";
                        }
                        catch { }
                        if (Project_Owner.Length == 0)
                        {
                            try {
                                Project_Owner = row["Job Manager"].ToString().Split(',')[1].Replace(" ", "") + "." + row["Job Manager"].ToString().Split(',')[0].Replace(" ", "") + "@ipsos.com";
                            }
                            catch { }
                        }
                        if (Project_Owner.Length == 0)
                        {
                            try {
                                Project_Owner = row["Job Owner"].ToString().Split(',')[1].Replace(" ", "") + "." + row["Job Owner"].ToString().Split(',')[0].Replace(" ", "") + "@ipsos.com";
                            }
                            catch { }
                        }
                        string MarketSectorID = "0";
                        string MarketSector = row["Client Sector"].ToString();
                        string ProjectTypeID = "0";
                        string Project_Type = row["DC Technique Group"].ToString();
                        string SubProjectTypeID = "0";
                        string SubProject_Type = row["DC Mode"].ToString();
                        string StartDate = row["Job Start Date"].ToString();
                        string EndDate = row["Job End Date"].ToString();

                        DataTable dtClient = Core.DatabaseQuery(conn, "SELECT * FROM [Project].[dbo].[ClientName] Where ClientName like '" + ClientName.Replace("'", "''").Replace("&amp;", "&") + "'");
                        if (dtClient.Rows.Count > 0)
                        {
                            ClientID = dtClient.Rows[0]["ID"].ToString();
                        }
                        else
                        {
                            ClientID = Core.ExecuteScalar(conn, "INSERT INTO [Project].[dbo].[ClientName] ([ClientName]) VALUES ('" + ClientName.Replace("'", "''").Replace("&amp;", "&") + "');").ToString();
                        }

                        DataTable dtMarketSector = Core.DatabaseQuery(conn, "SELECT * FROM [Project].[dbo].[MarketSector] Where SectorName like '" + MarketSector.Replace("&amp;", "&") + "'");
                        if (dtMarketSector.Rows.Count > 0)
                        {
                            MarketSectorID = dtMarketSector.Rows[0]["ID"].ToString();
                        }
                        else
                        {
                            MarketSectorID = Core.ExecuteScalar(conn, "INSERT INTO [Project].[dbo].[MarketSector] ([SectorName]) VALUES ('" + MarketSector.Replace("&amp;", "&") + "');").ToString();
                        }

                        DataTable dtSubProjectType = Core.DatabaseQuery(conn, "SELECT * FROM [Project].[dbo].[Project_SubType] Where SubType like '" + SubProject_Type.Replace("&amp;", "&") + "'");
                        if (dtSubProjectType.Rows.Count > 0)
                        {
                            ProjectTypeID = dtSubProjectType.Rows[0]["Project_Type_ID"].ToString();
                            SubProjectTypeID = dtSubProjectType.Rows[0]["ID"].ToString();
                        }
                        else
                        {
                            ProjectTypeID = Core.ExecuteScalar(conn, "INSERT INTO [Project].[dbo].[Project_Type] ([Project_Type]) VALUES ('" + Project_Type.Replace("&amp;", "&") + "');").ToString();
                            SubProjectTypeID = Core.ExecuteScalar(conn, "INSERT INTO [Project].[dbo].[Project_SubType] ([Project_Type_ID],[SubType]) VALUES (" + ProjectTypeID + ",'" + SubProject_Type.Replace("&amp;", "&") + "');").ToString();
                        }

                        DataTable dtProjectType = Core.DatabaseQuery(conn, "SELECT * FROM [Project].[dbo].[Project_Type] Where Project_Type like '" + Project_Type.Replace("&amp;", "&") + "'");
                        if (dtProjectType.Rows.Count > 0)
                        {
                            ProjectTypeID = dtProjectType.Rows[0]["ID"].ToString();
                        }
                        else
                        {
                            ProjectTypeID = Core.ExecuteScalar(conn, "INSERT INTO [Project].[dbo].[Project_Type] ([Project_Type]) VALUES ('" + Project_Type + "');").ToString();
                        }


                        DataTable dtProject = Core.DatabaseQuery(conn, "SELECT * FROM [Project].[dbo].[Project] Where REPLACE(Symphony_Number, '-', '') = '" + row["Job ID"].ToString() + "'");

                        string ProjectStartDate = "";
                        string ProjectEndDate = "";
                        bool a = true;
                        bool b = true;
                        try
                        {
                            ProjectStartDate = Convert.ToDateTime(StartDate).ToString("yyyy-MM-dd");
                        }
                        catch
                        {
                            a = false;
                        }

                        try
                        {
                            ProjectEndDate = Convert.ToDateTime(EndDate).ToString("yyyy-MM-dd");
                        }
                        catch
                        {
                            b = false;
                        }
                        DateTime parsedDate;
                        if (DateTime.TryParseExact(EndDate, "yyyy-MM-dd",
    CultureInfo.InvariantCulture,
    DateTimeStyles.None,
    out parsedDate))
                        {
                            ProjectEndDate = parsedDate.ToString("yyyy-MM-dd");
                            b = true;
                        }
                        else
                        {
                            b = false;
                        }
                        try
                        {
                            if (dtProject.Rows.Count > 0 && a == true && b == true && Symphony_Number.Length > 0)
                            {
                                Core.DatabaseInsert(conn, "UPDATE [dbo].[Project] SET  [Project_StartDate] = '" + ProjectStartDate + "' ,[Project_EndDate] = '" + ProjectEndDate + "', [Status] = '" + Status + "', [Project_Owner] = '"+Project_Owner+ "',ClientName_ID = '"+ClientID+ "',Creator = '" + Session["Email"].ToString() +"' Where REPLACE(Symphony_Number, '-', '') = '" + row["Job ID"].ToString() + "'");
                            }
                            else if (dtProject.Rows.Count > 0 && a == true && b == false && Symphony_Number.Length > 0)
                            {
                                Core.DatabaseInsert(conn, "UPDATE [dbo].[Project] SET  [Project_StartDate] = '" + ProjectStartDate + "' ,[Project_EndDate] = '" + ProjectEndDate + "', [Status] = '" + Status + "', [Project_Owner] = '"+Project_Owner+ "',ClientName_ID = '"+ClientID+ "',Creator = '" + Session["Email"].ToString() +"' Where REPLACE(Symphony_Number, '-', '') = '" + row["Job ID"].ToString() + "'");
                            }
                            else if (dtProject.Rows.Count > 0 && a == false && b == true && Symphony_Number.Length > 0)
                            {
                                Core.DatabaseInsert(conn, "UPDATE [dbo].[Project] SET  [Project_StartDate] = '" + ProjectStartDate + "' ,[Project_EndDate] = '" + ProjectEndDate + "', [Status] = '" + Status + "', [Project_Owner] = '"+Project_Owner+ "',ClientName_ID = '"+ClientID+ "',Creator = '" + Session["Email"].ToString() +"' Where REPLACE(Symphony_Number, '-', '') = '" + row["Job ID"].ToString() + "'");
                            }
                            else if (dtProject.Rows.Count > 0 && a == false && b == false && Symphony_Number.Length > 0)
                            {
                                Core.DatabaseInsert(conn, "UPDATE [dbo].[Project] SET  [Project_StartDate] = NULL ,[Project_EndDate] = NULL, [Status] = '" + Status + "', [Project_Owner] = '"+Project_Owner+ "',ClientName_ID = '"+ClientID+ "',Creator = '" + Session["Email"].ToString() +"'  Where REPLACE(Symphony_Number, '-', '') = '" + row["Job ID"].ToString() + "'");
                            }
                            else if (dtProject.Rows.Count == 0 && a == true && b == true && Symphony_Number.Length > 0)
                            {
                                Core.DatabaseInsert(conn, "INSERT INTO [dbo].[Project] ([Symphony_Number],[Project_Name],[Project_Type_ID],[Project_SubType_ID],[Project_StartDate],[Project_EndDate],[Project_Owner],[Creator],[Created_DateTime],[Status],[ClientName_ID],[MarketSector_ID]) VALUES ('" + ChangeSymNumber(Symphony_Number) + "','" + Project_Name.Replace("'", "''") + "','" + ProjectTypeID + "','" + SubProjectTypeID + "','" + Convert.ToDateTime(StartDate).ToString("yyyy-MM-dd") + "','" + Convert.ToDateTime(EndDate).ToString("yyyy-MM-dd") + "','" + Project_Owner + "','" + Session["Email"].ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + Status + "','" + ClientID + "','" + MarketSectorID + "')");
                            }
                            else if (dtProject.Rows.Count == 0 && a == true && b == false && Symphony_Number.Length > 0)
                            {
                                Core.DatabaseInsert(conn, "INSERT INTO [dbo].[Project] ([Symphony_Number],[Project_Name],[Project_Type_ID],[Project_SubType_ID],[Project_StartDate],[Project_EndDate],[Project_Owner],[Creator],[Created_DateTime],[Status],[ClientName_ID],[MarketSector_ID]) VALUES ('" + ChangeSymNumber(Symphony_Number) + "','" + Project_Name.Replace("'", "''") + "','" + ProjectTypeID + "','" + SubProjectTypeID + "','" + Convert.ToDateTime(StartDate).ToString("yyyy-MM-dd") + "',NULL,'" + Project_Owner + "','" + Session["Email"].ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + Status + "','" + ClientID + "','" + MarketSectorID + "')");
                            }
                            else if (dtProject.Rows.Count == 0 && a == false && b == false && Symphony_Number.Length > 0)
                            {
                                Core.DatabaseInsert(conn, "INSERT INTO [dbo].[Project] ([Symphony_Number],[Project_Name],[Project_Type_ID],[Project_SubType_ID],[Project_StartDate],[Project_EndDate],[Project_Owner],[Creator],[Created_DateTime],[Status],[ClientName_ID],[MarketSector_ID]) VALUES ('" + ChangeSymNumber(Symphony_Number) + "','" + Project_Name.Replace("'", "''") + "','" + ProjectTypeID + "','" + SubProjectTypeID + "',NULL,NULL,'" + Project_Owner + "','" + Session["Email"].ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + Status + "','" + ClientID + "','" + MarketSectorID + "')");
                            }
                        }
                        catch { }
                    }
                    catch { }
                }
            }

        }
        public DataTable HtmlToDataTable(string html, int startRowIndex = 8)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var table = doc.DocumentNode.SelectSingleNode("//table");
            if (table == null) return null;

            var dt = new DataTable();

            // Get all rows
            var rows = table.SelectNodes(".//tr");
            if (rows == null) return dt;

            // We'll assume the columns are defined in the row BEFORE the startRowIndex,
            // e.g. row 5 (index 4), or you can define them manually.

            // Let's take columns from row 5 (index 4), so startRowIndex should be > 5

            if (startRowIndex <= 1 || startRowIndex > rows.Count)
                throw new ArgumentException("startRowIndex is out of range");

            // Define columns from row (startRowIndex - 1)
            var headerCells = rows[startRowIndex - 2].SelectNodes("th|td");
            if (headerCells == null) return dt;

            foreach (var cell in headerCells)
            {
                dt.Columns.Add(cell.InnerText.Trim());
            }

            // Now add data rows starting from startRowIndex (1-based)
            for (int i = startRowIndex - 1; i < rows.Count; i++)
            {
                var cells = rows[i].SelectNodes("td");
                if (cells == null) continue;

                var dataRow = dt.NewRow();
                for (int j = 0; j < cells.Count && j < dt.Columns.Count; j++)
                {
                    dataRow[j] = cells[j].InnerText.Trim();
                }
                dt.Rows.Add(dataRow);
            }

            return dt;
        }
        protected void Button10_Click1(object sender, EventArgs e)
        {
            if(Panel3.Visible == true)
            {
                Panel3.Visible = false;
            }
            else
            {
                Panel3.Visible = true;
            }
        }
    }
}