<%@ Page Title="Login" Language="C#" MasterPageFile="~/Site2.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Project._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="login-container">
        <div class="login-card">
            <!-- Header -->
            <div class="login-header">
                <i class="fas fa-project-diagram logo-icon"></i>
                <h1>Project Management</h1>
                <p>Sign in to your account</p>
            </div>

            <!-- Login Form -->
            <div class="login-body">
                <asp:Label ID="LblLoginMessage" runat="server" Visible="false" role="alert" />

                <!-- Username Field -->
                <div class="form-group">
                    <label class="form-label" for="<%= TextBox1.ClientID %>">
                        <i class="fas fa-user" style="color: #374ea1; margin-right: 0.5rem;"></i>Username
                    </label>
                    <asp:TextBox ID="TextBox1" runat="server"
                        placeholder="firstname.lastname@ipsos.com"
                        autocomplete="username" autocapitalize="none" spellcheck="false" MaxLength="256"
                        CssClass="login-form-control" />
                </div>

                <!-- Password Field -->
                <div class="form-group">
                    <label class="form-label" for="<%= TextBox2.ClientID %>">
                        <i class="fas fa-lock" style="color: #374ea1; margin-right: 0.5rem;"></i>Password
                    </label>
                    <asp:TextBox ID="TextBox2" runat="server"
                        TextMode="Password"
                        placeholder="Enter your password"
                        autocomplete="current-password"
                        CssClass="login-form-control" />
                </div>

                <!-- Forgot Password -->
                <div class="remember-forgot">
                    <span class="login-hint">Use your Ipsos network account</span>
                    <a href="https://ipsos.service-now.com/ess" class="forgot-link">
                        Forgot Password?
                    </a>
                </div>

                <!-- Login Button -->
                <asp:Button ID="Button1" runat="server"
                    Text="Sign In"
                    OnClick="Button1_Click"
                    CssClass="login-btn" />
            </div>

            <!-- Footer -->
            <div class="login-footer">
                <p>&copy; <%= DateTime.Now.Year %> Ipsos Limited. All rights reserved.</p>
            </div>
        </div>
    </div>
</asp:Content>
