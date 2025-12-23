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
                <!-- Username Field -->
                <div class="form-group">
                    <label class="form-label">
                        <i class="fas fa-envelope" style="color: #374ea1; margin-right: 0.5rem;"></i>Username / Email
                    </label>
                    <div class="input-wrapper">
                        <i class="fas fa-user input-icon"></i>
                        <asp:TextBox ID="TextBox1" runat="server"
                            placeholder="Enter your email"
                            CssClass="login-form-control" />
                    </div>
                </div>

                <!-- Password Field -->
                <div class="form-group">
                    <label class="form-label">
                        <i class="fas fa-lock" style="color: #374ea1; margin-right: 0.5rem;"></i>Password
                    </label>
                    <div class="input-wrapper">
                        <i class="fas fa-key input-icon"></i>
                        <asp:TextBox ID="TextBox2" runat="server"
                            TextMode="Password"
                            placeholder="Enter your password"
                            CssClass="login-form-control" />
                    </div>
                </div>

                <!-- Remember Me & Forgot Password -->
                <div class="remember-forgot">
                    <label class="remember-me">
                        <input type="checkbox" name="remember-me" />
                        <span>Remember me</span>
                    </label>
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
