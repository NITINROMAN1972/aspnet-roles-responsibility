using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using System.Configuration;
using System.Collections.Generic;
using System.Web.UI;

public partial class RoleNResp_RoleAndResp : System.Web.UI.Page
{
    string connectionString = ConfigurationManager.ConnectionStrings["Ginie"].ConnectionString;
    string selectedRole;

    protected void Page_Load(object sender, EventArgs e)
    {
        // PEOJECT  : SECR
        // CODE     : 751

        if (!IsPostBack)
        {
            Bind_Role();
            DataTable dtResp = GetResponsibilitiesData();
            BuildTreeView(dtResp, null);
        }
    }

    public void Bind_Role()
    {
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();
            string sql = "select UserRole, RoleName, HomePage from UserRoles751";
            SqlCommand cmd = new SqlCommand(sql, con);

            SqlDataAdapter ad = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            ad.Fill(dt);
            con.Close();

            ddlRole.DataSource = dt;
            ddlRole.DataTextField = "RoleName";
            //ddlRole.DataValueField = "UserRoleID";
            ddlRole.DataBind();
            ddlRole.Items.Insert(0, new ListItem("------Select Role------", "0"));
        }
    }

    private DataTable GetResponsibilitiesData()
    {
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();
            string sql = "select Seq, MenuText, MenuParent, MenuCode, Publish from MenuBars751 where Publish = 'YES' and MenuParent != ''";
            SqlCommand cmd = new SqlCommand(sql, con);

            SqlDataAdapter ad = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            ad.Fill(dt);
            con.Close();

            return dt;
        }
    }

    //============================================={ TreeView Generation Starts }===========================================================

    private void BuildTreeView(DataTable dtResp, TreeNode parentNode)
    {
        // setting TreeView Depth
        //TreeView1.ExpandDepth = 0; //treeview complete collapse
        //TreeView1.ExpandDepth = 1000; // higher values to fully expand, but not recommended

        // Create a mapping of MenuCode to TreeNode
        Dictionary<string, TreeNode> menuCodeToNode = new Dictionary<string, TreeNode>();

        // Sort the DataTable based on MenuParent and Seq
        DataView dv = dtResp.DefaultView;
        dv.Sort = "MenuParent, Seq";
        DataTable sortedDt = dv.ToTable();

        foreach (DataRow resp in sortedDt.Rows)
        {
            string menuCode = resp["MenuCode"].ToString();
            string menuParentCode = resp["MenuParent"].ToString();
            string menuName = resp["MenuText"].ToString();
            string publish = resp["Publish"].ToString();

            // Only build the tree for rows where Publish is 'YES'
            if (publish.Equals("YES", StringComparison.OrdinalIgnoreCase))
            {
                TreeNode newNode = new TreeNode(menuName, menuCode);

                // If the MenuCode is not in the dictionary, add it as a top-level node
                if (!menuCodeToNode.ContainsKey(menuCode))
                {
                    menuCodeToNode[menuCode] = newNode;

                    if (!string.IsNullOrEmpty(menuParentCode) && menuCodeToNode.ContainsKey(menuParentCode))
                    {
                        // adding child for existing parent
                        // Only add child nodes if the parent node's Publish value is 'YES'
                        menuCodeToNode[menuParentCode].ChildNodes.Add(newNode);

                        // This prevents the node from being selectable
                        newNode.SelectAction = TreeNodeSelectAction.None;
                        newNode.Text = $"<span class='text-dark fw-lighter'>{menuName}</span>";
                    }
                    else
                    {
                        Boolean isParent = CheckForNodeIsParent(menuCode); //checking for a node to be parent
                        Boolean isChildNodeParentPublishIsYes = CheckForParentNodePublishAsYes(menuCode); // checking a node's parent to have Publish YES

                        if (isParent)
                        {
                            // If there is no parent, add it to the TreeView
                            TreeView1.Nodes.Add(newNode);

                            // This prevents the node from being selectable
                            newNode.SelectAction = TreeNodeSelectAction.None;

                            //newNode.Text = $"<span class='text-primary-emphasis fw-semibold fw-bold text-uppercase fs-6'>{menuName}</span>";
                            newNode.Text = $"<span class='text-primary-emphasis fw-semibold bg-opacity-50 bg-body-tertiary border shadow px-3 rounded-2'>{menuName}</span>";
                        }
                        else
                        {
                            if (isChildNodeParentPublishIsYes)
                            {
                                // additional check to add only those child whose Parent Publish values is "YES"
                                TreeView1.Nodes.Add(newNode);

                                // This prevents the node from being selectable
                                newNode.SelectAction = TreeNodeSelectAction.None;

                                //newNode.Text = $"<span class='text-primary fw-semibold fs-6'>{menuName}</span>";
                                newNode.Text = $"<span class='text-dark fw-lighter'>{menuName}</span>";
                            }
                        }
                    }
                }
            }
        }
    }

    protected Boolean CheckForNodeIsParent(string menuCode)
    {
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();
            string sql = "SELECT TOP 1 1 FROM MenuBars751 WHERE MenuParent = @MenuCode";
            SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@MenuCode", menuCode);

            return cmd.ExecuteScalar() != null;
        }
    }

    protected Boolean CheckForParentNodePublishAsYes(string menuCode)
    {
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();
            string sql = "select m1.Seq, m1.MenuText, m1.MenuParent, m1.MenuCode, m1.Publish " +
                         "from MenuBars751 m1, MenuBars751 m2 " +
                         "where m1.MenuParent = m2.MenuCode and m2.Publish = 'YES' and m1.MenuCode = @MenuCode";

            SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@MenuCode", menuCode);
            cmd.ExecuteNonQuery();

            SqlDataAdapter ad = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            ad.Fill(dt);
            con.Close();

            return dt.Rows.Count > 0;
        }
    }

    //============================================={ After Role is changed }===============================================================

    protected void ddlRole_SelectedIndexChanged(object sender, EventArgs e)
    {
        selectedRole = ddlRole.SelectedValue;
        DataTable dt = new DataTable();
        Session["SelectedRole"] = selectedRole.ToString();

        ClearCheckboxes(TreeView1.Nodes);

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();
            string sql = "WITH SplitResponsibilities AS (SELECT UserRole, value AS Responsibility FROM mnu751 CROSS APPLY STRING_SPLIT(Responsibility, ','))" +
                         "SELECT UserRole,Responsibility FROM SplitResponsibilities where UserRole = @UserRole";
            SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@UserRole", selectedRole.ToString());
            cmd.ExecuteNonQuery();

            SqlDataAdapter ad = new SqlDataAdapter(cmd);
            ad.Fill(dt);
            con.Close();
        }

        foreach (DataRow row in dt.Rows)
        {
            string menuCode = row["Responsibility"].ToString();
            string role = row["UserRole"].ToString();

            TreeNode node = FindNodeByValue(TreeView1.Nodes, menuCode);

            if (node != null && role == selectedRole)
            {
                node.Checked = true;
            }
        }
    }

    private TreeNode FindNodeByValue(TreeNodeCollection nodes, string MenuParentCode)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Value == MenuParentCode)
            {
                return node;
            }

            TreeNode foundNode = FindNodeByValue(node.ChildNodes, MenuParentCode);

            if (foundNode != null)
            {
                return foundNode;
            }
        }
        return null;
    }

    private void ClearCheckboxes(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            node.Checked = false;
            ClearCheckboxes(node.ChildNodes);
        }
    }

    //============================================={ After Node is Clicked }===============================================================

    protected void TreeView1_SelectedNodeChanged(object sender, EventArgs e)
    {
        TreeNode selectedNode = TreeView1.SelectedNode;

        // Check if a node is selected
        if (selectedNode != null)
        {
            // If the node is already expanded, collapse it
            if (selectedNode.Expanded ?? false)
            {
                selectedNode.Collapse();
            }
            else
            {
                // If the node is not expanded, expand it along with its child nodes
                selectedNode.ExpandAll();
            }
        }
    }

    //============================================={ After Save button clicked }===========================================================

    protected void btnSave_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(ddlRole.SelectedValue) || ddlRole.SelectedValue == "0")
        {
            Page.Validate();
        }
        else
        {
            // 1. get all selected responsibilities for the selected role
            selectedRole = ddlRole.SelectedValue;

            // 2. check if role exists in mnu table
            Boolean roleIsPresent = CheckRoleExistsInMnu(selectedRole);

            if (roleIsPresent)
            {
                // 3. assign responsiblities
                AssignResponsibilitiesToRole(selectedRole);

                // 4. Collect the selected nodes from the TreeView
                List<string> selectedNodes = GetSelectedNodes(TreeView1.Nodes);

                // 5. Update the responsibilities in the database for the selected role
                UpdateDatabaseResponsibilities(selectedNodes);

                LiteralMsg.Text = "Roles and Responsibilities Updated!";

                // JS alert message pop-up without redirecting
                string message = $"{Session["SelectedRole"].ToString()} responsibilities has been updated";
                string script = $"alert('{message}')";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "messageScript", script, true);
            }
            else
            {
                // 3. add role
                addNewRoleIntoMnuTable(selectedRole);

                // 4. assign responsiblities
                AssignResponsibilitiesToRole(selectedRole);

                // 5. Collect the selected nodes from the TreeView
                List<string> selectedNodes = GetSelectedNodes(TreeView1.Nodes);

                // 6. Update the responsibilities in the database for the selected role
                UpdateDatabaseResponsibilities(selectedNodes);

                LiteralMsg.Text = "Roles and Responsibilities Updated!";

                // JS alert message pop-up without redirecting
                string message = $"{Session["SelectedRole"].ToString()} responsibilities has been updated";
                string script = $"alert('{message}')";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "messageScript", script, true);
            }
        }
    }

    private Boolean CheckRoleExistsInMnu(string selectedRole)
    {
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();
            string sql = "SELECT UserRole, Responsibility FROM mnu751 WHERE UserRole = @UserRole";
            SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@UserRole", selectedRole.ToString());
            cmd.ExecuteNonQuery();

            SqlDataAdapter ad = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            ad.Fill(dt);
            con.Close();

            return dt.Rows.Count > 0;
        }
    }

    private void AssignResponsibilitiesToRole(string selectedRole)
    {
        DataTable dt = new DataTable();

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();
            string sql = "WITH SplitResponsibilities AS (SELECT UserRole, value AS Responsibility FROM mnu751 CROSS APPLY STRING_SPLIT(Responsibility, ','))" +
                         "SELECT UserRole, Responsibility FROM SplitResponsibilities where UserRole = @UserRole";
            SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@UserRole", selectedRole.ToString());
            cmd.ExecuteNonQuery();

            SqlDataAdapter ad = new SqlDataAdapter(cmd);
            ad.Fill(dt);
            con.Close();
        }
    }

    private void addNewRoleIntoMnuTable(string selectedRole)
    {
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();
            string sql = "INSERT INTO mnu751 (UserRole) VALUES (@UserRole)";
            SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@UserRole", selectedRole.ToString());
            cmd.ExecuteNonQuery();
            con.Close();
        }
    }

    private List<string> GetSelectedNodes(TreeNodeCollection nodes)
    {
        List<string> selectedNodes = new List<string>();

        foreach (TreeNode node in nodes)
        {
            if (node.Checked)
            {
                selectedNodes.Add(node.Value);
            }

            selectedNodes.AddRange(GetSelectedNodes(node.ChildNodes));
        }

        return selectedNodes;
    }

    private void UpdateDatabaseResponsibilities(List<string> selectedNodes)
    {
        // Join the selected nodes into a single string
        string updatedResponsibilities = string.Join(",", selectedNodes);

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();
            string updateSql = "UPDATE mnu751 SET Responsibility = @Responsibilities WHERE UserRole = @UserRole";
            SqlCommand updateCmd = new SqlCommand(updateSql, con);
            updateCmd.Parameters.AddWithValue("@Responsibilities", updatedResponsibilities);
            updateCmd.Parameters.AddWithValue("@UserRole", selectedRole);
            int rowsAffected = updateCmd.ExecuteNonQuery();
            con.Close();
        }
    }

    //============================================={ After Reset button clicked }==========================================================

    protected void btnReset_Click(object sender, EventArgs e)
    {
        ClearCheckboxes(TreeView1.Nodes);
    }

    //============================================={ After Check Event }==========================================================

}